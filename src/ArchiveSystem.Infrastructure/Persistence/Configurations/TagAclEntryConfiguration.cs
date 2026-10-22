using ArchiveSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArchiveSystem.Infrastructure.Persistence.Configurations
{
    public class TagAclEntryConfiguration : IEntityTypeConfiguration<TagAclEntry>
    {
        public void Configure(EntityTypeBuilder<TagAclEntry> builder)
        {
            builder.ToTable("TagAclEntries");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id)
                .UseIdentityColumn()
                .ValueGeneratedOnAdd();

            builder.Property(x => x.SubjectType).IsRequired();
            builder.Property(x => x.SubjectId).IsRequired();
            builder.Property(x => x.TagId).IsRequired();
            builder.Property(x => x.PermissionMask).HasConversion<int>().IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.ModifiedAt).IsRequired(false);

            builder.HasOne(x => x.Tag)
                .WithMany()
                .HasForeignKey(x => x.TagId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.TagId, x.SubjectType, x.SubjectId })
                .HasDatabaseName("IX_TagAclEntries_Tag_Subject");

            builder.HasIndex(x => new { x.SubjectType, x.SubjectId })
                .HasDatabaseName("IX_TagAclEntries_Subject");
        }
    }
}
