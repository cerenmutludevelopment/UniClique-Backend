using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UniCliqueBackend.Domain.Common;

namespace UniCliqueBackend.Domain.Entities
{
    public class BusinessDetail : BaseEntity
    {
        [Required]
        public Guid UserId { get; set; }
        
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        [Required, MaxLength(200)]
        public string BusinessName { get; set; } = "";

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

        [MaxLength(2000)]
        public string? PhotoUrls { get; set; } // Comma separated URLs
    }
}
