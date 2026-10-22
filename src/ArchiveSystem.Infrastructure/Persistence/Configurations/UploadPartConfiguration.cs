using ArchiveSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArchiveSystem.Infrastructure.Persistence.Configurations
{
    public class UploadPartConfiguration : IEntityTypeConfiguration<UploadPart>
    {
        public void Configure(EntityTypeBuilder<UploadPart> builder)
        {
            builder.ToTable("UploadParts");

            builder.HasKey(p => p.Id);
            builder.Property(p => p.Id)
                .UseIdentityColumn()
                .ValueGeneratedOnAdd();

            builder.Property(p => p.SessionId).IsRequired();
            builder.Property(p => p.PartNumber).IsRequired();
            builder.Property(p => p.ETag).HasMaxLength(128).IsRequired();
            builder.Property(p => p.SizeBytes).IsRequired();
            builder.Property(p => p.UploadedAt).IsRequired();
            builder.Property(p => p.CreatedAt).IsRequired();
            builder.Property(p => p.ModifiedAt).IsRequired(false);

            builder.HasIndex(p => new { p.SessionId, p.PartNumber })
                .IsUnique()
                .HasDatabaseName("IX_UploadParts_SessionId_PartNumber");
        }
    }
}
