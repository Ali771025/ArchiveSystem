using ArchiveSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArchiveSystem.Infrastructure.Persistence.Configurations
{
    public class DynamicTemplateConfiguration : IEntityTypeConfiguration<DynamicTemplate>
    {
        public void Configure(EntityTypeBuilder<DynamicTemplate> builder)
        {
            builder.ToTable("DynamicTemplates");

            builder.HasKey(t => t.Id);
            builder.Property(t => t.Id).ValueGeneratedNever();
            builder.Property(t => t.Name).HasMaxLength(200).IsRequired();
            builder.Property(t => t.IsActive).IsRequired();
            builder.Property(t => t.CreatedAt).IsRequired();
            builder.Property(t => t.UpdatedAt).IsRequired(false);

            builder.HasIndex(t => t.ClassificationId)
                .IsUnique()
                .HasDatabaseName("UX_DynamicTemplates_ClassificationId");

            builder.HasOne(t => t.Classification)
                .WithMany(c => c.DynamicTemplates)
                .HasForeignKey(t => t.ClassificationId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
