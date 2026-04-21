using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace UniCliqueBackend.Persistence.Contexts
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

            // API projesinin yolunu belirle
            var basePath = Directory.GetCurrentDirectory();
            var apiPath = Path.Combine(basePath, "UniCliqueBackendAPI");
            if (!Directory.Exists(apiPath))
            {
                apiPath = Path.Combine(basePath, "..", "UniCliqueBackendAPI");
            }

            // Konfigürasyonu yükle (appsettings + user secrets + env)
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile(Path.Combine(apiPath, "appsettings.json"), optional: true)
                .AddJsonFile(Path.Combine(apiPath, "appsettings.Development.json"), optional: true)
                .AddUserSecrets("4601c2c0-c912-4a4f-9118-3a2cf029ce8b")
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration.GetConnectionString("PostgreSql") 
                ?? configuration["ConnectionStrings:PostgreSql"];

            if (string.IsNullOrEmpty(connectionString))
            {
                // Fallback (Sadece hiçbir yerde bulunamazsa kullanılır)
                connectionString = "Host=localhost;Port=5432;Database=uniclique_db;Username=postgres;Password=postgres";
            }

            var t = connectionString.Trim();
            var l = t.ToLowerInvariant();
            
            // Neon gibi servislerin sağladığı postgresql:// formatını Npgsql formatına çevir
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
                connectionString = kv;
            }

            optionsBuilder.UseNpgsql(connectionString, b => b.MigrationsAssembly("UniCliqueBackend.Persistence"));
            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
