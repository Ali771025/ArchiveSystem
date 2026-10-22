using ArchiveSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArchiveSystem.Infrastructure.Persistence.Configurations
{
    public sealed class AuditTrailLogConfiguration : IEntityTypeConfiguration<AuditTrailLog>
    {
        public void Configure(EntityTypeBuilder<AuditTrailLog> builder)
        {
            builder.ToTable("Logs_AuditTrail");

            builder.HasKey(x => x.AuditId);
            builder.Property(x => x.AuditId)
                .UseIdentityColumn()
                .ValueGeneratedOnAdd();

            builder.Property(x => x.EventType)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.TableName)
                .HasMaxLength(128)
                .IsRequired();

            builder.Property(x => x.RecordId)
                .HasMaxLength(128)
                .IsRequired();

            builder.Property(x => x.UserId)
                .HasMaxLength(64)
                .IsRequired(false);

            builder.Property(x => x.ActionDate)
                .IsRequired();

            builder.Property(x => x.OldValues)
                .IsRequired(false);

            builder.Property(x => x.NewValues)
                .IsRequired(false);

            builder.Property(x => x.AffectedColumns)
                .IsRequired(false);

            builder.HasIndex(x => x.ActionDate)
                .HasDatabaseName("IX_Logs_AuditTrail_ActionDate");

            builder.HasIndex(x => new { x.UserId, x.ActionDate })
                .HasDatabaseName("IX_Logs_AuditTrail_UserId_ActionDate");

            builder.HasIndex(x => new { x.TableName, x.ActionDate })
                .HasDatabaseName("IX_Logs_AuditTrail_TableName_ActionDate");
        }
    }
}
