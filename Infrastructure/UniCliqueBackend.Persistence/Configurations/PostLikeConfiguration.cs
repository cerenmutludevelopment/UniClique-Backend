using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniCliqueBackend.Domain.Entities;

namespace UniCliqueBackend.Persistence.Configurations
{
    public class PostLikeConfiguration : IEntityTypeConfiguration<PostLike>
    {
        public void Configure(EntityTypeBuilder<PostLike> builder)
        {
            builder.ToTable("PostLikes");
            builder.HasKey(pl => pl.Id);

            builder.HasOne(pl => pl.Post)
                   .WithMany(p => p.Likes)
                   .HasForeignKey(pl => pl.PostId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(pl => pl.User)
                   .WithMany(u => u.PostLikes)
                   .HasForeignKey(pl => pl.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
