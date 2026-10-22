using ArchiveSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArchiveSystem.Infrastructure.Persistence.Configurations
{
    public class CaseFieldValueDateConfiguration : IEntityTypeConfiguration<CaseFieldValueDate>
    {
        public void Configure(EntityTypeBuilder<CaseFieldValueDate> builder)
        {
            builder.ToTable("CaseFieldValueDates");

            builder.HasKey(v => new { v.CaseId, v.FieldId });
            builder.Property(v => v.Value).IsRequired();
            builder.Property(v => v.CreatedAt).IsRequired();
            builder.Property(v => v.UpdatedAt).IsRequired(false);

            builder.HasIndex(v => new { v.FieldId, v.Value })
                .HasDatabaseName("IX_CaseFieldValueDates_FieldId_Value");
            builder.HasIndex(v => new { v.FieldId, v.Value, v.CaseId })
                .HasDatabaseName("IX_CaseFieldValueDates_FieldId_Value_CaseId");

            builder.HasOne(v => v.Case)
                .WithMany(c => c.DynamicDateValues)
                .HasForeignKey(v => v.CaseId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(v => v.Field)
                .WithMany()
                .HasForeignKey(v => v.FieldId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
