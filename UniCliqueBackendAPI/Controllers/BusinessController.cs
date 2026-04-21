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

        [HttpGet("stats")]
        [Authorize(Roles = "Business,Admin")]
        public async Task<IActionResult> GetStats()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var stats = await _businessService.GetBusinessStatsAsync(userId);
            return Ok(stats);
        }

        // Admin Endpoints
        [HttpPost("admin/create")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateBusiness([FromBody] AdminCreateBusinessDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var password = await _businessService.AdminCreateBusinessAsync(model);
                return Ok(new { 
                    Message = "İşletme başarıyla oluşturuldu.", 
                    TemporaryPassword = password,
                    BusinessEmail = model.Email
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
