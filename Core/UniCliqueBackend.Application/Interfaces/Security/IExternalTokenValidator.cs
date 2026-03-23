using System.Threading.Tasks;

namespace UniCliqueBackend.Application.Interfaces.Security
{
    public interface IExternalTokenValidator
    {
        Task<(string ProviderUserId, string? Email, string? FullName)> ValidateGoogleIdTokenAsync(string idToken);
        Task<(string ProviderUserId, string? Email, string? FullName)> ValidateAppleIdentityTokenAsync(string identityToken);
    }
}
