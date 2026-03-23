using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using UniCliqueBackend.Application.Interfaces.Security;
using UniCliqueBackend.Application.Options;

namespace UniCliqueBackend.Persistence.Security
{
    public class ExternalTokenValidator : IExternalTokenValidator
    {
        private readonly ExternalAuthOptions _options;
        private readonly HttpClient _http;

        public ExternalTokenValidator(IOptions<ExternalAuthOptions> options)
        {
            _options = options.Value;
            _http = new HttpClient();
        }

        public async Task<(string ProviderUserId, string? Email, string? FullName)> ValidateGoogleIdTokenAsync(string idToken)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(idToken);

            if (_options.SkipSignatureValidation)
            {
                var sub = jwt.Subject ?? jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? "";
                var email = jwt.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
                var name = jwt.Claims.FirstOrDefault(c => c.Type == "name")?.Value;
                return (sub, email, name);
            }

            var googleIssuer = "https://accounts.google.com";
            var aud = _options.GoogleClientId;
            var jwksJson = await _http.GetStringAsync("https://www.googleapis.com/oauth2/v3/certs");
            var jwks = new JsonWebKeySet(jwksJson);
            var keys = jwks.GetSigningKeys();

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = googleIssuer,
                ValidateAudience = true,
                ValidAudience = aud,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = keys,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(2)
            };

            handler.ValidateToken(idToken, validationParameters, out var validated);
            var token = (JwtSecurityToken)validated;
            var sub2 = token.Subject ?? token.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? "";
            var email2 = token.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
            var name2 = token.Claims.FirstOrDefault(c => c.Type == "name")?.Value;
            return (sub2, email2, name2);
        }

        public async Task<(string ProviderUserId, string? Email, string? FullName)> ValidateAppleIdentityTokenAsync(string identityToken)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(identityToken);

            if (_options.SkipSignatureValidation)
            {
                var sub = jwt.Subject ?? jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? "";
                var email = jwt.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
                var name = jwt.Claims.FirstOrDefault(c => c.Type == "name")?.Value;
                return (sub, email, name);
            }

            var appleIssuer = "https://appleid.apple.com";
            var aud = _options.AppleClientId;
            var jwksJson = await _http.GetStringAsync("https://appleid.apple.com/auth/keys");
            var jwks = new JsonWebKeySet(jwksJson);
            var keys = jwks.GetSigningKeys();

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = appleIssuer,
                ValidateAudience = true,
                ValidAudience = aud,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = keys,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(2)
            };

            handler.ValidateToken(identityToken, validationParameters, out var validated);
            var token = (JwtSecurityToken)validated;
            var sub2 = token.Subject ?? token.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? "";
            var email2 = token.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
            var name2 = token.Claims.FirstOrDefault(c => c.Type == "name")?.Value;
            return (sub2, email2, name2);
        }
    }
}
