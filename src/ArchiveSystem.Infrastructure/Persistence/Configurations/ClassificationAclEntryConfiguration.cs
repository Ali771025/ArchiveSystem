using ArchiveSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArchiveSystem.Infrastructure.Persistence.Configurations
{
    public class ClassificationAclEntryConfiguration : IEntityTypeConfiguration<ClassificationAclEntry>
    {
        public void Configure(EntityTypeBuilder<ClassificationAclEntry> builder)
        {
            builder.ToTable("ClassificationAclEntries");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id)
                .UseIdentityColumn()
                .ValueGeneratedOnAdd();

            builder.Property(x => x.SubjectType).IsRequired();
            builder.Property(x => x.SubjectId).IsRequired();
            builder.Property(x => x.ClassificationId).IsRequired();
            builder.Property(x => x.PermissionMask).HasConversion<int>().IsRequired();
            builder.Property(x => x.IsExplicit).IsRequired();
            builder.Property(x => x.InheritMode).IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.ModifiedAt).IsRequired(false);

            builder.HasOne(x => x.Classification)
                .WithMany()
                .HasForeignKey(x => x.ClassificationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.ClassificationId, x.SubjectType, x.SubjectId })
                .HasDatabaseName("IX_ClassificationAclEntries_Classification_Subject");

            builder.HasIndex(x => new { x.SubjectType, x.SubjectId })
                .HasDatabaseName("IX_ClassificationAclEntries_Subject");
        }
    }
}
