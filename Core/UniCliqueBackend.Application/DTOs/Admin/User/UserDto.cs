using System;
using UniCliqueBackend.Domain.Enums;

namespace UniCliqueBackend.Application.DTOs.Admin.User
{
    public class UserDto
    {
        public string Id { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Username { get; set; } = "";
        public RoleType Role { get; set; }
        public bool IsStudent { get; set; }
        public StudentVerificationStatus StudentVerificationStatus { get; set; }
        public string? StudentDocumentUrl { get; set; }
        public DateTime? StudentVerifiedAt { get; set; }
        public bool IsEmailVerified { get; set; }
        public bool IsActive { get; set; }
        public bool IsBanned { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
