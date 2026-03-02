using Microsoft.EntityFrameworkCore;
using UniCliqueBackend.Application.Interfaces.Repositories;
using UniCliqueBackend.Domain.Entities;
using UniCliqueBackend.Domain.Enums;
using UniCliqueBackend.Persistence.Contexts;

namespace UniCliqueBackend.Persistence.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddRefreshTokenAsync(Guid userId, UserRefreshToken token)
        {
            token.UserId = userId;
            _context.UserRefreshTokens.Add(token);
            await _context.SaveChangesAsync();
        }

        public async Task AddAsync(User user)
        {
            _context.Users.Add(user);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                if (!string.IsNullOrWhiteSpace(msg))
                {
                    var l = msg.ToLowerInvariant();
                    if (l.Contains("ix_users_email") || l.Contains("users_email_key"))
                        throw new Exception("User with this email or username already exists.");
                    if (l.Contains("ix_users_username") || l.Contains("users_username_key"))
                        throw new Exception("User with this email or username already exists.");
                    if (l.Contains("ix_users_phonenumber") || l.Contains("users_phonenumber_key"))
                        throw new Exception("Phone already exists.");
                }
                throw;
            }
        }

        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                if (!string.IsNullOrWhiteSpace(msg))
                {
                    var l = msg.ToLowerInvariant();
                    if (l.Contains("ix_users_email") || l.Contains("users_email_key"))
                        throw new Exception("User with this email or username already exists.");
                    if (l.Contains("ix_users_username") || l.Contains("users_username_key"))
                        throw new Exception("User with this email or username already exists.");
                    if (l.Contains("ix_users_phonenumber") || l.Contains("users_phonenumber_key"))
                        throw new Exception("Phone already exists.");
                }
                throw;
            }
        }

        public async Task<UserRefreshToken?> GetRefreshTokenAsync(string tokenHash)
        {
            return await _context.UserRefreshTokens
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.TokenHash == tokenHash && !x.IsDeleted);
        }

        public async Task RevokeRefreshTokenAsync(UserRefreshToken token, string? replacedByTokenHash)
        {
            var entity = await _context.UserRefreshTokens.FirstOrDefaultAsync(x => x.Id == token.Id);
            if (entity != null)
            {
                entity.RevokedAt = DateTime.UtcNow;
                entity.ReplacedByTokenHash = replacedByTokenHash;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(x => x.Email == email);
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _context.Users
                .FirstOrDefaultAsync(x => x.Username == username);
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await _context.Users
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> ExistsAsync(string email, string username)
        {
            return await _context.Users
                .AnyAsync(x => x.Email == email || x.Username == username);
        }

        public async Task<IEnumerable<User>> SearchUsersAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<User>();

            query = query.Trim().ToLower();

            return await _context.Users
                .Where(u => !u.IsDeleted && !u.IsBanned &&
                            (u.Username.ToLower().Contains(query) || 
                             u.FullName.ToLower().Contains(query)))
                .Take(20) // Limit results
                .ToListAsync();
        }

        public async Task<User?> GetByExternalLoginAsync(string provider, string providerUserId)
        {
            var login = await _context.UserExternalLogins
                .Include(x => x.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Provider == provider && x.ProviderUserId == providerUserId && !x.IsDeleted);
            return login?.User;
        }

        public async Task AddExternalLoginAsync(Guid userId, UserExternalLogin externalLogin)
        {
            externalLogin.UserId = userId;
            _context.UserExternalLogins.Add(externalLogin);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                if (!string.IsNullOrWhiteSpace(msg))
                {
                    var l = msg.ToLowerInvariant();
                    if (l.Contains("userexternallogins_provider_provideruserid_key")
                        || l.Contains("ix_userexternallogins_provider_provideruserid"))
                    {
                        throw new Exception("External login already exists.");
                    }
                }
                throw;
            }
        }

        public async Task<bool> PhoneExistsAsync(string phoneE164, string phoneLocal)
        {
            return await _context.Users.AnyAsync(x =>
                x.PhoneNumber == phoneE164 || x.PhoneNumber == phoneLocal
            );
        }

        public async Task AddVerificationCodeAsync(Guid userId, UserVerificationCode code)
        {
            code.UserId = userId;
            _context.UserVerificationCodes.Add(code);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveActiveVerificationCodesAsync(Guid userId, VerificationPurpose purpose)
        {
            var codes = await _context.UserVerificationCodes
                .Where(x => x.UserId == userId && x.Purpose == purpose && !x.IsDeleted && !x.IsUsed)
                .ToListAsync();
            if (codes.Count > 0)
            {
                _context.UserVerificationCodes.RemoveRange(codes);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<UserVerificationCode?> GetLatestVerificationCodeAsync(Guid userId, VerificationPurpose purpose)
        {
            return await _context.UserVerificationCodes
                .Where(x => x.UserId == userId && x.Purpose == purpose && !x.IsDeleted && !x.IsUsed)
                .OrderByDescending(x => x.SentAt)
                .FirstOrDefaultAsync();
        }

        public async Task UpdateVerificationCodeAsync(UserVerificationCode code)
        {
            _context.UserVerificationCodes.Update(code);
            await _context.SaveChangesAsync();
        }

        public async Task ClearAllUserDataAsync()
        {
            // PROPER HARD DELETE SEQUENCE FOR NON-ADMIN USERS
            // We use raw SQL to bypass Soft Delete interception in SaveChangesAsync
            // and to handle dependencies manually where Cascade might not be set.
            
            var sql = @"
                -- 1. Dependent User Data
                DELETE FROM ""UserRefreshTokens"" WHERE ""UserId"" IN (SELECT ""Id"" FROM ""Users"" WHERE ""Role"" != 2);
                DELETE FROM ""UserVerificationCodes"" WHERE ""UserId"" IN (SELECT ""Id"" FROM ""Users"" WHERE ""Role"" != 2);
                DELETE FROM ""UserConsents"" WHERE ""UserId"" IN (SELECT ""Id"" FROM ""Users"" WHERE ""Role"" != 2);
                DELETE FROM ""UserExternalLogins"" WHERE ""UserId"" IN (SELECT ""Id"" FROM ""Users"" WHERE ""Role"" != 2);
                DELETE FROM ""BusinessRequests"" WHERE ""UserId"" IN (SELECT ""Id"" FROM ""Users"" WHERE ""Role"" != 2);

                -- 2. Friendships (If either party is a target user, delete the link)
                DELETE FROM ""Friendships"" WHERE ""RequesterId"" IN (SELECT ""Id"" FROM ""Users"" WHERE ""Role"" != 2) 
                    OR ""AddresseeId"" IN (SELECT ""Id"" FROM ""Users"" WHERE ""Role"" != 2);

                -- 3. Posts (Delete posts made by target users)
                DELETE FROM ""Posts"" WHERE ""UserId"" IN (SELECT ""Id"" FROM ""Users"" WHERE ""Role"" != 2);

                -- 4. Event Paticipation (Remove target users from events)
                DELETE FROM ""EventParticipants"" WHERE ""UserId"" IN (SELECT ""Id"" FROM ""Users"" WHERE ""Role"" != 2);

                -- 5. Events (Events organized by target users)
                -- 5.1 First delete Posts on these events (even if made by others/Admins)
                DELETE FROM ""Posts"" WHERE ""EventId"" IN (SELECT ""Id"" FROM ""Events"" WHERE ""OwnerId"" IN (SELECT ""Id"" FROM ""Users"" WHERE ""Role"" != 2));
                
                -- 5.2 Delete Participants of these events
                DELETE FROM ""EventParticipants"" WHERE ""EventId"" IN (SELECT ""Id"" FROM ""Events"" WHERE ""OwnerId"" IN (SELECT ""Id"" FROM ""Users"" WHERE ""Role"" != 2));
                
                -- 5.3 Delete the Events themselves
                DELETE FROM ""Events"" WHERE ""OwnerId"" IN (SELECT ""Id"" FROM ""Users"" WHERE ""Role"" != 2);

                -- 6. Finally, delete the Users
                DELETE FROM ""Users"" WHERE ""Role"" != 2;
            ";

            await _context.Database.ExecuteSqlRawAsync(sql);
        }

        public async Task HardDeleteUserByIdAsync(Guid userId)
        {
            var sql = @"
                DELETE FROM ""UserRefreshTokens"" WHERE ""UserId"" = {0};
                DELETE FROM ""UserVerificationCodes"" WHERE ""UserId"" = {0};
                DELETE FROM ""UserConsents"" WHERE ""UserId"" = {0};
                DELETE FROM ""UserExternalLogins"" WHERE ""UserId"" = {0};
                DELETE FROM ""BusinessRequests"" WHERE ""UserId"" = {0};
                DELETE FROM ""Friendships"" WHERE ""RequesterId"" = {0} OR ""AddresseeId"" = {0};
                DELETE FROM ""Posts"" WHERE ""UserId"" = {0};
                DELETE FROM ""EventParticipants"" WHERE ""UserId"" = {0};
                DELETE FROM ""Posts"" WHERE ""EventId"" IN (SELECT ""Id"" FROM ""Events"" WHERE ""OwnerId"" = {0});
                DELETE FROM ""EventParticipants"" WHERE ""EventId"" IN (SELECT ""Id"" FROM ""Events"" WHERE ""OwnerId"" = {0});
                DELETE FROM ""Events"" WHERE ""OwnerId"" = {0};
                DELETE FROM ""Users"" WHERE ""Id"" = {0};
            ";
            await _context.Database.ExecuteSqlRawAsync(sql, userId);
        }
    }
}
