using System;
using System.ComponentModel.DataAnnotations;
using UniCliqueBackend.Domain.Enums;

namespace UniCliqueBackend.Application.DTOs.Report
{
    public class CreateReportDto
    {
        [Required]
        [MaxLength(100)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        public ReportType Type { get; set; } // Will be overridden by the specific endpoint mostly

        public Guid? RelatedEntityId { get; set; }
    }
}
