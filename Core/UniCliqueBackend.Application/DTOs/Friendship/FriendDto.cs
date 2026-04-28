using System;

namespace UniCliqueBackend.Application.DTOs.Friendship
{
    public class FriendDto
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? ProfilePhotoUrl { get; set; }
        public string? University { get; set; }
        public string? Department { get; set; }
        
        // New fields for dynamic UI
        public UniCliqueBackend.Domain.Enums.FriendshipStatus? Status { get; set; }
        public bool IsSentByMe { get; set; }
        public Guid? FriendshipId { get; set; }
    }
}
