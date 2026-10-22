using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArchiveSystem.Infrastructure.Persistence.Configurations
{
    public class DynamicFieldDefinitionConfiguration : IEntityTypeConfiguration<DynamicFieldDefinition>
    {
        public void Configure(EntityTypeBuilder<DynamicFieldDefinition> builder)
        {
            builder.ToTable("DynamicFieldDefinitions");

            builder.HasKey(f => f.Id);
            builder.Property(f => f.Id).ValueGeneratedNever();
            builder.Property(f => f.Key).HasMaxLength(120).IsRequired();
            builder.Property(f => f.Label).HasMaxLength(200).IsRequired();
            builder.Property(f => f.FieldType).HasConversion<int>().IsRequired();
            builder.Property(f => f.IsRequired).IsRequired();
            builder.Property(f => f.MinNumber).HasColumnType("decimal(18,4)").IsRequired(false);
            builder.Property(f => f.MaxNumber).HasColumnType("decimal(18,4)").IsRequired(false);
            builder.Property(f => f.MinLength).IsRequired(false);
            builder.Property(f => f.MaxLength).IsRequired(false);
            builder.Property(f => f.Regex).HasMaxLength(500).IsRequired(false);
            builder.Property(f => f.ErrorMessage).HasMaxLength(500).IsRequired(false);
            builder.Property(f => f.IsMultiSelect).IsRequired();
            builder.Property(f => f.ShowAsFilter).IsRequired().HasDefaultValue(false);
            builder.Property(f => f.FilterType)
                .HasConversion<int>()
                .IsRequired()
                .HasDefaultValue(DynamicFieldFilterType.Contains);
            builder.Property(f => f.Order).IsRequired();
            builder.Property(f => f.IsActive).IsRequired();
            builder.Property(f => f.CreatedAt).IsRequired();
            builder.Property(f => f.UpdatedAt).IsRequired(false);

            builder.HasIndex(f => new { f.GroupId, f.Key })
                .IsUnique()
                .HasDatabaseName("UX_DynamicFieldDefinitions_GroupId_Key");

            builder.HasIndex(f => new { f.GroupId, f.Order })
                .HasDatabaseName("IX_DynamicFieldDefinitions_GroupId_Order");

            builder.HasOne(f => f.Group)
                .WithMany(g => g.Fields)
                .HasForeignKey(f => f.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
