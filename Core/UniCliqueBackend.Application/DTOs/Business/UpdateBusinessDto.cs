using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace UniCliqueBackend.Application.DTOs.Business
{
    /// <summary>
    /// Partial update DTO (PATCH style). Only provided fields will be updated.
    /// </summary>
    public class UpdateBusinessDto
    {
        [MaxLength(200)]
        public string? BusinessName { get; set; }

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        public TimeOnly? OpeningHours { get; set; }

        public TimeOnly? ClosingHours { get; set; }

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
