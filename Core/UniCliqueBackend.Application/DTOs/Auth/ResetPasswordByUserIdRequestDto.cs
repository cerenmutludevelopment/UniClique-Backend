namespace UniCliqueBackend.Application.DTOs.Auth
{
    public class ResetPasswordByUserIdRequestDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}
