// src/ArchiveSystem.Infrastructure/Persistence/Configurations/UserCaseAccessConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ArchiveSystem.Infrastructure.Identity;

namespace ArchiveSystem.Infrastructure.Persistence.Configurations
{
    public class UserCaseAccessConfiguration : IEntityTypeConfiguration<UserCaseAccess>
    {
        public void Configure(EntityTypeBuilder<UserCaseAccess> builder)
        {
            builder.HasKey(uc => new { uc.UserId, uc.CaseId });

            builder.Property(uc => uc.Permissions)
                .HasMaxLength(255)
                .IsRequired();

            builder.HasOne(uc => uc.User)
                .WithMany(u => u.UserCaseAccesses)
                .HasForeignKey(uc => uc.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(uc => uc.Case)
                .WithMany()
                .HasForeignKey(uc => uc.CaseId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Property(uc => uc.CreatedAt)
                .IsRequired();
        }
    }
}