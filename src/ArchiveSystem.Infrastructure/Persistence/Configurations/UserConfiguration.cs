// src/ArchiveSystem.Infrastructure/Persistence/Configurations/UserConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ArchiveSystem.Infrastructure.Identity;

namespace ArchiveSystem.Infrastructure.Persistence.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            builder.ToTable("AspNetUsers");

            builder.HasKey(u => u.Id);
            builder.Property(u => u.Id)
                .UseIdentityColumn()
                .ValueGeneratedOnAdd();

            builder.Property(u => u.UserName)
                .HasMaxLength(256)
                .IsRequired();

            builder.Property(u => u.Email)
                .HasMaxLength(256)
                .IsRequired();

            builder.Property(u => u.PhoneNumber)
                .HasMaxLength(20)
                .IsRequired(false);

            builder.Property(u => u.FirstName)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(u => u.LastName)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(u => u.NationalId)
                .HasMaxLength(20)
                .IsRequired(false);

            builder.Property(u => u.PersonnelCode)
                .HasMaxLength(50)
                .IsRequired(false);

            builder.Property(u => u.ActiveDuration)
                .HasColumnType("time")
                .IsRequired();

            builder.Property(u => u.AutoBackupEnabled)
                .HasDefaultValue(false)
                .IsRequired();

            builder.Property(u => u.CreatedBy)
                .HasMaxLength(100)
                .IsRequired(false);

            builder.Property(u => u.CreatedAt)
                .IsRequired();

            builder.Property(u => u.LastModified)
                .IsRequired(false);

            builder.Property(u => u.LastModifiedBy)
                .HasMaxLength(100)
                .IsRequired(false);

            builder.Ignore(u => u.ModifiedAt);

            builder.Property(u => u.LastLoginAt)
                .IsRequired(false);

            builder.Property(u => u.IsActive)
                .ValueGeneratedNever()
                .IsRequired();

            builder.Property(u => u.LastPasswordResetRequestedAt)
                .IsRequired(false);

            builder.HasIndex(u => u.UserName).HasDatabaseName("IX_Users_UserName").IsUnique();
            builder.HasIndex(u => u.Email).HasDatabaseName("IX_Users_Email").IsUnique();
            builder.HasIndex(u => u.PhoneNumber)
                .HasDatabaseName("IX_Users_PhoneNumber")
                .IsUnique()
                .HasFilter("[PhoneNumber] IS NOT NULL");
            builder.HasIndex(u => u.IsActive).HasDatabaseName("IX_Users_IsActive");
            builder.HasIndex(u => u.NationalId)
                .HasDatabaseName("IX_Users_NationalId")
                .IsUnique()
                .HasFilter("[NationalId] IS NOT NULL");
            builder.HasIndex(u => u.PersonnelCode)
                .HasDatabaseName("IX_Users_PersonnelCode")
                .IsUnique()
                .HasFilter("[PersonnelCode] IS NOT NULL");
        }
    }
}
