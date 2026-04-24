using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using UniCliqueBackend.Application.DTOs.Business;
using UniCliqueBackend.Application.Interfaces.Services;

namespace UniCliqueBackendAPI.Controllers
{
    [ApiController]
    [Route("api/business")]
    [Authorize]
    public class BusinessController : ControllerBase
    {
        private readonly IBusinessService _businessService;

        public BusinessController(IBusinessService businessService)
        {
            _businessService = businessService;
        }

        [HttpGet("stats/{userId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetStats(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return BadRequest("UserId is required.");

            var stats = await _businessService.GetBusinessStatsAsync(userId);
            return Ok(stats);
        }

        [HttpGet("all")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll([FromQuery] string? cursor = null, [FromQuery] int pageSize = 20, [FromQuery] string? searchTerm = null)
        {
            var result = await _businessService.GetAllBusinessesAsync(cursor, pageSize, searchTerm);
            return Ok(result);
        }

        // Admin Endpoints
        [HttpPost("admin/create")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateBusiness([FromBody] AdminCreateBusinessDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var businessId = await _businessService.AdminCreateBusinessAsync(model);
                return Ok(new { 
                    Message = "İşletme başarıyla oluşturuldu.", 
                    BusinessId = businessId,
                    BusinessName = model.BusinessName
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPatch("admin/{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateBusiness(Guid userId, [FromBody] UpdateBusinessDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var id = await _businessService.UpdateBusinessAsync(userId, model);
                return Ok(new { 
                    Message = "İşletme başarıyla güncellendi.",
                    BusinessId = id
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("admin/{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteBusiness(Guid userId)
        {
            try
            {
                await _businessService.DeleteBusinessAsync(userId);
                return Ok(new { Message = "İşletme başarıyla silindi." });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
