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

        public async Task<List<BusinessDetail>> GetAllBusinessDetailsAsync()
        {
            return await _context.BusinessDetails.Where(b => !b.IsDeleted).ToListAsync();
        }

        public async Task<(List<BusinessDetail> Items, string? NextCursor)> GetPaginatedBusinessDetailsAsync(string? cursor, int pageSize, string? searchTerm)
        {
            var query = _context.BusinessDetails.Where(b => !b.IsDeleted).AsQueryable();

            // 1. Search Filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(b => 
                    b.BusinessName.ToLower().Contains(searchTerm) || 
                    (b.City != null && b.City.ToLower().Contains(searchTerm)) || 
                    (b.Address != null && b.Address.ToLower().Contains(searchTerm)));
            }

            // 2. Cursor Decoding
            if (!string.IsNullOrEmpty(cursor))
            {
                try 
                {
                    var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
                    var parts = decoded.Split('|');
                    if (parts.Length == 2)
                    {
                        var ticks = long.Parse(parts[0]);
                        var id = Guid.Parse(parts[1]);
                        var cursorDate = new DateTime(ticks, DateTimeKind.Utc);

                        // Newest first: CreatedAt < cursorDate OR (CreatedAt == cursorDate AND Id < id)
                        // Note: Guid comparison in EF can be tricky, but this is the standard logic
                        query = query.Where(b => b.CreatedAt < cursorDate || (b.CreatedAt == cursorDate && b.Id.CompareTo(id) < 0));
                    }
                }
                catch { /* Ignore invalid cursor */ }
            }

            // 3. Execution
            var items = await query
                .OrderByDescending(b => b.CreatedAt)
                .ThenByDescending(b => b.Id)
                .Take(pageSize + 1) // Take one extra to check if there's a next page
                .ToListAsync();

            bool hasNextPage = items.Count > pageSize;
            string? nextCursor = null;

            if (hasNextPage)
            {
                items.RemoveAt(pageSize);
                var lastItem = items.Last();
                var rawCursor = $"{lastItem.CreatedAt.Ticks}|{lastItem.Id}";
                nextCursor = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(rawCursor));
            }

            return (items, nextCursor);
        }

        public async Task DeleteBusinessDetailAsync(BusinessDetail detail)
        {
            _context.BusinessDetails.Remove(detail);
            await _context.SaveChangesAsync();
        }
    }
}
