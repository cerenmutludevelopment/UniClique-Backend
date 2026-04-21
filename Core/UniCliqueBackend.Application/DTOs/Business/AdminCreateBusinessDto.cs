using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace UniCliqueBackend.Application.DTOs.Business
{
    public class AdminCreateBusinessDto
    {
        [Required, MaxLength(200)]
        public string BusinessName { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        public TimeOnly OpeningHours { get; set; }

        public TimeOnly ClosingHours { get; set; }

        [MaxLength(1000)]
        public string? Activities { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(2000)]
        public string? Description { get; set; }

        public List<string>? PhotoUrls { get; set; }
    }
}
