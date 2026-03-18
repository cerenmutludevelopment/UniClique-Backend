using System.Collections.Generic;
using System.Threading.Tasks;
using UniCliqueBackend.Application.DTOs.Admin.User;
using UniCliqueBackend.Application.DTOs.User;

namespace UniCliqueBackend.Application.Interfaces.Services
{
    public interface IUserService
    {
        Task<IEnumerable<UserDto>> GetAllUsersAsync(int pageNumber, int pageSize);
        Task<UserDto?> GetUserByIdAsync(string id);
        Task<bool> UpdateUserRoleAsync(string id, UpdateUserRoleDto model, string adminId);
        Task<bool> UpdateUserStatusAsync(string id, UpdateUserStatusDto model, string adminId);

        // User Profile Methods
        Task<UserProfileDto?> GetUserProfileAsync(string userId);
        Task<bool> UpdateProfileAsync(string userId, UpdateProfileDto model);
        Task<bool> ChangePasswordAsync(string userId, ChangePasswordDto model);
        Task<bool> SoftDeleteAccountAsync(string userId);
        Task<IEnumerable<UserDto>> GetStudentRequestsAsync(UniCliqueBackend.Domain.Enums.StudentVerificationStatus status);
        Task<bool> ApproveStudentAsync(string id, string adminId, string? note);
        Task<bool> RejectStudentAsync(string id, string adminId, string note);
    }
}
