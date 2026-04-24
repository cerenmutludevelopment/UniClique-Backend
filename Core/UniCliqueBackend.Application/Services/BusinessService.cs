using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UniCliqueBackend.Application.DTOs.Business;
using UniCliqueBackend.Application.DTOs.Common;
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

        public async Task<Guid> AdminCreateBusinessAsync(AdminCreateBusinessDto model)
        {
            // 1. Check if user already exists
            var existing = await _userRepository.GetByEmailAsync(model.Email);
            if (existing != null) throw new Exception("Bir işletme veya kullanıcı bu e-posta adresiyle zaten kayıtlı.");

            // No need for a real password since businesses won't log in
            var passwordHash = _passwordHasher.HashPassword(Guid.NewGuid().ToString("N"));

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
            
            // Password is not returned anymore as per user request
            return user.Id;
        }

        public async Task<Guid> UpdateBusinessAsync(Guid userId, UpdateBusinessDto model)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || user.Role != RoleType.Business) throw new Exception("İşletme bulunamadı.");

            var detail = await _businessRepository.GetBusinessDetailByUserIdAsync(userId);
            if (detail == null) throw new Exception("İşletme detayları bulunamadı.");

            // Update User fields (Partial)
            if (model.BusinessName != null) user.FullName = model.BusinessName;
            if (model.PhoneNumber != null) user.PhoneNumber = model.PhoneNumber;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            // Update Detail fields (Partial)
            if (model.BusinessName != null) detail.BusinessName = model.BusinessName;
            if (model.OpeningHours.HasValue) detail.OpeningHours = model.OpeningHours.Value;
            if (model.ClosingHours.HasValue) detail.ClosingHours = model.ClosingHours.Value;
            if (model.Activities != null) detail.Activities = model.Activities;
            if (model.City != null) detail.City = model.City;
            if (model.Address != null) detail.Address = model.Address;
            if (model.Description != null) detail.Description = model.Description;
            if (model.PhotoUrls != null) detail.PhotoUrls = string.Join(",", model.PhotoUrls);
            
            detail.UpdatedAt = DateTime.UtcNow;

            await _businessRepository.UpdateBusinessDetailAsync(detail);

            return user.Id;
        }

        public async Task DeleteBusinessAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || user.Role != RoleType.Business) throw new Exception("İşletme bulunamadı.");

            var detail = await _businessRepository.GetBusinessDetailByUserIdAsync(userId);
            if (detail != null)
            {
                detail.IsDeleted = true;
                detail.DeletedAt = DateTime.UtcNow;
                await _businessRepository.UpdateBusinessDetailAsync(detail);
            }

            // Soft delete user as per user request to prevent re-registration with same email
            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);
        }

        public async Task<PaginatedResultDto<BusinessListDto>> GetAllBusinessesAsync(string? cursor, int pageSize, string? searchTerm)
        {
            var (items, nextCursor) = await _businessRepository.GetPaginatedBusinessDetailsAsync(cursor, pageSize, searchTerm);
            
            var dtos = items.Select(d => new BusinessListDto
            {
                UserId = d.UserId,
                BusinessName = d.BusinessName,
                Description = d.Description,
                City = d.City,
                Address = d.Address,
                OpeningHours = d.OpeningHours,
                ClosingHours = d.ClosingHours,
                Activities = d.Activities,
                PhotoUrls = d.PhotoUrls != null 
                    ? d.PhotoUrls.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() 
                    : new List<string>()
            }).ToList();

            return new PaginatedResultDto<BusinessListDto>
            {
                Items = dtos,
                NextCursor = nextCursor,
                HasNextPage = nextCursor != null
            };
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
        }
    }
