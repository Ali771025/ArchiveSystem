// src/ArchiveSystem.Infrastructure/Persistence/Configurations/CaseConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ArchiveSystem.Domain.Entities;

namespace ArchiveSystem.Infrastructure.Persistence.Configurations
{
    public class CaseConfiguration : IEntityTypeConfiguration<Case>
    {
        public void Configure(EntityTypeBuilder<Case> builder)
        {
            builder.ToTable("Cases");

            builder.HasKey(c => c.Id);
            builder.Property(c => c.Id)
                .UseIdentityColumn()
                .ValueGeneratedOnAdd();

            builder.Property(c => c.CaseNumber)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(c => c.ExternalCaseNumber)
                .HasMaxLength(100)
                .IsRequired(false);

            builder.Property(c => c.Title)
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(c => c.AvatarImagePath)
                .HasMaxLength(500)
                .IsRequired(false);

            builder.Property(c => c.PostalCode)
                .HasMaxLength(10)
                .IsRequired(false);

            builder.Property(c => c.CreatedByUserId)
                .IsRequired(false);

            builder.Property(c => c.CreatedByUserName)
                .HasMaxLength(200)
                .IsRequired(false);

            builder.Property(c => c.CreatedAt)
                .IsRequired();

            builder.Property(c => c.ModifiedAt)
                .IsRequired(false);

            builder.Property(c => c.ClosedAt)
                .IsRequired(false);

            // Relationships
            builder.HasOne(c => c.Classification)
                .WithMany()
                .HasForeignKey(c => c.ClassificationId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(c => c.CaseNumber).HasDatabaseName("IX_Cases_CaseNumber").IsUnique();
            builder.HasIndex(c => c.ExternalCaseNumber)
                .HasDatabaseName("IX_Cases_ExternalCaseNumber")
                .HasFilter("[ExternalCaseNumber] IS NOT NULL")
                .IsUnique();
            builder.HasIndex(c => c.ClassificationId).HasDatabaseName("IX_Cases_ClassificationId");
            builder.HasIndex(c => c.CreatedByUserId).HasDatabaseName("IX_Cases_CreatedByUserId");
        }
    }
}
