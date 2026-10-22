using ArchiveSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArchiveSystem.Infrastructure.Persistence.Configurations
{
    public class OwnerConfiguration : IEntityTypeConfiguration<Owner>
    {
        public void Configure(EntityTypeBuilder<Owner> builder)
        {
            builder.ToTable("Owners");

            builder.HasKey(o => o.Id);
            builder.Property(o => o.Id)
                .UseIdentityColumn()
                .ValueGeneratedOnAdd();

            builder.Property(o => o.OwnerType).HasConversion<int>().IsRequired();

            builder.Property(o => o.Mobile).HasMaxLength(20).IsRequired(false);
            builder.Property(o => o.PostalCode).HasMaxLength(20).IsRequired(false);
            builder.Property(o => o.Address).HasMaxLength(1000).IsRequired(false);
            builder.Property(o => o.HomePhone).HasMaxLength(20).IsRequired(false);
            builder.Property(o => o.TaxpayerId).HasMaxLength(14).IsRequired(false);

            builder.Property(o => o.FirstName).HasMaxLength(100).IsRequired(false);
            builder.Property(o => o.LastName).HasMaxLength(100).IsRequired(false);
            builder.Property(o => o.NationalCode).HasMaxLength(10).IsRequired(false);
            builder.Property(o => o.IdentityNumber).HasMaxLength(50).IsRequired(false);

            builder.Property(o => o.CompanyName).HasMaxLength(200).IsRequired(false);
            builder.Property(o => o.NationalId).HasMaxLength(11).IsRequired(false);
            builder.Property(o => o.RegistrationNumber).HasMaxLength(50).IsRequired(false);

            builder.Property(o => o.CreatedAt).IsRequired();
            builder.Property(o => o.ModifiedAt).IsRequired(false);

            builder.HasIndex(o => o.NationalCode)
                .HasDatabaseName("IX_Owners_NationalCode")
                .IsUnique()
                .HasFilter("[NationalCode] IS NOT NULL");
            builder.HasIndex(o => o.NationalId)
                .HasDatabaseName("IX_Owners_NationalId")
                .IsUnique()
                .HasFilter("[NationalId] IS NOT NULL");
            builder.HasIndex(o => o.FirstName).HasDatabaseName("IX_Owners_FirstName");
            builder.HasIndex(o => o.LastName).HasDatabaseName("IX_Owners_LastName");
            builder.HasIndex(o => o.CompanyName).HasDatabaseName("IX_Owners_CompanyName");
        }
    }
}
