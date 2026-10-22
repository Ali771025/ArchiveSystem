using ArchiveSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArchiveSystem.Infrastructure.Persistence.Configurations
{
    public sealed class TaxpayerConfiguration : IEntityTypeConfiguration<Taxpayer>
    {
        public void Configure(EntityTypeBuilder<Taxpayer> builder)
        {
            builder.ToTable("Taxpayers");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .ValueGeneratedNever();

            builder.Property(x => x.TaxpayerType)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.FirstName)
                .HasMaxLength(100)
                .IsRequired(false);

            builder.Property(x => x.LastName)
                .HasMaxLength(100)
                .IsRequired(false);

            builder.Property(x => x.NationalId)
                .HasMaxLength(10)
                .IsRequired(false);

            builder.Property(x => x.CompanyName)
                .HasMaxLength(100)
                .IsRequired(false);

            builder.Property(x => x.NationalCompanyId)
                .HasMaxLength(11)
                .IsRequired(false);

            builder.Property(x => x.IdentityNumber)
                .HasMaxLength(50)
                .IsRequired(false);

            builder.Property(x => x.Mobile)
                .HasMaxLength(20)
                .IsRequired(false);

            builder.Property(x => x.LandlinePhone)
                .HasMaxLength(20)
                .IsRequired(false);

            builder.Property(x => x.PostalCode)
                .HasMaxLength(10)
                .IsRequired(false);

            builder.Property(x => x.Address)
                .HasMaxLength(1000)
                .IsRequired(false);

            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.ModifiedAt).IsRequired(false);
            builder.Property(x => x.DeletedAt).IsRequired(false);

            builder.HasQueryFilter(x => x.DeletedAt == null);

            builder.HasIndex(x => x.NationalId)
                .HasDatabaseName("IX_Taxpayers_NationalId")
                .IsUnique()
                .HasFilter("[NationalId] IS NOT NULL AND [DeletedAt] IS NULL");

            builder.HasIndex(x => x.NationalCompanyId)
                .HasDatabaseName("IX_Taxpayers_NationalCompanyId")
                .IsUnique()
                .HasFilter("[NationalCompanyId] IS NOT NULL AND [DeletedAt] IS NULL");

            builder.HasIndex(x => x.Mobile)
                .HasDatabaseName("IX_Taxpayers_Mobile")
                .IsUnique()
                .HasFilter("[Mobile] IS NOT NULL AND [DeletedAt] IS NULL");

            builder.HasIndex(x => x.FirstName).HasDatabaseName("IX_Taxpayers_FirstName");
            builder.HasIndex(x => x.LastName).HasDatabaseName("IX_Taxpayers_LastName");
            builder.HasIndex(x => x.CompanyName).HasDatabaseName("IX_Taxpayers_CompanyName");
        }
    }
}
