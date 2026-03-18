using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniCliqueBackend.Domain.Entities;

namespace UniCliqueBackend.Persistence.Configurations
{
    public class EventParticipantConfiguration : IEntityTypeConfiguration<EventParticipant>
    {
        public void Configure(EntityTypeBuilder<EventParticipant> builder)
        {
            builder.ToTable("EventParticipants");
            builder.HasQueryFilter(ep =>
                !ep.IsDeleted &&
                !ep.User.IsDeleted &&
                !ep.Event.IsDeleted);
        }
    }
}
