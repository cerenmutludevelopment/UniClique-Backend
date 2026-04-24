using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UniCliqueBackend.Domain.Entities;

namespace UniCliqueBackend.Application.Interfaces.Repositories
{
    public interface IBusinessRepository
    {
        Task AddBusinessDetailAsync(BusinessDetail detail);
        Task UpdateBusinessDetailAsync(BusinessDetail detail);
        Task<BusinessDetail?> GetBusinessDetailByUserIdAsync(Guid userId);
        
        // Stats helpers
        Task<int> GetTotalEventsAsync(Guid ownerId);
        Task<int> GetTotalParticipantsAsync(Guid ownerId);
        Task<int> GetActiveEventsAsync(Guid ownerId);
        Task<List<BusinessDetail>> GetAllBusinessDetailsAsync();
        Task<(List<BusinessDetail> Items, string? NextCursor)> GetPaginatedBusinessDetailsAsync(string? cursor, int pageSize, string? searchTerm);
        Task DeleteBusinessDetailAsync(BusinessDetail detail);
    }
}
