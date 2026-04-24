using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UniCliqueBackend.Application.DTOs.Business;
using UniCliqueBackend.Application.DTOs.Common;

namespace UniCliqueBackend.Application.Interfaces.Services
{
    public interface IBusinessService
    {
        // Admin
        Task<Guid> AdminCreateBusinessAsync(AdminCreateBusinessDto model);
        Task<Guid> UpdateBusinessAsync(Guid userId, UpdateBusinessDto model);
        Task DeleteBusinessAsync(Guid userId);
        
        // Stats
        Task<BusinessStatsDto> GetBusinessStatsAsync(string userId);

        // Listing
        Task<PaginatedResultDto<BusinessListDto>> GetAllBusinessesAsync(string? cursor, int pageSize, string? searchTerm);
    }
}
