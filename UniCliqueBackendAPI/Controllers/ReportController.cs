using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UniCliqueBackend.Application.DTOs.Report;
using UniCliqueBackend.Application.Interfaces.Services;

namespace UniCliqueBackendAPI.Controllers
{
    [ApiController]
    [Route("api/reports")]
    [Authorize]
    public class ReportController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportController(IReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpPost("event/{eventId}")]
        public async Task<IActionResult> SubmitEventReport(Guid eventId, [FromBody] CreateReportDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _reportService.SubmitEventReportAsync(userId, eventId, model);
            if (!result) return BadRequest("Failed to submit event report.");

            return Ok("Event reported successfully.");
        }

        [HttpPost("user/{targetUserId}")]
        public async Task<IActionResult> SubmitUserReport(Guid targetUserId, [FromBody] CreateReportDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _reportService.SubmitUserReportAsync(userId, targetUserId, model);
            if (!result) return BadRequest("Failed to submit user report. Make sure you are not reporting yourself.");

            return Ok("User reported successfully.");
        }

        [HttpPost("general")]
        public async Task<IActionResult> SubmitGeneralReport([FromBody] CreateReportDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _reportService.SubmitGeneralReportAsync(userId, model);
            if (!result) return BadRequest("Failed to submit report.");

            return Ok("Report submitted successfully.");
        }
    }
}
