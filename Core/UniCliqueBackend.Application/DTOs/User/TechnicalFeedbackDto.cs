using System.ComponentModel.DataAnnotations;

namespace UniCliqueBackend.Application.DTOs.User
{
    public class TechnicalFeedbackDto
    {
        [Required, MaxLength(100)]
        public string Subject { get; set; } = string.Empty;

        [Required, MaxLength(2000)]
        public string Description { get; set; } = string.Empty;
    }
}
