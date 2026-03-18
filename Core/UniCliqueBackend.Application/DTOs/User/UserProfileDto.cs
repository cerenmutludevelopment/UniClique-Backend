using System;
using UniCliqueBackend.Domain.Enums;

namespace UniCliqueBackend.Application.DTOs.User
{
    public class UserProfileDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DateTime BirthDate { get; set; }
        public RoleType Role { get; set; }
        public bool IsStudent { get; set; }
        public string? StudentDocumentUrl { get; set; }
        public StudentVerificationStatus StudentVerificationStatus { get; set; }
        
        // Profile Details
        public string? ProfilePhotoUrl { get; set; }
        public string? University { get; set; }
        public string? Department { get; set; }
        public string? Bio { get; set; }
        
        // Stats
        public int InteractionScore { get; set; }
        public int FriendCount { get; set; }
        public int CreatedEventCount { get; set; }
        public int JoinedEventCount { get; set; }
        
        public bool IsEmailVerified { get; set; }
    }
}
