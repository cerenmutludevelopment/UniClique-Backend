using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UniCliqueBackend.Application.Interfaces.Repositories;
using UniCliqueBackend.Domain.Entities;
using UniCliqueBackend.Persistence.Contexts;

namespace UniCliqueBackend.Persistence.Repositories
{
    public class BusinessRepository : IBusinessRepository
    {
        private readonly AppDbContext _context;

        public BusinessRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddBusinessDetailAsync(BusinessDetail detail)
        {
            await _context.BusinessDetails.AddAsync(detail);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateBusinessDetailAsync(BusinessDetail detail)
        {
            _context.BusinessDetails.Update(detail);
            await _context.SaveChangesAsync();
        }

        public async Task<BusinessDetail?> GetBusinessDetailByUserIdAsync(Guid userId)
        {
            return await _context.BusinessDetails
                .FirstOrDefaultAsync(b => b.UserId == userId);
        }

        public async Task<int> GetTotalEventsAsync(Guid ownerId)
        {
            return await _context.Events.CountAsync(e => e.OwnerId == ownerId);
        }

        public async Task<int> GetTotalParticipantsAsync(Guid ownerId)
        {
            return await _context.Events
                .Where(e => e.OwnerId == ownerId)
                .SumAsync(e => e.CurrentParticipantsCount);
        }

        public async Task<int> GetActiveEventsAsync(Guid ownerId)
        {
            return await _context.Events
                .CountAsync(e => e.OwnerId == ownerId && !e.IsCancelled && e.EndDate > DateTime.UtcNow);
        }
    }
}
