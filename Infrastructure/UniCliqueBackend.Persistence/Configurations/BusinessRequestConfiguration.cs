using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniCliqueBackend.Domain.Entities;

namespace UniCliqueBackend.Persistence.Configurations
{
    public class BusinessRequestConfiguration : IEntityTypeConfiguration<BusinessRequest>
    {
        public void Configure(EntityTypeBuilder<BusinessRequest> builder)
        {
            builder.ToTable("BusinessRequests");
            builder.HasQueryFilter(br => !br.IsDeleted && !br.User.IsDeleted);
        }
    }
}
