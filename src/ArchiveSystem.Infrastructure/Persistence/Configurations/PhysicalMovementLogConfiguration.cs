using ArchiveSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArchiveSystem.Infrastructure.Persistence.Configurations
{
    public class PhysicalMovementLogConfiguration : IEntityTypeConfiguration<PhysicalMovementLog>
    {
        public void Configure(EntityTypeBuilder<PhysicalMovementLog> builder)
        {
            builder.ToTable("PhysicalMovementLogs");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();

            builder.Property(x => x.DossierNumber).HasMaxLength(50).IsRequired();
            builder.Property(x => x.ReceiverFirstName).HasMaxLength(100).IsRequired();
            builder.Property(x => x.ReceiverLastName).HasMaxLength(100).IsRequired();
            builder.Property(x => x.NationalCode).HasMaxLength(10).IsRequired();
            builder.Property(x => x.PhoneNumber).HasMaxLength(15).IsRequired();
            builder.Property(x => x.CheckoutDate).IsRequired();
            builder.Property(x => x.ExpectedReturnDate).IsRequired(false);
            builder.Property(x => x.Description).HasMaxLength(500).IsRequired(false);
            builder.Property(x => x.CreatedAtUtc).IsRequired();

            builder.HasIndex(x => x.DossierNumber).HasDatabaseName("IX_PhysicalMovementLogs_DossierNumber");
            builder.HasIndex(x => x.CheckoutDate).HasDatabaseName("IX_PhysicalMovementLogs_CheckoutDate");
        }
    }
}
