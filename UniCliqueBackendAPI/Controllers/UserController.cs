using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UniCliqueBackend.Application.DTOs.User;
using UniCliqueBackend.Application.Interfaces.Services;
using Microsoft.AspNetCore.Http;

namespace UniCliqueBackendAPI.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IFileStorageService _fileStorage;

        public UserController(IUserService userService, IFileStorageService fileStorage)
        {
            _userService = userService;
            _fileStorage = fileStorage;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var profile = await _userService.GetUserProfileAsync(userId);
            if (profile == null) return NotFound("User not found.");

            return Ok(profile);
        }

        [HttpPut("me")]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileDto model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _userService.UpdateProfileAsync(userId, model);
            if (!result) return BadRequest("Failed to update profile.");

            return Ok("Profile updated successfully.");
        }

        [HttpPatch("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _userService.ChangePasswordAsync(userId, model);
            if (!result) return BadRequest("Password change failed. Check your current password.");

            return Ok("Password changed successfully.");
        }

        [HttpDelete("me")]
        public async Task<IActionResult> DeleteMyAccount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _userService.SoftDeleteAccountAsync(userId);
            if (!result) return BadRequest("Failed to delete account.");

            return Ok("Account deleted successfully.");
        }

        [HttpPost("me/student-proof")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadMyStudentProof(IFormFile file)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            if (file == null || file.Length == 0) return BadRequest("File is required.");
            var id = await _fileStorage.SaveStudentProofAsync(file.OpenReadStream(), file.ContentType, file.FileName);
            var endpoint = $"/api/auth/student-proof/{id}";
            var abs = $"{Request.Scheme}://{Request.Host}{endpoint}";
            var ok = await _userService.SetStudentProofAsync(userId, endpoint);
            if (!ok) return BadRequest("Failed to set student proof.");
            return Created(abs, new { id, url = abs });
        }
    }
}
