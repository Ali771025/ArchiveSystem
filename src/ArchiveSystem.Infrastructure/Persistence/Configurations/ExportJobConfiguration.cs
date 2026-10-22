using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArchiveSystem.Infrastructure.Persistence.Configurations
{
    public class ExportJobConfiguration : IEntityTypeConfiguration<ExportJob>
    {
        public void Configure(EntityTypeBuilder<ExportJob> builder)
        {
            builder.ToTable("ExportJobs");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id)
                .ValueGeneratedNever();

            builder.Property(x => x.CreatedByUserId).IsRequired();
            builder.Property(x => x.Type)
                .HasConversion<byte>()
                .IsRequired();
            builder.Property(x => x.Status)
                .HasConversion<byte>()
                .IsRequired();
            builder.Property(x => x.ProgressPercent).IsRequired(false);
            builder.Property(x => x.RequestJson).HasMaxLength(2000).IsRequired(false);
            builder.Property(x => x.ResultObjectKey).HasMaxLength(1024).IsRequired(false);
            builder.Property(x => x.ErrorMessage).HasMaxLength(2000).IsRequired(false);
            builder.Property(x => x.ExpiresAt).IsRequired();
            builder.Property(x => x.StartedAt).IsRequired(false);
            builder.Property(x => x.CompletedAt).IsRequired(false);
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.ModifiedAt).IsRequired(false);

            builder.HasIndex(x => new { x.CreatedByUserId, x.CreatedAt })
                .HasDatabaseName("IX_ExportJobs_CreatedByUserId_CreatedAt");
            builder.HasIndex(x => new { x.Status, x.CreatedAt })
                .HasDatabaseName("IX_ExportJobs_Status_CreatedAt");
        }
    }
}
