using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UniCliqueBackend.Application.DTOs.Business;

namespace UniCliqueBackend.Application.Interfaces.Services
{
    public interface IBusinessService
    {
        // Admin
        Task<string> AdminCreateBusinessAsync(AdminCreateBusinessDto model);
        
        // Stats
        Task<BusinessStatsDto> GetBusinessStatsAsync(string userId);
    }
}
