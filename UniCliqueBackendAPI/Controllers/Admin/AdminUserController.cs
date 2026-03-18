using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UniCliqueBackend.Application.DTOs.Admin.User;
using UniCliqueBackend.Application.Interfaces.Services;
using UniCliqueBackend.Domain.Enums;

namespace UniCliqueBackendAPI.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(Roles = "Admin")]
    public class AdminUserController : ControllerBase
    {
        private readonly IUserService _userService;

        public AdminUserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("student-requests")]
        public async Task<IActionResult> GetStudentRequests([FromQuery] StudentVerificationStatus status = StudentVerificationStatus.Pending)
        {
            var list = await _userService.GetStudentRequestsAsync(status);
            foreach (var u in list)
            {
                u.StudentDocumentUrl = ToAbsolute(u.StudentDocumentUrl);
            }
            return Ok(list);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var users = await _userService.GetAllUsersAsync(pageNumber, pageSize);
            foreach (var u in users)
            {
                u.StudentDocumentUrl = ToAbsolute(u.StudentDocumentUrl);
            }
            return Ok(users);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(string id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null) return NotFound("User not found.");
            user.StudentDocumentUrl = ToAbsolute(user.StudentDocumentUrl);
            return Ok(user);
        }

        [HttpPut("{id}/role")]
        public async Task<IActionResult> UpdateUserRole(string id, [FromBody] UpdateUserRoleDto model)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (adminId == null) return Unauthorized();

            var result = await _userService.UpdateUserRoleAsync(id, model, adminId);
            if (!result) return BadRequest("Failed to update role or user not found.");

            return Ok("User role updated successfully.");
        }

        [HttpPut("{id}/student-approve")]
        public async Task<IActionResult> ApproveStudent(string id, [FromBody] ReviewStudentDto model)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (adminId == null) return Unauthorized();
            var ok = await _userService.ApproveStudentAsync(id, adminId, model?.Note);
            if (!ok) return BadRequest("Failed to approve student or user not found.");
            return Ok("Student approved.");
        }

        [HttpPut("{id}/student-reject")]
        public async Task<IActionResult> RejectStudent(string id, [FromBody] ReviewStudentDto model)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (adminId == null) return Unauthorized();
            var note = model?.Note ?? "No reason provided.";
            var ok = await _userService.RejectStudentAsync(id, adminId, note);
            if (!ok) return BadRequest("Failed to reject student or user not found.");
            return Ok("Student rejected.");
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateUserStatus(string id, [FromBody] UpdateUserStatusDto model)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (adminId == null) return Unauthorized();

            var result = await _userService.UpdateUserStatusAsync(id, model, adminId);
            if (!result) return BadRequest("Failed to update status or user not found.");

            return Ok("User status updated successfully.");
        }

        private string? ToAbsolute(string? rel)
        {
            if (string.IsNullOrWhiteSpace(rel)) return rel;
            if (rel.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || rel.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return rel;
            if (!rel.StartsWith("/")) rel = "/" + rel;
            return $"{Request.Scheme}://{Request.Host}{rel}";
        }
    }
}
