using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniCliqueBackend.Domain.Entities;

namespace UniCliqueBackend.Persistence.Configurations
{
    public class FriendshipConfiguration : IEntityTypeConfiguration<Friendship>
    {
        public void Configure(EntityTypeBuilder<Friendship> builder)
        {
            builder.HasKey(f => f.Id);

            // Requester Relationship
            builder.HasOne(f => f.Requester)
                .WithMany(u => u.UseSentFriendRequests)
                .HasForeignKey(f => f.RequesterId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascading delete to avoid cycles

            // Addressee Relationship
            builder.HasOne(f => f.Addressee)
                .WithMany(u => u.UserReceivedFriendRequests)
                .HasForeignKey(f => f.AddresseeId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascading delete to avoid cycles

            builder.HasQueryFilter(f => !f.IsDeleted && !f.Requester.IsDeleted && !f.Addressee.IsDeleted);
        }
    }
}
