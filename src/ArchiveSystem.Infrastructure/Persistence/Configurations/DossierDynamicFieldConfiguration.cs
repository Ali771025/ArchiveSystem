using ArchiveSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArchiveSystem.Infrastructure.Persistence.Configurations
{
    public class DossierDynamicFieldConfiguration : IEntityTypeConfiguration<DossierDynamicField>
    {
        public void Configure(EntityTypeBuilder<DossierDynamicField> builder)
        {
            builder.ToTable("DossierDynamicFields");

            builder.HasKey(f => f.Id);
            builder.Property(f => f.Id).ValueGeneratedNever();

            builder.Property(f => f.Title).HasMaxLength(150).IsRequired();
            builder.Property(f => f.ApiName).HasMaxLength(80).IsRequired();
            builder.Property(f => f.FieldType).HasConversion<int>().IsRequired();
            builder.Property(f => f.ValidationRegex).HasMaxLength(500).IsRequired(false);
            builder.Property(f => f.ErrorMessage).HasMaxLength(500).IsRequired(false);
            builder.Property(f => f.DisplayOrder).IsRequired();
            builder.Property(f => f.Scope).HasConversion<int>().IsRequired();
            builder.Property(f => f.IsActive).IsRequired();
            builder.Property(f => f.CreatedAt).IsRequired();
            builder.Property(f => f.ModifiedAt).IsRequired(false);
            builder.Property(f => f.DeletedAt).IsRequired(false);

            builder.HasOne(f => f.Group)
                .WithMany(g => g.Fields)
                .HasForeignKey(f => f.GroupId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(f => f.Classification)
                .WithMany()
                .HasForeignKey(f => f.ClassificationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(f => f.Options)
                .WithOne(o => o.DynamicField)
                .HasForeignKey(o => o.DynamicFieldId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(f => f.Values)
                .WithOne(v => v.DynamicField)
                .HasForeignKey(v => v.DynamicFieldId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(f => f.ApiName)
                .HasDatabaseName("IX_DossierDynamicFields_ApiName");
            builder.HasIndex(f => new { f.Scope, f.ClassificationId })
                .HasDatabaseName("IX_DossierDynamicFields_Scope_ClassificationId");
            builder.HasIndex(f => f.DeletedAt)
                .HasDatabaseName("IX_DossierDynamicFields_DeletedAt");
        }
    }
}
