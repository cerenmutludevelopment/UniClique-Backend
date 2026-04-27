using System;
using UniCliqueBackend.Domain.Enums;

namespace UniCliqueBackend.Application.DTOs.Report
{
    public class ReportDto
    {
        public Guid Id { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ReportType Type { get; set; }
        public Guid? RelatedEntityId { get; set; }
        public string ReportedById { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsResolved { get; set; }
    }
}
