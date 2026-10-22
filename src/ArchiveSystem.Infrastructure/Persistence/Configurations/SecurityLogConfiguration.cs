using ArchiveSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArchiveSystem.Infrastructure.Persistence.Configurations
{
    public sealed class SecurityLogConfiguration : IEntityTypeConfiguration<SecurityLog>
    {
        public void Configure(EntityTypeBuilder<SecurityLog> builder)
        {
            builder.ToTable("Logs_Security");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id)
                .HasColumnName("Id")
                .ValueGeneratedOnAdd();

            builder.Property(x => x.Message)
                .IsRequired(false);

            builder.Property(x => x.Level)
                .HasMaxLength(128)
                .IsRequired(false);

            builder.Property(x => x.ActionDate)
                .HasColumnName("TimeStamp")
                .IsRequired();

            builder.Property(x => x.EventType)
                .HasMaxLength(120)
                .IsRequired(false);

            builder.Property(x => x.ActorUserId)
                .HasMaxLength(64)
                .IsRequired(false);

            builder.Property(x => x.TargetUserId)
                .HasMaxLength(64)
                .IsRequired(false);

            builder.Property(x => x.ClientIpAddress)
                .HasColumnName("ClientIpAddress")
                .HasMaxLength(64)
                .IsRequired(false);

            builder.Property(x => x.SecurityMetadata)
                .IsRequired(false);

            builder.HasIndex(x => x.ActionDate)
                .HasDatabaseName("IX_Logs_Security_ActionDate");

            builder.HasIndex(x => new { x.ActorUserId, x.ActionDate })
                .HasDatabaseName("IX_Logs_Security_ActorUserId_ActionDate");

            builder.HasIndex(x => new { x.TargetUserId, x.ActionDate })
                .HasDatabaseName("IX_Logs_Security_TargetUserId_ActionDate");

            builder.HasIndex(x => new { x.EventType, x.ActionDate })
                .HasDatabaseName("IX_Logs_Security_EventType_ActionDate");
        }
    }
}
