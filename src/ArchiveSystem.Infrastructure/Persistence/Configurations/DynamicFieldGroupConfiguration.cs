using ArchiveSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArchiveSystem.Infrastructure.Persistence.Configurations
{
    public class DynamicFieldGroupConfiguration : IEntityTypeConfiguration<DynamicFieldGroup>
    {
        public void Configure(EntityTypeBuilder<DynamicFieldGroup> builder)
        {
            builder.ToTable("DynamicFieldGroups");

            builder.HasKey(g => g.Id);
            builder.Property(g => g.Id).ValueGeneratedNever();
            builder.Property(g => g.Title).HasMaxLength(200).IsRequired();
            builder.Property(g => g.Order).IsRequired();
            builder.Property(g => g.CreatedAt).IsRequired();
            builder.Property(g => g.UpdatedAt).IsRequired(false);

            builder.HasIndex(g => new { g.TemplateId, g.Order })
                .HasDatabaseName("IX_DynamicFieldGroups_TemplateId_Order");

            builder.HasOne(g => g.Template)
                .WithMany(t => t.Groups)
                .HasForeignKey(g => g.TemplateId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
