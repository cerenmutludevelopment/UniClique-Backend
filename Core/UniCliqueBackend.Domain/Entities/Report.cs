using System;
using System.ComponentModel.DataAnnotations;
using UniCliqueBackend.Domain.Common;
using UniCliqueBackend.Domain.Enums;

namespace UniCliqueBackend.Domain.Entities
{
    public class Report : BaseEntity
    {
        [Required]
        public Guid ReporterId { get; set; }
        public User Reporter { get; set; } = null!;

        [Required, MaxLength(100)]
        public string Subject { get; set; } = string.Empty;

        [Required, MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        public ReportType Type { get; set; }

        // For reporting specific users or events
        public Guid? RelatedEntityId { get; set; }

        public bool IsResolved { get; set; } = false;
        public DateTime? ResolvedAt { get; set; }
        public string? AdminNote { get; set; }
    }
}
