using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UniCliqueBackend.Application.Interfaces.Repositories;
using UniCliqueBackend.Domain.Entities;
using UniCliqueBackend.Domain.Enums;
using UniCliqueBackend.Persistence.Contexts;

namespace UniCliqueBackend.Persistence.Repositories
{
    public class FriendshipRepository : IFriendshipRepository
    {
        private readonly AppDbContext _context;

        public FriendshipRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Friendship?> GetFriendshipAsync(Guid user1Id, Guid user2Id)
        {
            return await _context.Friendships
                .FirstOrDefaultAsync(f => 
                    (f.RequesterId == user1Id && f.AddresseeId == user2Id) || 
                    (f.RequesterId == user2Id && f.AddresseeId == user1Id));
        }

        public async Task<Friendship?> GetByIdAsync(Guid id)
        {
            return await _context.Friendships
                .Include(f => f.Requester)
                .Include(f => f.Addressee)
                .FirstOrDefaultAsync(f => f.Id == id);
        }

        public async Task<IEnumerable<Friendship>> GetFriendRequestsAsync(Guid userId, bool incoming)
        {
            if (incoming)
            {
                return await _context.Friendships
                    .Include(f => f.Requester)
                    .Where(f => f.AddresseeId == userId && f.Status == FriendshipStatus.Pending)
                    .ToListAsync();
            }
            else
            {
                return await _context.Friendships
                    .Include(f => f.Addressee)
                    .Where(f => f.RequesterId == userId && f.Status == FriendshipStatus.Pending)
                    .ToListAsync();
            }
        }

        public async Task<IEnumerable<Friendship>> GetAllFriendshipsForUserAsync(Guid userId)
        {
            return await _context.Friendships
                .Where(f => f.RequesterId == userId || f.AddresseeId == userId)
                .ToListAsync();
        }

        public async Task<IEnumerable<User>> GetFriendsAsync(Guid userId)
        {
            var friendships = await _context.Friendships
                .Where(f => (f.RequesterId == userId || f.AddresseeId == userId) && f.Status == FriendshipStatus.Accepted)
                .Include(f => f.Requester)
                .Include(f => f.Addressee)
                .ToListAsync();

            var friends = friendships
                .Select(f => f.RequesterId == userId ? f.Addressee : f.Requester)
                .ToList();

            return friends;
        }

        public async Task AddAsync(Friendship friendship)
        {
            await _context.Friendships.AddAsync(friendship);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Friendship friendship)
        {
            _context.Friendships.Update(friendship);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Friendship friendship)
        {
            _context.Friendships.Remove(friendship);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> AreFriendsAsync(Guid user1Id, Guid user2Id)
        {
            return await _context.Friendships
                .AnyAsync(f => 
                    ((f.RequesterId == user1Id && f.AddresseeId == user2Id) || 
                     (f.RequesterId == user2Id && f.AddresseeId == user1Id)) 
                    && f.Status == FriendshipStatus.Accepted);
        }
    }
}
