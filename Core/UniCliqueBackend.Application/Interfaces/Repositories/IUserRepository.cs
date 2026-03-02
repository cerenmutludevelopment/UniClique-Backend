using UniCliqueBackend.Domain.Entities;
using UniCliqueBackend.Domain.Enums;

namespace UniCliqueBackend.Application.Interfaces.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByUsernameAsync(string username);
        Task<User?> GetByIdAsync(Guid id);
        
        Task<IEnumerable<User>> SearchUsersAsync(string query); // New Search Method

        Task AddAsync(User user);
        Task UpdateAsync(User user);
        Task<bool> ExistsAsync(string email, string username);
        Task AddRefreshTokenAsync(Guid userId, UserRefreshToken token);
        Task<UserRefreshToken?> GetRefreshTokenAsync(string tokenHash);
        Task RevokeRefreshTokenAsync(UserRefreshToken token, string? replacedByTokenHash);
        Task<User?> GetByExternalLoginAsync(string provider, string providerUserId);
        Task AddExternalLoginAsync(Guid userId, UserExternalLogin externalLogin);
        Task<bool> PhoneExistsAsync(string phoneE164, string phoneLocal);
        Task AddVerificationCodeAsync(Guid userId, UserVerificationCode code);
        Task RemoveActiveVerificationCodesAsync(Guid userId, VerificationPurpose purpose);
        Task<UserVerificationCode?> GetLatestVerificationCodeAsync(Guid userId, VerificationPurpose purpose);
        Task UpdateVerificationCodeAsync(UserVerificationCode code);
        Task ClearAllUserDataAsync();
        Task HardDeleteUserByIdAsync(Guid userId);
    }
}
