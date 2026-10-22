using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArchiveSystem.Infrastructure.Persistence.Configurations
{
    public class FileVersionConfiguration : IEntityTypeConfiguration<FileVersion>
    {
        public void Configure(EntityTypeBuilder<FileVersion> builder)
        {
            builder.ToTable("FileVersions");

            builder.HasKey(v => v.Id);
            builder.Property(v => v.Id)
                .UseIdentityColumn()
                .ValueGeneratedOnAdd();

            builder.Property(v => v.FileId).IsRequired();
            builder.Property(v => v.VersionNumber).IsRequired();
            builder.Property(v => v.StoragePath).HasMaxLength(1024).IsRequired();
            builder.Property(v => v.FileName).HasMaxLength(255).IsRequired();
            builder.Property(v => v.ContentType).HasMaxLength(200).IsRequired();
            builder.Property(v => v.Size).IsRequired();
            builder.Property(v => v.Sha256).HasMaxLength(64).IsRequired();
            builder.Property(v => v.Thumbnail).IsRequired(false);
            builder.Property(v => v.ThumbnailContentType).HasMaxLength(128).IsRequired(false);
            builder.Property(v => v.ThumbnailWidth).IsRequired(false);
            builder.Property(v => v.ThumbnailHeight).IsRequired(false);
            builder.Property(v => v.ThumbnailStatus)
                .HasConversion<int>()
                .HasDefaultValue(ProcessingStatus.Pending)
                .IsRequired();
            builder.Property(v => v.ThumbnailError).HasMaxLength(512).IsRequired(false);
            builder.Property(v => v.OcrResult).IsRequired(false);
            builder.Property(v => v.OcrText).IsRequired(false);
            builder.Property(v => v.OcrStatus)
                .HasConversion<int>()
                .HasDefaultValue(ProcessingStatus.Pending)
                .IsRequired();
            builder.Property(v => v.OcrError).HasMaxLength(512).IsRequired(false);
            builder.Property(v => v.OcrLanguage).HasMaxLength(32).IsRequired(false);
            builder.Property(v => v.OcrWordCount).IsRequired(false);
            builder.Property(v => v.CreatedAt).IsRequired();
            builder.Property(v => v.ModifiedAt).IsRequired(false);
            builder.Property(v => v.DeletedAt).IsRequired(false);

            builder.HasIndex(v => new { v.FileId, v.VersionNumber }).IsUnique().HasDatabaseName("IX_FileVersions_FileId_VersionNumber");
            builder.HasIndex(v => new { v.FileId, v.CreatedAt }).HasDatabaseName("IX_FileVersions_FileId_CreatedAt");
            builder.HasIndex(v => new { v.ThumbnailStatus, v.CreatedAt }).HasDatabaseName("IX_FileVersions_ThumbnailStatus_CreatedAt");
            builder.HasIndex(v => new { v.OcrStatus, v.CreatedAt }).HasDatabaseName("IX_FileVersions_OcrStatus_CreatedAt");
            builder.HasIndex(v => v.Sha256).HasDatabaseName("IX_FileVersions_Sha256");
            builder.HasIndex(v => v.StoragePath).HasDatabaseName("IX_FileVersions_StoragePath");
        }
    }
}
