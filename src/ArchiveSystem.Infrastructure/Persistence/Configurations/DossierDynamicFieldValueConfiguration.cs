using ArchiveSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArchiveSystem.Infrastructure.Persistence.Configurations
{
    public class DossierDynamicFieldValueConfiguration : IEntityTypeConfiguration<DossierDynamicFieldValue>
    {
        public void Configure(EntityTypeBuilder<DossierDynamicFieldValue> builder)
        {
            builder.ToTable("DossierDynamicFieldValues");

            builder.HasKey(v => v.Id);
            builder.Property(v => v.Id)
                .UseIdentityColumn()
                .ValueGeneratedOnAdd();

            builder.Property(v => v.FieldType).HasConversion<int>().IsRequired();
            builder.Property(v => v.TextValue).HasMaxLength(4000).IsRequired(false);
            builder.Property(v => v.IntegerValue).IsRequired(false);
            builder.Property(v => v.DecimalValue).HasColumnType("decimal(18,4)").IsRequired(false);
            builder.Property(v => v.DateValue).HasColumnType("date").IsRequired(false);
            builder.Property(v => v.DateTimeValue).IsRequired(false);
            builder.Property(v => v.TimeValue).IsRequired(false);
            builder.Property(v => v.CheckboxValue).IsRequired(false);
            builder.Property(v => v.OptionValue).HasMaxLength(150).IsRequired(false);
            builder.Property(v => v.CreatedAt).IsRequired();
            builder.Property(v => v.ModifiedAt).IsRequired(false);
            builder.Property(v => v.DeletedAt).IsRequired(false);

            builder.HasOne(v => v.Dossier)
                .WithMany(d => d.DynamicFieldValues)
                .HasForeignKey(v => v.DossierId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(v => v.DynamicField)
                .WithMany(f => f.Values)
                .HasForeignKey(v => v.DynamicFieldId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(v => new { v.DossierId, v.DynamicFieldId })
                .HasDatabaseName("IX_DossierDynamicFieldValues_DossierId_DynamicFieldId");
            builder.HasIndex(v => v.DeletedAt)
                .HasDatabaseName("IX_DossierDynamicFieldValues_DeletedAt");
        }
    }
}
