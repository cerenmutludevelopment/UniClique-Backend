namespace UniCliqueBackend.Application.Options
{
    public class ExternalAuthOptions
    {
        public string? GoogleClientId { get; set; }
        public string? AppleClientId { get; set; }
        public bool SkipSignatureValidation { get; set; } = true;
    }
}
