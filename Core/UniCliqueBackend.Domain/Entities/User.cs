using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using UniCliqueBackend.Domain.Common;
using UniCliqueBackend.Domain.Enums;

namespace UniCliqueBackend.Domain.Entities
{
    public class User : BaseEntity
    {
        [Required, MaxLength(100)]
        public string FullName { get; set; } = ""; 

        [Required, EmailAddress, MaxLength(255)]
        public string Email { get; set; } = "";

        [Required, MaxLength(50)]
        public string Username { get; set; } = "";

        [Required]
        public string PasswordHash { get; set; } = "";

        [Required, MaxLength(20)]
        public string PhoneNumber { get; set; } = "";

        public DateTime BirthDate { get; set; }

        public RoleType Role { get; set; } = RoleType.User;

        public bool IsStudent { get; set; } = false;
        [MaxLength(300)]
        public string? StudentDocumentUrl { get; set; }
        public StudentVerificationStatus StudentVerificationStatus { get; set; } = StudentVerificationStatus.None;
        public DateTime? StudentVerifiedAt { get; set; }
        [MaxLength(300)]
        public string? StudentVerificationNote { get; set; }

        // Profile Details
        [MaxLength(200)]
        public string? ProfilePhotoUrl { get; set; } // Profil fotoğrafı
        
        [MaxLength(100)]
        public string? University { get; set; } // Üniversite
        
        [MaxLength(100)]
        public string? Department { get; set; } // Bölüm

        [MaxLength(500)]
        public string? Bio { get; set; } // Biyografi

        // Gamification
        public int InteractionScore { get; set; } = 0; // Etkileşim puanı

        public bool IsEmailVerified { get; set; } = false;
        public DateTime? EmailVerifiedAt { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsBanned { get; set; } = false;
        public bool IsDeleted { get; set; } = false; // Soft delete

        public DateTime? LastLoginAt { get; set; }

        // Navigation
        public ICollection<UserConsent> UserConsents { get; set; } = new List<UserConsent>();
        public ICollection<UserRefreshToken> RefreshTokens { get; set; } = new List<UserRefreshToken>();
        public ICollection<UserVerificationCode> VerificationCodes { get; set; } = new List<UserVerificationCode>();
        public ICollection<UserExternalLogin> ExternalLogins { get; set; } = new List<UserExternalLogin>();

        // New Feature Navigations
        public ICollection<Friendship> UseSentFriendRequests { get; set; } = new List<Friendship>();
        public ICollection<Friendship> UserReceivedFriendRequests { get; set; } = new List<Friendship>();
        public ICollection<Event> CreatedEvents { get; set; } = new List<Event>();
        public ICollection<EventParticipant> JoinedEvents { get; set; } = new List<EventParticipant>();
        public ICollection<Post> Posts { get; set; } = new List<Post>();
        public ICollection<BusinessRequest> BusinessRequests { get; set; } = new List<BusinessRequest>();
    }
}
