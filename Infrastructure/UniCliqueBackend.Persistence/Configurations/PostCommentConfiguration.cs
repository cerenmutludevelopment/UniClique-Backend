using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniCliqueBackend.Domain.Entities;

namespace UniCliqueBackend.Persistence.Configurations
{
    public class PostCommentConfiguration : IEntityTypeConfiguration<PostComment>
    {
        public void Configure(EntityTypeBuilder<PostComment> builder)
        {
            builder.ToTable("PostComments");
            builder.HasKey(pc => pc.Id);

            builder.HasOne(pc => pc.Post)
                   .WithMany(p => p.Comments)
                   .HasForeignKey(pc => pc.PostId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(pc => pc.User)
                   .WithMany(u => u.PostComments)
                   .HasForeignKey(pc => pc.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
