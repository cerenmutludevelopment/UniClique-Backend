using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UniCliqueBackend.Domain.Common;

namespace UniCliqueBackend.Domain.Entities
{
    public class Post : BaseEntity
    {
        [Required]
        public Guid EventId { get; set; }
        [ForeignKey("EventId")]
        public Event Event { get; set; } = null!;

        [Required]
        public Guid UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        [MaxLength(500)]
        public string? Content { get; set; }

        [MaxLength(200)]
        public string? PhotoUrl { get; set; }

        public bool IsDeleted { get; set; } = false;

        public ICollection<PostLike> Likes { get; set; } = new List<PostLike>();
        public ICollection<PostComment> Comments { get; set; } = new List<PostComment>();
    }
}
