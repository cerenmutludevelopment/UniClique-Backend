using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniCliqueBackend.Domain.Entities;

namespace UniCliqueBackend.Persistence.Configurations
{
    public class PostConfiguration : IEntityTypeConfiguration<Post>
    {
        public void Configure(EntityTypeBuilder<Post> builder)
        {
            builder.ToTable("Posts");
            builder.HasQueryFilter(p =>
                !p.IsDeleted &&
                !p.User.IsDeleted &&
                !p.Event.IsDeleted);
        }
    }
}
