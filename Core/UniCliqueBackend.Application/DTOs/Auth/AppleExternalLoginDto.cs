namespace UniCliqueBackend.Application.DTOs.Auth
{
    public class AppleExternalLoginDto
    {
        public string ProviderUserId { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? FullName { get; set; }
    }
}
