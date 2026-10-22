using ArchiveSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArchiveSystem.Infrastructure.Persistence.Configurations
{
    public class CaseFieldValueStringConfiguration : IEntityTypeConfiguration<CaseFieldValueString>
    {
        public void Configure(EntityTypeBuilder<CaseFieldValueString> builder)
        {
            builder.ToTable("CaseFieldValueStrings");

            builder.HasKey(v => new { v.CaseId, v.FieldId });
            builder.Property(v => v.Value).HasMaxLength(4000).IsRequired();
            builder.Property(v => v.CreatedAt).IsRequired();
            builder.Property(v => v.UpdatedAt).IsRequired(false);

            builder.HasIndex(v => new { v.FieldId, v.Value })
                .HasDatabaseName("IX_CaseFieldValueStrings_FieldId_Value");
            builder.HasIndex(v => new { v.FieldId, v.Value, v.CaseId })
                .HasDatabaseName("IX_CaseFieldValueStrings_FieldId_Value_CaseId");

            builder.HasOne(v => v.Case)
                .WithMany(c => c.DynamicStringValues)
                .HasForeignKey(v => v.CaseId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(v => v.Field)
                .WithMany()
                .HasForeignKey(v => v.FieldId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
