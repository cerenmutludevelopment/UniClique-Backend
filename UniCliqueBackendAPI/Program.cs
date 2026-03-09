using Microsoft.EntityFrameworkCore;
using UniCliqueBackend.Persistence.Contexts;
using UniCliqueBackend.Persistence;
using UniCliqueBackend.Application;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using UniCliqueBackend.Application.Options;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using UniCliqueBackend.Application.DTOs.Common;
using System.Security.Claims;
using System.Net.Mail;



var builder = WebApplication.CreateBuilder(args);

// --------------------
// DATABASE
// --------------------
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var conn = builder.Configuration.GetConnectionString("PostgreSql");
    if (string.IsNullOrWhiteSpace(conn))
        conn = builder.Configuration.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrWhiteSpace(conn))
    {
        var t = conn.Trim();
        var l = t.ToLowerInvariant();
        if (l.StartsWith("postgres://") || l.StartsWith("postgresql://"))
        {
            var u = new Uri(t);
            var ui = u.UserInfo.Split(':', 2);
            var un = ui.Length > 0 ? ui[0] : "";
            var pw = ui.Length > 1 ? ui[1] : "";
            var h = u.Host;
            var pt = u.Port > 0 ? u.Port : 5432;
            var db = u.AbsolutePath.TrimStart('/');
            var kv = $"Host={h};Port={pt};Database={db};Username={un};Password={pw}";
            var q = u.Query;
            if (!string.IsNullOrEmpty(q))
            {
                var parts = q.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in parts)
                {
                    var kvp = p.Split('=', 2);
                    var k = kvp[0].ToLowerInvariant();
                    var v = kvp.Length > 1 ? kvp[1] : "";
                    if (k == "sslmode" && !string.IsNullOrEmpty(v))
                    {
                        var vv = char.ToUpperInvariant(v[0]) + v.Substring(1);
                        kv += $";SslMode={vv}";
                    }
                    if (k == "trust_server_certificate" && !string.IsNullOrEmpty(v))
                    {
                        kv += $";Trust Server Certificate={v}";
                    }
                }
            }
            conn = kv;
        }
    }
    options.UseNpgsql(conn);
});

// --------------------
// LAYER REGISTRATIONS
// --------------------
builder.Services.AddApplication();
builder.Services.AddPersistence();

// --------------------
// VALIDATION
// --------------------
builder.Services.AddControllers();

builder.Services
    .AddFluentValidationAutoValidation()
    .AddFluentValidationClientsideAdapters();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "UniClique API", Version = "v1" });
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Header değeri: Bearer {token}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Authorization" }
    };
    c.AddSecurityDefinition("Authorization", securityScheme);
    var securityRequirement = new OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>() }
    };
    c.AddSecurityRequirement(securityRequirement);
});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
       
        var issuer = builder.Configuration["Jwt:Issuer"];
        var audience = builder.Configuration["Jwt:Audience"];
        var secret = builder.Configuration["Jwt:SecretKey"];
            options.TokenValidationParameters = new TokenValidationParameters
            {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,

            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret!)),
            ClockSkew = TimeSpan.Zero,
            RoleClaimType = ClaimTypes.Role
        };
    });

builder.Services.Configure<EmailPolicyOptions>(builder.Configuration.GetSection("EmailPolicy"));

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errorsList = context.ModelState
            .Where(ms => ms.Value!.Errors.Count > 0)
            .SelectMany(kvp => kvp.Value!.Errors.Select(err => new
            {
                field = kvp.Key,
                code = "validation",
                message = err.ErrorMessage
            }))
            .ToList();

        var payload = new ApiMessageDto
        {
            Message = "Doğrulama hatası"
        };
        return new BadRequestObjectResult(payload);
    };
});

var app = builder.Build();

// --------------------
// MIDDLEWARE
// --------------------
// Swagger is enabled in both Dev and Production for easier testing/verification
app.UseSwagger();
app.UseSwaggerUI();

// Root path handler to avoid 404
app.MapGet("/", () => Results.Ok(new { message = "UniClique API is running!", swagger = "/swagger/index.html" }));

// --------------------
// SEED DATABASE
// --------------------
using (var scope = app.Services.CreateScope())
{
    try 
    {
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await UniCliqueBackend.Persistence.Seed.AppDbSeeder.SeedAsync(context);
    }
    catch (Exception ex)
    {
        // Simple logging or ignore for now, preventing crash if db not ready
        Console.WriteLine($"Seeding failed: {ex.Message}");
    }
}

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var feature = context.Features.Get<IExceptionHandlerFeature>();
        var ex = feature?.Error;

        var statusCode = 500;
        var code = "error.unhandled";
        var message = "Beklenmeyen bir hata oluştu.";

        var msg = ex?.Message ?? "";
        if (!string.IsNullOrEmpty(msg))
        {
            if (msg.Contains("Invalid credentials.", StringComparison.OrdinalIgnoreCase))
            {
                statusCode = 401; code = "auth.invalid_credentials"; message = "Kimlik bilgileri geçersiz.";
            }
            else if (msg.Contains("Account is not active.", StringComparison.OrdinalIgnoreCase))
            {
                statusCode = 403; code = "auth.account_inactive"; message = "Hesap aktif değil.";
            }
            else if (msg.Contains("Invalid refresh token.", StringComparison.OrdinalIgnoreCase))
            {
                statusCode = 401; code = "token.invalid"; message = "Yenileme tokenı geçersiz.";
            }
            else if (msg.Contains("Refresh token revoked.", StringComparison.OrdinalIgnoreCase))
            {
                statusCode = 401; code = "token.revoked"; message = "Yenileme tokenı iptal edildi.";
            }
            else if (msg.Contains("Refresh token expired.", StringComparison.OrdinalIgnoreCase))
            {
                statusCode = 401; code = "token.expired"; message = "Yenileme tokenı süresi doldu.";
            }
            else if (msg.Contains("User not found.", StringComparison.OrdinalIgnoreCase))
            {
                statusCode = 404; code = "user.not_found"; message = "Kullanıcı bulunamadı.";
            }
            else if (msg.Contains("already exists", StringComparison.OrdinalIgnoreCase))
            {
                statusCode = 409; code = "user.already_exists"; message = "Bu e-posta veya kullanıcı adı kullanılıyor.";
            }
            else if (msg.Contains("Phone already exists", StringComparison.OrdinalIgnoreCase))
            {
                statusCode = 409; code = "user.phone_exists"; message = "Telefon numarası zaten kayıtlı.";
            }
            else if (msg.Contains("Email is required for first-time external login.", StringComparison.OrdinalIgnoreCase))
            {
                statusCode = 400; code = "external_login.email_required"; message = "İlk dış giriş için e-posta zorunludur.";
            }
            else if (msg.Contains("Verification code not found.", StringComparison.OrdinalIgnoreCase))
            {
                statusCode = 404; code = "verification.code_not_found"; message = "Doğrulama kodu bulunamadı.";
            }
            else if (msg.Contains("Verification code expired.", StringComparison.OrdinalIgnoreCase))
            {
                statusCode = 400; code = "verification.code_expired"; message = "Doğrulama kodunun süresi dolmuş.";
            }
            else if (msg.Contains("Invalid verification code.", StringComparison.OrdinalIgnoreCase))
            {
                statusCode = 400; code = "verification.code_invalid"; message = "Doğrulama kodu geçersiz.";
            }
            else if (msg.Contains("Email already verified.", StringComparison.OrdinalIgnoreCase))
            {
                statusCode = 409; code = "verification.already_verified"; message = "E-posta zaten doğrulanmış.";
            }
            else if (msg.Contains("Email not verified.", StringComparison.OrdinalIgnoreCase))
            {
                statusCode = 403; code = "auth.email_not_verified"; message = "E-posta doğrulanmadı.";
            }
            else if (msg.Contains("Verification code sent.", StringComparison.OrdinalIgnoreCase))
            {
                statusCode = 403; code = "auth.verification_required"; message = "Doğrulama kodu gönderildi.";
            }
            else if (msg.Contains("SMTP send timeout", StringComparison.OrdinalIgnoreCase))
            {
                statusCode = 504; code = "smtp.timeout"; message = "SMTP gönderimi zaman aşımına uğradı.";
            }
            else if (msg.Contains("SMTP auth failed", StringComparison.OrdinalIgnoreCase))
            {
                statusCode = 502; code = "smtp.auth_failed"; message = "SMTP kimlik doğrulama başarısız.";
            }
            else if (msg.Contains("SMTP connect failed", StringComparison.OrdinalIgnoreCase))
            {
                statusCode = 502; code = "smtp.connect_failed"; message = "SMTP sunucusuna bağlanılamadı.";
            }
            else if (msg.Contains("SMTP network unreachable", StringComparison.OrdinalIgnoreCase))
            {
                statusCode = 502; code = "smtp.network_unreachable"; message = "SMTP ağına erişilemedi.";
            }
            else if (msg.Contains("SMTP invalid email format", StringComparison.OrdinalIgnoreCase))
            {
                statusCode = 400; code = "smtp.invalid_email"; message = "Gönderen veya alıcı e-posta adresi geçersiz.";
            }
        }
        if (app.Environment.IsDevelopment() && !string.IsNullOrEmpty(ex?.Message))
        {
            message = ex!.Message;
        }

        var payload = new ApiMessageDto
        {
            Message = message
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(payload);
    });
});

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
