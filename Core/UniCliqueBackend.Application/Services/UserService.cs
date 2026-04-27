using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UniCliqueBackend.Application.DTOs.Admin.User;
using UniCliqueBackend.Application.DTOs.User;
using UniCliqueBackend.Application.Interfaces.Repositories;
using UniCliqueBackend.Application.Interfaces.Security;
using UniCliqueBackend.Application.Interfaces.Services;
using UniCliqueBackend.Domain.Entities;
using UniCliqueBackend.Domain.Enums;

namespace UniCliqueBackend.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IAdminRepository _adminRepository;
        private readonly IUserRepository _userRepository;
        private readonly IFriendshipRepository _friendshipRepository;
        private readonly IReportRepository _reportRepository;
        private readonly IPasswordHasher _passwordHasher;

        public UserService(IAdminRepository adminRepository, IUserRepository userRepository, IFriendshipRepository friendshipRepository, IReportRepository reportRepository, IPasswordHasher passwordHasher)
        {
            _adminRepository = adminRepository;
            _userRepository = userRepository;
            _friendshipRepository = friendshipRepository;
            _reportRepository = reportRepository;
            _passwordHasher = passwordHasher;
        }

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync(int pageNumber, int pageSize)
        {
            var users = await _adminRepository.GetAllUsersAsync(pageNumber, pageSize);
            
            return users.Select(u => new UserDto
            {
                Id = u.Id.ToString(),
                FullName = u.FullName,
                Email = u.Email,
                Username = u.Username,
                Role = u.Role,
                IsStudent = u.IsStudent,
                StudentVerificationStatus = u.StudentVerificationStatus,
                StudentDocumentUrl = u.StudentDocumentUrl,
                StudentVerifiedAt = u.StudentVerifiedAt,
                IsEmailVerified = u.IsEmailVerified,
                IsActive = u.IsActive,
                IsBanned = u.IsBanned,
                CreatedAt = u.CreatedAt
            });
        }

        public async Task<UserDto?> GetUserByIdAsync(string id)
        {
            if (!Guid.TryParse(id, out var userId)) return null;

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return null;

            return new UserDto
            {
                Id = user.Id.ToString(),
                FullName = user.FullName,
                Email = user.Email,
                Username = user.Username,
                Role = user.Role,
                IsStudent = user.IsStudent,
                StudentVerificationStatus = user.StudentVerificationStatus,
                StudentDocumentUrl = user.StudentDocumentUrl,
                StudentVerifiedAt = user.StudentVerifiedAt,
                IsEmailVerified = user.IsEmailVerified,
                IsActive = user.IsActive,
                IsBanned = user.IsBanned,
                CreatedAt = user.CreatedAt
            };
        }

        public async Task<bool> UpdateUserRoleAsync(string id, UpdateUserRoleDto model, string adminId)
        {
            if (!Guid.TryParse(id, out var userId)) return false;

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return false;

            var oldRole = user.Role;
            user.Role = model.NewRole;

            await _userRepository.UpdateAsync(user);

            // Audit Log
            var auditLog = new AuditLog
            {
                UserId = adminId,
                TargetUserId = id,
                Action = "ROLE_CHANGE",
                Details = $"Changed role from {oldRole} to {model.NewRole}",
                CreatedAt = DateTime.UtcNow
            };
            await _adminRepository.AddAuditLogAsync(auditLog);

            return true;
        }

        public async Task<bool> UpdateUserStatusAsync(string id, UpdateUserStatusDto model, string adminId)
        {
            if (!Guid.TryParse(id, out var userId)) return false;

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return false;

            var changes = new List<string>();

            if (model.IsActive.HasValue)
            {
                user.IsActive = model.IsActive.Value;
                changes.Add($"IsActive: {model.IsActive.Value}");
            }

            if (model.IsBanned.HasValue)
            {
                user.IsBanned = model.IsBanned.Value;
                changes.Add($"IsBanned: {model.IsBanned.Value}");
            }

            if (model.IsDeleted.HasValue && model.IsDeleted.Value)
            {
                user.IsDeleted = true;
                user.DeletedAt = DateTime.UtcNow;
                changes.Add("Soft Deleted");
            }
            else if (model.IsDeleted.HasValue && !model.IsDeleted.Value)
            {
                user.IsDeleted = false;
                user.DeletedAt = null; // Restore
                 changes.Add("Restored");
            }

            await _userRepository.UpdateAsync(user);

            // Audit
            var auditLog = new AuditLog
            {
                UserId = adminId,
                TargetUserId = id,
                Action = "STATUS_CHANGE",
                Details = string.Join(", ", changes),
                CreatedAt = DateTime.UtcNow
            };
            await _adminRepository.AddAuditLogAsync(auditLog);

            return true;
        }
        // User Profile Methods
        public async Task<UserProfileDto?> GetUserProfileAsync(string targetId, string currentId)
        {
            if (!Guid.TryParse(targetId, out var targetUserId) || !Guid.TryParse(currentId, out var currentUserId)) 
                return null;

            var user = await _userRepository.GetByIdAsync(targetUserId);
            if (user == null) return null;

            var friendship = await _friendshipRepository.GetFriendshipAsync(targetUserId, currentUserId);
            bool isFriend = friendship != null && friendship.Status == FriendshipStatus.Accepted;
            bool isOwnProfile = targetUserId == currentUserId;

            var computedStatus = user.StudentVerificationStatus;
            if (user.IsStudent && !string.IsNullOrWhiteSpace(user.StudentDocumentUrl) && computedStatus == StudentVerificationStatus.None)
            {
                computedStatus = StudentVerificationStatus.Pending;
            }

            var dto = new UserProfileDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Username = user.Username,
                Role = user.Role,
                IsStudent = user.IsStudent,
                StudentVerificationStatus = computedStatus,
                ProfilePhotoUrl = user.ProfilePhotoUrl,
                University = user.University,
                Department = user.Department,
                InteractionScore = user.InteractionScore,
                IsEmailVerified = user.IsEmailVerified,
                IsPrivateAccount = user.IsPrivateAccount,
                FriendshipStatusWithCurrentUser = friendship?.Status
            };

            // If profile is private and they are not friends (and it's not their own profile), hide sensitive info
            if (user.IsPrivateAccount && !isFriend && !isOwnProfile)
            {
                dto.Email = "******";
                dto.PhoneNumber = "******";
                dto.Bio = "This account is private.";
                dto.BirthDate = DateTime.MinValue;
                dto.StudentDocumentUrl = null;
                dto.FriendCount = 0;
                dto.CreatedEventCount = 0;
                dto.JoinedEventCount = 0;
            }
            else
            {
                dto.Email = user.Email;
                dto.PhoneNumber = user.PhoneNumber;
                dto.Bio = user.Bio;
                dto.BirthDate = user.BirthDate;
                dto.StudentDocumentUrl = user.StudentDocumentUrl;
                
                // Fetch real counts
                var friends = await _friendshipRepository.GetFriendsAsync(targetUserId);
                dto.FriendCount = friends.Count();
                // TODO: Add created/joined event counts from respective repositories
            }

            return dto;
        }

        public async Task<bool> UpdateProfileAsync(string id, UpdateProfileDto model)
        {
            if (!Guid.TryParse(id, out var userId)) return false;

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return false;

            if (model.FullName != null) user.FullName = model.FullName;
            if (model.ProfilePhotoUrl != null) user.ProfilePhotoUrl = model.ProfilePhotoUrl;
            if (model.University != null) user.University = model.University;
            if (model.Department != null) user.Department = model.Department;
            if (model.Bio != null) user.Bio = model.Bio;

            await _userRepository.UpdateAsync(user);
            return true;
        }

        public async Task<bool> ToggleProfilePrivacyAsync(string id)
        {
            if (!Guid.TryParse(id, out var userId)) return false;
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return false;

            user.IsPrivateAccount = !user.IsPrivateAccount;
            await _userRepository.UpdateAsync(user);
            return true;
        }

        public async Task<bool> ChangePasswordAsync(string id, ChangePasswordDto model)
        {
            if (!Guid.TryParse(id, out var userId)) return false;

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return false;

            if (!_passwordHasher.Verify(model.CurrentPassword, user.PasswordHash))
                return false;

            user.PasswordHash = _passwordHasher.HashPassword(model.NewPassword);
            await _userRepository.UpdateAsync(user);
            return true;
        }

        public async Task<bool> SoftDeleteAccountAsync(string id)
        {
            if (!Guid.TryParse(id, out var userId)) return false;

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return false;

            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);
            return true;
        }

        public async Task<bool> SetStudentProofAsync(string userId, string documentUrl)
        {
            if (!Guid.TryParse(userId, out var uid)) return false;
            var user = await _userRepository.GetByIdAsync(uid);
            if (user == null) return false;
            user.IsStudent = true;
            user.StudentDocumentUrl = documentUrl;
            user.StudentVerificationStatus = StudentVerificationStatus.Pending;
            user.StudentVerifiedAt = null;
            user.StudentVerificationNote = null;
            await _userRepository.UpdateAsync(user);
            return true;
        }
 
        
        

        public async Task<IEnumerable<UserDto>> GetStudentRequestsAsync(Domain.Enums.StudentVerificationStatus status)
        {
            var list = await _adminRepository.GetAllUsersAsync(1, int.MaxValue);
            var filtered = list.Where(u =>
                u.IsStudent &&
                (
                    u.StudentVerificationStatus == status ||
                    (status == StudentVerificationStatus.Pending &&
                     u.StudentVerificationStatus == StudentVerificationStatus.None &&
                     !string.IsNullOrWhiteSpace(u.StudentDocumentUrl))
                ));
            return filtered.Select(u => new UserDto
            {
                Id = u.Id.ToString(),
                FullName = u.FullName,
                Email = u.Email,
                Username = u.Username,
                Role = u.Role,
                IsStudent = u.IsStudent,
                StudentVerificationStatus = u.StudentVerificationStatus,
                StudentDocumentUrl = u.StudentDocumentUrl,
                StudentVerifiedAt = u.StudentVerifiedAt,
                IsEmailVerified = u.IsEmailVerified,
                IsActive = u.IsActive,
                IsBanned = u.IsBanned,
                CreatedAt = u.CreatedAt
            });
        }

        public async Task<bool> ApproveStudentAsync(string id, string adminId, string? note)
        {
            if (!Guid.TryParse(id, out var userId)) return false;
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return false;
            user.StudentVerificationStatus = Domain.Enums.StudentVerificationStatus.Approved;
            user.StudentVerifiedAt = DateTime.UtcNow;
            user.StudentVerificationNote = note;
            await _userRepository.UpdateAsync(user);
            var auditLog = new AuditLog
            {
                UserId = adminId,
                TargetUserId = id,
                Action = "STUDENT_APPROVE",
                Details = note ?? "",
                CreatedAt = DateTime.UtcNow
            };
            await _adminRepository.AddAuditLogAsync(auditLog);
            return true;
        }

        public async Task<bool> RejectStudentAsync(string id, string adminId, string note)
        {
            if (!Guid.TryParse(id, out var userId)) return false;
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return false;
            user.StudentVerificationStatus = Domain.Enums.StudentVerificationStatus.Rejected;
            user.StudentVerifiedAt = DateTime.UtcNow;
            user.StudentVerificationNote = note;
            user.IsBanned = true;
            await _userRepository.UpdateAsync(user);
            var auditLog = new AuditLog
            {
                UserId = adminId,
                TargetUserId = id,
                Action = "STUDENT_REJECT",
                Details = note,
                CreatedAt = DateTime.UtcNow
            };
            await _adminRepository.AddAuditLogAsync(auditLog);
            return true;
        }
    }
}
