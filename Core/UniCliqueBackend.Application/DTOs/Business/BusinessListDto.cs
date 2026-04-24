using System;
using System.Collections.Generic;

namespace UniCliqueBackend.Application.DTOs.Business
{
    public class BusinessListDto
    {
        public Guid UserId { get; set; }
        public string BusinessName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? City { get; set; }
        public string? Address { get; set; }
        public TimeOnly OpeningHours { get; set; }
        public TimeOnly ClosingHours { get; set; }
        public List<string> PhotoUrls { get; set; } = new();
        public string? Activities { get; set; }
    }
}
