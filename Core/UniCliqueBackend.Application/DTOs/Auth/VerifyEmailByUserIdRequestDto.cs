namespace UniCliqueBackend.Application.DTOs.Auth
{
    public class VerifyEmailByUserIdRequestDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }
}
