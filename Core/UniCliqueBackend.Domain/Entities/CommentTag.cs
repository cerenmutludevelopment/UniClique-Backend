using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UniCliqueBackend.Domain.Common;

namespace UniCliqueBackend.Domain.Entities
{
    public class CommentTag : BaseEntity
    {
        [Required]
        public Guid CommentId { get; set; }
        [ForeignKey("CommentId")]
        public PostComment Comment { get; set; } = null!;

        [Required]
        public Guid UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
    }
}
