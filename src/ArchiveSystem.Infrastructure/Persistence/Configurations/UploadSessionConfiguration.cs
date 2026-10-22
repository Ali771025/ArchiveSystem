using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArchiveSystem.Infrastructure.Persistence.Configurations
{
    public class UploadSessionConfiguration : IEntityTypeConfiguration<UploadSession>
    {
        public void Configure(EntityTypeBuilder<UploadSession> builder)
        {
            builder.ToTable("UploadSessions");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.Id).ValueGeneratedNever();
            builder.Property(s => s.FileId).IsRequired(false);
            builder.Property(s => s.CaseId).IsRequired(false);
            builder.Property(s => s.TenantId).HasMaxLength(120).IsRequired();
            builder.Property(s => s.OriginalFileName).HasMaxLength(255).IsRequired();
            builder.Property(s => s.ContentType).HasMaxLength(200).IsRequired();
            builder.Property(s => s.ExpectedSizeBytes).IsRequired();
            builder.Property(s => s.ExpectedSha256).HasMaxLength(64).IsRequired();
            builder.Property(s => s.PartSizeBytes).IsRequired();
            builder.Property(s => s.BucketName).HasMaxLength(255).IsRequired();
            builder.Property(s => s.ObjectKey).HasMaxLength(1024).IsRequired();
            builder.Property(s => s.MultipartUploadId).HasMaxLength(255).IsRequired();
            builder.Property(s => s.Status).HasConversion<int>().HasDefaultValue(UploadSessionStatus.Initiated).IsRequired();
            builder.Property(s => s.FailureReason).HasMaxLength(1000).IsRequired(false);
            builder.Property(s => s.CompletedAt).IsRequired(false);
            builder.Property(s => s.FinalizedVersionId).IsRequired(false);
            builder.Property(s => s.CreatedAt).IsRequired();
            builder.Property(s => s.ModifiedAt).IsRequired(false);

            builder.HasOne(s => s.File)
                .WithMany()
                .HasForeignKey(s => s.FileId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(s => s.Parts)
                .WithOne(p => p.Session)
                .HasForeignKey(p => p.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(s => new { s.Status, s.CreatedAt }).HasDatabaseName("IX_UploadSessions_Status_CreatedAt");
            builder.HasIndex(s => s.FileId).HasDatabaseName("IX_UploadSessions_FileId");
            builder.HasIndex(s => s.MultipartUploadId).HasDatabaseName("IX_UploadSessions_MultipartUploadId");
        }
    }
}
