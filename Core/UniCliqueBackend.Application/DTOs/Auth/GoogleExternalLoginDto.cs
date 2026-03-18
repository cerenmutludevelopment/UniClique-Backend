namespace UniCliqueBackend.Application.DTOs.Auth
{
    public class GoogleExternalLoginDto
    {
        public string ProviderUserId { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? FullName { get; set; }
    }
}
