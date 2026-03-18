using Microsoft.EntityFrameworkCore;
using UniCliqueBackend.Domain.Entities;
using UniCliqueBackend.Persistence.Configurations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace UniCliqueBackend.Persistence.Contexts
{
    public class AppDbContext : DbContext
    {
        // 🔹 DbSet'ler
        public DbSet<User> Users => Set<User>();
        public DbSet<UserConsent> UserConsents => Set<UserConsent>();
        public DbSet<UserVerificationCode> UserVerificationCodes => Set<UserVerificationCode>();
        public DbSet<UserRefreshToken> UserRefreshTokens => Set<UserRefreshToken>();
        public DbSet<UserExternalLogin> UserExternalLogins => Set<UserExternalLogin>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<Friendship> Friendships => Set<Friendship>();
        public DbSet<Event> Events => Set<Event>();
        public DbSet<EventParticipant> EventParticipants => Set<EventParticipant>();
        public DbSet<Post> Posts => Set<Post>();
        public DbSet<BusinessRequest> BusinessRequests => Set<BusinessRequest>();

        // 🔹 Constructor
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // 🔹 Model Configuration
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configuration sınıfları
            modelBuilder.ApplyConfiguration(new UserConfiguration());
            modelBuilder.ApplyConfiguration(new UserConsentConfiguration());
            modelBuilder.ApplyConfiguration(new UserVerificationCodeConfiguration());
            modelBuilder.ApplyConfiguration(new UserRefreshTokenConfiguration());
            modelBuilder.ApplyConfiguration(new UserExternalLoginConfiguration());
            modelBuilder.ApplyConfiguration(new FriendshipConfiguration());
            modelBuilder.ApplyConfiguration(new EventConfiguration());
            modelBuilder.ApplyConfiguration(new EventParticipantConfiguration());
            modelBuilder.ApplyConfiguration(new PostConfiguration());
            modelBuilder.ApplyConfiguration(new BusinessRequestConfiguration());

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime))
                    {
                        property.SetValueConverter(
                            new ValueConverter<DateTime, DateTime>(
                                v => v.Kind == DateTimeKind.Utc
                                    ? v
                                    : DateTime.SpecifyKind(v, DateTimeKind.Utc),
                                v => DateTime.SpecifyKind(v, DateTimeKind.Utc)
                            )
                        );
                    }

                    if (property.ClrType == typeof(DateTime?))
                    {
                        property.SetValueConverter(
                            new ValueConverter<DateTime?, DateTime?>(
                                v => v.HasValue
                                    ? (v.Value.Kind == DateTimeKind.Utc
                                        ? v
                                        : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc))
                                    : v,
                                v => v.HasValue
                                    ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc)
                                    : v
                            )
                        );
                    }
                }
            }



            base.OnModelCreating(modelBuilder);
        }

        // 🔹 Audit alanlarını otomatik yönetmek için (opsiyonel ama ÇOK faydalı)
        public override async Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker.Entries()
                .Where(e =>
                    e.Entity is UniCliqueBackend.Domain.Common.BaseEntity &&
                    (e.State == EntityState.Added ||
                     e.State == EntityState.Modified ||
                     e.State == EntityState.Deleted));

            foreach (var entry in entries)
            {
                var entity = (UniCliqueBackend.Domain.Common.BaseEntity)entry.Entity;

                if (entry.State == EntityState.Added && entity.CreatedAt == default)
                {
                    entity.CreatedAt = DateTime.UtcNow;
                }


                if (entry.State == EntityState.Modified)
                {
                    entity.UpdatedAt = DateTime.UtcNow;
                }

                // Soft delete
                if (entry.State == EntityState.Deleted)
                {
                    entry.State = EntityState.Modified;
                    entity.IsDeleted = true;
                    entity.DeletedAt = DateTime.UtcNow;
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}
