using ArchiveSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArchiveSystem.Infrastructure.Persistence.Configurations
{
    public class DynamicFieldOptionConfiguration : IEntityTypeConfiguration<DynamicFieldOption>
    {
        public void Configure(EntityTypeBuilder<DynamicFieldOption> builder)
        {
            builder.ToTable("DynamicFieldOptions");

            builder.HasKey(o => o.Id);
            builder.Property(o => o.Id).ValueGeneratedNever();
            builder.Property(o => o.Value).HasMaxLength(150).IsRequired();
            builder.Property(o => o.Label).HasMaxLength(200).IsRequired();
            builder.Property(o => o.Order).IsRequired();
            builder.Property(o => o.IsActive).IsRequired();
            builder.Property(o => o.CreatedAt).IsRequired();
            builder.Property(o => o.UpdatedAt).IsRequired(false);

            builder.HasIndex(o => new { o.FieldId, o.Value })
                .IsUnique()
                .HasDatabaseName("UX_DynamicFieldOptions_FieldId_Value");

            builder.HasIndex(o => new { o.FieldId, o.Order })
                .HasDatabaseName("IX_DynamicFieldOptions_FieldId_Order");

            builder.HasOne(o => o.Field)
                .WithMany(f => f.Options)
                .HasForeignKey(o => o.FieldId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
