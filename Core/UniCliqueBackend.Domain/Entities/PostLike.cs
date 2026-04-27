using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UniCliqueBackend.Domain.Common;

namespace UniCliqueBackend.Domain.Entities
{
    public class PostLike : BaseEntity
    {
        [Required]
        public Guid PostId { get; set; }
        [ForeignKey("PostId")]
        public Post Post { get; set; } = null!;

        [Required]
        public Guid UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
    }
}
