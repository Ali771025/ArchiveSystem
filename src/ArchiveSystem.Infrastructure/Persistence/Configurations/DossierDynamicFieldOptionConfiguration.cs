using ArchiveSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArchiveSystem.Infrastructure.Persistence.Configurations
{
    public class DossierDynamicFieldOptionConfiguration : IEntityTypeConfiguration<DossierDynamicFieldOption>
    {
        public void Configure(EntityTypeBuilder<DossierDynamicFieldOption> builder)
        {
            builder.ToTable("DossierDynamicFieldOptions");

            builder.HasKey(o => o.Id);
            builder.Property(o => o.Id).ValueGeneratedNever();

            builder.Property(o => o.Value).HasMaxLength(150).IsRequired();
            builder.Property(o => o.Label).HasMaxLength(150).IsRequired();
            builder.Property(o => o.DisplayOrder).IsRequired();
            builder.Property(o => o.IsActive).IsRequired();
            builder.Property(o => o.CreatedAt).IsRequired();
            builder.Property(o => o.ModifiedAt).IsRequired(false);

            builder.HasOne(o => o.DynamicField)
                .WithMany(f => f.Options)
                .HasForeignKey(o => o.DynamicFieldId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(o => new { o.DynamicFieldId, o.Value })
                .HasDatabaseName("IX_DossierDynamicFieldOptions_DynamicFieldId_Value");
            builder.HasIndex(o => new { o.DynamicFieldId, o.DisplayOrder })
                .HasDatabaseName("IX_DossierDynamicFieldOptions_DynamicFieldId_DisplayOrder");
        }
    }
}
