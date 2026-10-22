
// src/ArchiveSystem.Infrastructure/Persistence/Configurations/ClassificationConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ArchiveSystem.Domain.Entities;

namespace ArchiveSystem.Infrastructure.Persistence.Configurations
{
    public class ClassificationConfiguration : IEntityTypeConfiguration<Classification>
    {
        public void Configure(EntityTypeBuilder<Classification> builder)
        {
            builder.ToTable("Classifications");

            builder.HasKey(c => c.Id);
            builder.Property(c => c.Id)
                .UseIdentityColumn()
                .ValueGeneratedOnAdd();

            builder.Property(c => c.Title)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(c => c.Path)
                .HasMaxLength(1000)
                .IsRequired();

            builder.Property(c => c.Level)
                .IsRequired();

            builder.Property(c => c.IsActive)
                .IsRequired();

            builder.Property(c => c.DeletedAt)
                .IsRequired(false);

            builder.Property(c => c.SystemKey)
                .HasMaxLength(64)
                .IsRequired(false);

            builder.Property(c => c.CreatedAt)
                .IsRequired();

            builder.Property(c => c.ModifiedAt)
                .IsRequired(false);

            // Self-referencing relationship
            builder.HasOne(c => c.Parent)
                .WithMany(c => c.Children)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(c => c.Path).HasDatabaseName("IX_Classifications_Path");
            builder.HasIndex(c => c.ParentId).HasDatabaseName("IX_Classifications_ParentId");
            builder.HasIndex(c => new { c.ParentId, c.Title })
                .IsUnique()
                .HasFilter("[DeletedAt] IS NULL")
                .HasDatabaseName("UX_Classifications_ParentId_Title_DeletedAt");
            builder.HasIndex(c => c.IsActive).HasDatabaseName("IX_Classifications_IsActive");
            builder.HasIndex(c => c.DeletedAt).HasDatabaseName("IX_Classifications_DeletedAt");
            builder.HasIndex(c => c.SystemKey)
                .IsUnique()
                .HasFilter("[SystemKey] IS NOT NULL")
                .HasDatabaseName("UX_Classifications_SystemKey");
        }
    }
}
