using System;
using System.ComponentModel.DataAnnotations;

namespace UniCliqueBackend.Application.DTOs.Post
{
    public class CreatePostCommentDto
    {
        [Required]
        [MaxLength(500)]
        public string Content { get; set; } = string.Empty;

        public System.Collections.Generic.List<Guid>? TaggedUserIds { get; set; } = new System.Collections.Generic.List<Guid>();
    }
}
