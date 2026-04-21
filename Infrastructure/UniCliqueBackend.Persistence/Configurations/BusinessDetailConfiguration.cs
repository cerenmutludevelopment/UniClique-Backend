using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniCliqueBackend.Domain.Entities;

namespace UniCliqueBackend.Persistence.Configurations
{
    public class BusinessDetailConfiguration : IEntityTypeConfiguration<BusinessDetail>
    {
        public void Configure(EntityTypeBuilder<BusinessDetail> builder)
        {
            builder.HasKey(bd => bd.Id);
            
            // Set Id to be same as UserId for 1:1 if desired, or just use a separate PK
            // Here we use Id as separate PK but UserId as unique FK
            
            builder.HasIndex(bd => bd.UserId).IsUnique();

            builder.Property(bd => bd.BusinessName)
                   .IsRequired()
                   .HasMaxLength(200);

            builder.Property(bd => bd.OpeningHours);

            builder.Property(bd => bd.ClosingHours);

            builder.Property(bd => bd.Activities)
                   .HasMaxLength(1000);

            builder.Property(bd => bd.City)
                   .HasMaxLength(100);

            builder.Property(bd => bd.Address)
                   .HasMaxLength(500);

            builder.Property(bd => bd.Description)
                   .HasMaxLength(2000);

            builder.Property(bd => bd.PhotoUrls)
                   .HasMaxLength(2000);

            // 1:1 Relationship
            builder.HasOne(bd => bd.User)
                   .WithOne(u => u.BusinessDetail)
                   .HasForeignKey<BusinessDetail>(bd => bd.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
