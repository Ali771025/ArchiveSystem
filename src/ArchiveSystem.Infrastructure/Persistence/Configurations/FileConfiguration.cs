// src/ArchiveSystem.Infrastructure/Persistence/Configurations/FileConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Domain.Enums;
using FileEntity = ArchiveSystem.Domain.Entities.ArchiveFile;

namespace ArchiveSystem.Infrastructure.Persistence.Configurations
{
    public class FileConfiguration : IEntityTypeConfiguration<FileEntity>
    {
        public void Configure(EntityTypeBuilder<FileEntity> builder)
        {
            builder.ToTable("Files");

            // Primary key
            builder.HasKey(f => f.Id);
            
            // Configure ID as identity column (for auto-increment)
            builder.Property(f => f.Id)
                .UseIdentityColumn()
                .ValueGeneratedOnAdd();

            // Properties
            builder.Property(f => f.Name)
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(f => f.Size)
                .HasConversion(
                    s => s.Bytes,
                    b => Domain.ValueObjects.FileSize.Create(b))
                .IsRequired();

            builder.Property(f => f.Extension)
                .HasConversion(
                    e => e.Value,
                    s => Domain.ValueObjects.FileExtension.Create(s))
                .HasMaxLength(10)
                .IsRequired();

            builder.Property(f => f.ContentType)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(f => f.StoragePath)
                .HasMaxLength(1000)
                .IsRequired();

            builder.Property(f => f.Status)
                .HasConversion<byte>()
                .IsRequired();

            builder.Property(f => f.CreatedAt)
                .IsRequired();

            builder.Property(f => f.ModifiedAt)
                .IsRequired(false);

            builder.Property(f => f.DeletedAt)
                .IsRequired(false);

            builder.Property(f => f.Hash)
                .HasMaxLength(255)
                .IsRequired(false);

            builder.Property(f => f.LatestVersionId)
                .IsRequired(false);

            // Relationships
            builder.HasOne(f => f.Case)
                .WithMany(c => c.Files)
                .HasForeignKey(f => f.CaseId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(f => f.Versions)
                .WithOne(v => v.File)
                .HasForeignKey(v => v.FileId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes for performance
            builder.HasIndex(f => f.CaseId).HasDatabaseName("IX_Files_CaseId");
            builder.HasIndex(f => f.Status).HasDatabaseName("IX_Files_Status");
            builder.HasIndex(f => f.Extension).HasDatabaseName("IX_Files_Extension");
            builder.HasIndex(f => f.CreatedAt).HasDatabaseName("IX_Files_CreatedAt");
            builder.HasIndex(f => f.StoragePath).HasDatabaseName("IX_Files_StoragePath").IsUnique();
            builder.HasIndex(f => f.LatestVersionId).HasDatabaseName("IX_Files_LatestVersionId");

            // Partitioning: This requires raw SQL in migrations
            // We'll configure the partitioning strategy in the migration
        }
    }
}
