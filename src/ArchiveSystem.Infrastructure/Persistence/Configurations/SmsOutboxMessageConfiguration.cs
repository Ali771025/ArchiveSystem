using ArchiveSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArchiveSystem.Infrastructure.Persistence.Configurations
{
    public sealed class SmsOutboxMessageConfiguration : IEntityTypeConfiguration<SmsOutboxMessage>
    {
        public void Configure(EntityTypeBuilder<SmsOutboxMessage> builder)
        {
            builder.ToTable("SmsOutboxMessages");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.MobileNumber)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(x => x.Purpose)
                .HasMaxLength(40)
                .IsRequired();

            builder.Property(x => x.Provider)
                .HasMaxLength(64)
                .IsRequired();

            builder.Property(x => x.PatternCode)
                .HasMaxLength(120)
                .IsRequired();

            builder.Property(x => x.PayloadJson)
                .IsRequired();

            builder.Property(x => x.CorrelationId)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.ClientIpAddress)
                .HasMaxLength(64);

            builder.Property(x => x.UserAgent)
                .HasMaxLength(512);

            builder.Property(x => x.LastError)
                .HasMaxLength(1000);

            builder.HasIndex(x => new { x.Status, x.NextAttemptAtUtc })
                .HasDatabaseName("IX_SmsOutboxMessages_Status_NextAttemptAtUtc");

            builder.HasIndex(x => x.CreatedAtUtc)
                .HasDatabaseName("IX_SmsOutboxMessages_CreatedAtUtc");

            builder.HasIndex(x => x.MobileNumber)
                .HasDatabaseName("IX_SmsOutboxMessages_MobileNumber");
        }
    }
}
