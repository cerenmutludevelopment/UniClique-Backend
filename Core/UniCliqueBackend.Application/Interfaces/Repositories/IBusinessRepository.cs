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
    }
}
