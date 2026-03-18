using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniCliqueBackend.Domain.Entities;

namespace UniCliqueBackend.Persistence.Configurations
{
    public class EventConfiguration : IEntityTypeConfiguration<Event>
    {
        public void Configure(EntityTypeBuilder<Event> builder)
        {
            builder.ToTable("Events");
            builder.HasQueryFilter(e => !e.IsDeleted && !e.Owner.IsDeleted);
        }
    }
}
