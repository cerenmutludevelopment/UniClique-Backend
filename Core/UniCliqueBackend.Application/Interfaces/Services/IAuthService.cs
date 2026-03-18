using UniCliqueBackend.Application.DTOs.Auth;

namespace UniCliqueBackend.Application.Interfaces.Services
{
    public interface IAuthService
    {
        Task<string> RegisterAsync(RegisterRequestDto request);

        Task<TokenResponseDto> LoginAsync(LoginRequestDto request);

        Task<TokenResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request);

        Task LogoutByRefreshAsync(string refreshToken);
        Task<TokenResponseDto> ExternalLoginAsync(ExternalLoginRequestDto request);
        Task<TokenResponseDto> VerifyEmailByUserIdAsync(VerifyEmailByUserIdRequestDto request);
        Task ResendRegisterEmailVerificationAsync(ResendEmailVerificationRequestDto request);
        Task ResetDatabaseAsync();
        Task<bool> DeleteUserByEmailAsync(string email);
        Task<string> ForgotPasswordStartAsync(ForgotPasswordStartRequestDto request);
        Task VerifyPasswordResetCodeByUserIdAsync(VerifyResetByUserIdRequestDto request);
        Task ResetPasswordWithCodeByUserIdAsync(ResetPasswordByUserIdRequestDto request);
        Task<bool> IsUsernameAvailableAsync(string username);
    }
}
