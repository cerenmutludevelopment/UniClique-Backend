using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UniCliqueBackend.Application.DTOs.Business;
using UniCliqueBackend.Application.Interfaces.Repositories;
using UniCliqueBackend.Application.Interfaces.Security;
using UniCliqueBackend.Application.Interfaces.Services;
using UniCliqueBackend.Domain.Entities;
using UniCliqueBackend.Domain.Enums;

namespace UniCliqueBackend.Application.Services
{
    public class BusinessService : IBusinessService
    {
        private readonly IBusinessRepository _businessRepository;
        private readonly IUserRepository _userRepository;
        private readonly IFriendshipRepository _friendshipRepository;
        private readonly IPasswordHasher _passwordHasher;

        public BusinessService(
            IBusinessRepository businessRepository,
            IUserRepository userRepository,
            IFriendshipRepository friendshipRepository,
            IPasswordHasher passwordHasher)
        {
            _businessRepository = businessRepository;
            _userRepository = userRepository;
            _friendshipRepository = friendshipRepository;
            _passwordHasher = passwordHasher;
        }

        public async Task<string> AdminCreateBusinessAsync(AdminCreateBusinessDto model)
        {
            // 1. Check if user already exists
            var existing = await _userRepository.GetByEmailAsync(model.Email);
            if (existing != null) throw new Exception("Bir işletme veya kullanıcı bu e-posta adresiyle zaten kayıtlı.");

            // 2. Generate random password
            var password = GenerateRandomPassword(10);
            var passwordHash = _passwordHasher.HashPassword(password);

            // 3. Create User
            var user = new User
            {
                FullName = model.BusinessName,
                Email = model.Email,
                Username = model.Email.Split('@')[0] + "_" + Guid.NewGuid().ToString("N").Substring(0, 4),
                PhoneNumber = model.PhoneNumber,
                PasswordHash = passwordHash,
                Role = RoleType.Business,
                IsActive = true,
                IsEmailVerified = true, // Admin creates it, so we assume verified or skip for now
                EmailVerifiedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            await _userRepository.AddAsync(user);

            // 4. Create BusinessDetail
            var detail = new BusinessDetail
            {
                UserId = user.Id,
                BusinessName = model.BusinessName,
                OpeningHours = model.OpeningHours,
                ClosingHours = model.ClosingHours,
                Activities = model.Activities,
                City = model.City,
                Address = model.Address,
                Description = model.Description,
                PhotoUrls = model.PhotoUrls != null ? string.Join(",", model.PhotoUrls) : null,
                CreatedAt = DateTime.UtcNow
            };

            await _businessRepository.AddBusinessDetailAsync(detail);

            return password;
        }

        public async Task<BusinessStatsDto> GetBusinessStatsAsync(string userId)
        {
            if (!Guid.TryParse(userId, out var uid)) return new BusinessStatsDto();

            var totalEvents = await _businessRepository.GetTotalEventsAsync(uid);
            var totalParticipants = await _businessRepository.GetTotalParticipantsAsync(uid);
            var activeEvents = await _businessRepository.GetActiveEventsAsync(uid);
            
            // Friendship count as "Followers"
            var friends = await _friendshipRepository.GetFriendsAsync(uid);
            var friendCount = friends.Count();

            double avg = totalEvents > 0 ? (double)totalParticipants / totalEvents : 0;

            return new BusinessStatsDto
            {
                TotalEvents = totalEvents,
                TotalParticipants = totalParticipants,
                ActiveEvents = activeEvents,
                FriendshipCount = friendCount,
                AverageParticipantsPerEvent = Math.Round(avg, 2)
            };
        }

        private string GenerateRandomPassword(int length)
        {
            // Okunabilirliği artırmak için benzer karakterler (0, O, I, l) çıkarıldı.
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXabcdefghijkmnopqrstuvwx23456789!*?";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
