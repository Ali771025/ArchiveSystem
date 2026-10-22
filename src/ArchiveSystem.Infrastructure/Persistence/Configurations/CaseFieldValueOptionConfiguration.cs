using ArchiveSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArchiveSystem.Infrastructure.Persistence.Configurations
{
    public class CaseFieldValueOptionConfiguration : IEntityTypeConfiguration<CaseFieldValueOption>
    {
        public void Configure(EntityTypeBuilder<CaseFieldValueOption> builder)
        {
            builder.ToTable("CaseFieldValueOptions");

            builder.HasKey(v => new { v.CaseId, v.FieldId, v.OptionId });
            builder.Property(v => v.CreatedAt).IsRequired();

            builder.HasIndex(v => new { v.FieldId, v.OptionId })
                .HasDatabaseName("IX_CaseFieldValueOptions_FieldId_OptionId");
            builder.HasIndex(v => new { v.CaseId, v.FieldId })
                .HasDatabaseName("IX_CaseFieldValueOptions_CaseId_FieldId");

            builder.HasOne(v => v.Case)
                .WithMany(c => c.DynamicOptionValues)
                .HasForeignKey(v => v.CaseId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(v => v.Field)
                .WithMany()
                .HasForeignKey(v => v.FieldId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(v => v.Option)
                .WithMany()
                .HasForeignKey(v => v.OptionId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
