using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using UniCliqueBackend.Application.DTOs.Common;
using UniCliqueBackend.Application.Interfaces.Services;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using UniCliqueBackend.Application.DTOs.Auth;


namespace UniCliqueBackendAPI.Controllers
{
    [ApiController]
    [Route("api/auth")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IUserService _userService;
        private readonly IFileStorageService _fileStorage;

        public AuthController(IAuthService authService, IUserService userService, IFileStorageService fileStorage)
        {
            _authService = authService;
            _userService = userService;
            _fileStorage = fileStorage;
        }

        [HttpGet("username-available")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> UsernameAvailable([FromQuery] string username)
        {
            var ok = await _authService.IsUsernameAvailableAsync(username);
            return Ok(new { available = ok });
        }
        [HttpPost("register-student")]
        [ProducesResponseType(typeof(ApiMessageDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiMessageDto), StatusCodes.Status400BadRequest)]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> RegisterStudent([FromForm] RegisterStudentDto dto, IFormFile? studentCertificate)
        {
            string? endpoint = null;
            bool isStudent = false;
            if (studentCertificate != null && studentCertificate.Length > 0)
            {
                var id = await _fileStorage.SaveStudentProofAsync(studentCertificate.OpenReadStream(), studentCertificate.ContentType, studentCertificate.FileName);
                endpoint = $"/api/auth/student-proof/{id}";
                isStudent = true;
            }

            var fullName = $"{dto.Name} {dto.Surname}".Trim();
            var request = new RegisterRequestDto
            {
                FullName = fullName,
                Email = dto.Email,
                Username = dto.Username,
                PhoneNumber = dto.PhoneNumber,
                BirthDate = dto.BirthDate,
                Password = dto.Password,
                IsStudent = isStudent,
                StudentDocumentUrl = endpoint
            };

            var userId = await _authService.RegisterAsync(request);
            return Ok(new { message = "Registration successful. Verification code sent.", userId });
        }

        

        [HttpPost("login")]
        [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiMessageDto), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiMessageDto), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiMessageDto), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            var result = await _authService.LoginAsync(request);
            return Ok(result);
        }

        [HttpPost("refresh")]
        [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiMessageDto), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiMessageDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiMessageDto), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.RefreshToken))
                return BadRequest(new ApiMessageDto { Message = "Refresh token is required." });
            var result = await _authService.RefreshTokenAsync(request);
            return Ok(result);
        }

        [HttpPost("logout/by-refresh")]
        [ProducesResponseType(typeof(ApiMessageDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiMessageDto), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LogoutByRefresh([FromBody] RefreshTokenRequestDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.RefreshToken))
                return BadRequest(new ApiMessageDto { Message = "Refresh token is required." });
            await _authService.LogoutByRefreshAsync(request.RefreshToken);
            return Ok(new ApiMessageDto { Message = "Logout successful" });
        }
        [HttpPost("external-login/google")]
        [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiMessageDto), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ExternalLoginGoogle([FromBody] GoogleExternalLoginDto request)
        {
            var inner = new ExternalLoginRequestDto
            {
                Provider = "google",
                ProviderUserId = request.ProviderUserId,
                Email = request.Email,
                FullName = request.FullName
            };
            var result = await _authService.ExternalLoginAsync(inner);
            return Ok(result);
        }

        [HttpPost("external-login/apple")]
        [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiMessageDto), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ExternalLoginApple([FromBody] AppleExternalLoginDto request)
        {
            var inner = new ExternalLoginRequestDto
            {
                Provider = "apple",
                ProviderUserId = request.ProviderUserId,
                Email = request.Email,
                FullName = request.FullName
            };
            var result = await _authService.ExternalLoginAsync(inner);
            return Ok(result);
        }
        [HttpPost("verify-email/by-user-id")]
        [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiMessageDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiMessageDto), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> VerifyEmailByUserId([FromBody] VerifyEmailByUserIdRequestDto request)
        {
            var result = await _authService.VerifyEmailByUserIdAsync(request);
            return Ok(result);
        }
        [HttpPost("verify-email/resend")]
        [ProducesResponseType(typeof(ApiMessageDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiMessageDto), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiMessageDto), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> ResendEmail([FromBody] ResendEmailVerificationRequestDto request)
        {
            await _authService.ResendRegisterEmailVerificationAsync(request);
            return Ok(new ApiMessageDto { Message = "Verification code sent." });
        }

        [HttpPost("forgot-password/start")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiMessageDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiMessageDto), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ForgotPasswordStart([FromBody] ForgotPasswordStartRequestDto request)
        {
            var userId = await _authService.ForgotPasswordStartAsync(request);
            return Ok(new { message = "Reset code sent if account exists.", userId });
        }


        [HttpPost("forgot-password/verify/by-user-id")]
        [ProducesResponseType(typeof(ApiMessageDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiMessageDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiMessageDto), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> VerifyResetCodeByUserId([FromBody] VerifyResetByUserIdRequestDto request)
        {
            await _authService.VerifyPasswordResetCodeByUserIdAsync(request);
            return Ok(new ApiMessageDto { Message = "Code verified." });
        }


        [HttpPost("forgot-password/reset/by-user-id")]
        [ProducesResponseType(typeof(ApiMessageDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiMessageDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiMessageDto), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResetPasswordByUserId([FromBody] ResetPasswordByUserIdRequestDto request)
        {
            await _authService.ResetPasswordWithCodeByUserIdAsync(request);
            return Ok(new ApiMessageDto { Message = "Password reset successful." });
        }
        [HttpPost("reset-db-test")]
        [ProducesResponseType(typeof(ApiMessageDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> ResetDb()
        {
            await _authService.ResetDatabaseAsync();
            return Ok(new ApiMessageDto { Message = "Database reset successfully (All tables truncated)." });
        }

        [HttpDelete("test/delete-user-by-email")]
        [ProducesResponseType(typeof(ApiMessageDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiMessageDto), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteUserByEmail([FromQuery] string email)
        {
            var ok = await _authService.DeleteUserByEmailAsync(email);
            if (!ok) return NotFound(new ApiMessageDto { Message = "User not found" });
            return Ok(new ApiMessageDto { Message = "User deleted" });
        }

        

        [Authorize]
        [HttpGet("student-proof/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiMessageDto), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiMessageDto), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiMessageDto), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetStudentProof(string id)
        {
            var isAdmin = User.IsInRole("Admin");
            if (!isAdmin)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(ClaimTypes.Name);
                if (string.IsNullOrWhiteSpace(userId)) return Unauthorized(new ApiMessageDto { Message = "Unauthorized" });
                var profile = await _userService.GetUserProfileAsync(userId);
                if (profile == null) return Unauthorized(new ApiMessageDto { Message = "Unauthorized" });
                var endpoint = $"/api/auth/student-proof/{id}";
                if (!string.Equals(profile.StudentDocumentUrl, endpoint, StringComparison.OrdinalIgnoreCase))
                {
                    return Forbid();
                }
            }

            var info = _fileStorage.OpenStudentProof(id);
            if (info == null) return NotFound(new ApiMessageDto { Message = "File not found." });
            return PhysicalFile(info.Value.Path, info.Value.ContentType, enableRangeProcessing: true);
        }

    }
}
