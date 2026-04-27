using System;

namespace UniCliqueBackend.Application.DTOs.Post
{
    public class PostDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string? UserProfilePhoto { get; set; }
        
        public Guid EventId { get; set; }
        public string EventTitle { get; set; } = string.Empty;
        
        public string? Content { get; set; }
        public string? PhotoUrl { get; set; }
        
        public DateTime CreatedAt { get; set; }

        public int LikeCount { get; set; } = 0;
        public int CommentCount { get; set; } = 0;
        public bool IsLiked { get; set; } = false;
    }
}
