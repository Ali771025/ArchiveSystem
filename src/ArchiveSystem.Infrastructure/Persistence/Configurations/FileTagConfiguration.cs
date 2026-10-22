using ArchiveSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArchiveSystem.Infrastructure.Persistence.Configurations
{
    public class FileTagConfiguration : IEntityTypeConfiguration<FileTag>
    {
        public void Configure(EntityTypeBuilder<FileTag> builder)
        {
            builder.ToTable("FileTags");

            builder.HasKey(x => new { x.FileId, x.TagId });

            builder.HasOne(x => x.File)
                .WithMany(x => x.FileTags)
                .HasForeignKey(x => x.FileId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Tag)
                .WithMany()
                .HasForeignKey(x => x.TagId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.FileId)
                .HasDatabaseName("IX_FileTags_FileId");

            builder.HasIndex(x => x.TagId)
                .HasDatabaseName("IX_FileTags_TagId");
        }
    }
}
