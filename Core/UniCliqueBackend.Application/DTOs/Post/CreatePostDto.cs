using System;
using System.ComponentModel.DataAnnotations;

namespace UniCliqueBackend.Application.DTOs.Post
{
    public class CreatePostDto
    {
        [Required]
        public Guid EventId { get; set; }

        [MaxLength(500)]
        public string? Content { get; set; }

        [MaxLength(200)]
        public string? PhotoUrl { get; set; }

        public System.Collections.Generic.List<Guid>? TaggedUserIds { get; set; } = new System.Collections.Generic.List<Guid>();
    }
}
