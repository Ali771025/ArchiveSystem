using ArchiveSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArchiveSystem.Infrastructure.Persistence.Configurations
{
    public sealed class CaseLoanConfiguration : IEntityTypeConfiguration<CaseLoan>
    {
        public void Configure(EntityTypeBuilder<CaseLoan> builder)
        {
            builder.ToTable("CaseLoans");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();

            builder.Property(x => x.BorrowerFirstName).HasMaxLength(100).IsRequired();
            builder.Property(x => x.BorrowerLastName).HasMaxLength(100).IsRequired();
            builder.Property(x => x.CaseNumber).HasMaxLength(50).IsRequired();
            builder.Property(x => x.NationalId).HasMaxLength(10).IsRequired();
            builder.Property(x => x.ContactNumber).HasMaxLength(20).IsRequired();
            builder.Property(x => x.CheckoutDate).IsRequired();
            builder.Property(x => x.ExpectedReturnDate).IsRequired(false);
            builder.Property(x => x.ActualReturnDate).IsRequired(false);
            builder.Property(x => x.Status).HasMaxLength(30).HasDefaultValue("Loaned").IsRequired();
            builder.Property(x => x.ReceiverUnit).HasMaxLength(150).IsRequired(false);
            builder.Property(x => x.RegisteredBy).HasMaxLength(150).IsRequired(false);
            builder.Property(x => x.Description).HasMaxLength(1000).IsRequired(false);
            builder.Property(x => x.IsReturned).HasDefaultValue(false).IsRequired();

            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.ModifiedAt).IsRequired(false);

            builder.HasIndex(x => x.CaseNumber).HasDatabaseName("IX_CaseLoans_CaseNumber");
            builder.HasIndex(x => x.NationalId).HasDatabaseName("IX_CaseLoans_NationalId");
            builder.HasIndex(x => x.BorrowerLastName).HasDatabaseName("IX_CaseLoans_BorrowerLastName");
            builder.HasIndex(x => x.CheckoutDate).HasDatabaseName("IX_CaseLoans_CheckoutDate");
            builder.HasIndex(x => x.IsReturned).HasDatabaseName("IX_CaseLoans_IsReturned");
            builder.HasIndex(x => x.Status).HasDatabaseName("IX_CaseLoans_Status");
        }
    }
}
