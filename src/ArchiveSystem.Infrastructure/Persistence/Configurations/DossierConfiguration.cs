using ArchiveSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArchiveSystem.Infrastructure.Persistence.Configurations
{
    public class DossierConfiguration : IEntityTypeConfiguration<Dossier>
    {
        public void Configure(EntityTypeBuilder<Dossier> builder)
        {
            builder.ToTable("Dossiers");

            builder.HasKey(d => d.Id);
            builder.Property(d => d.Id)
                .UseIdentityColumn()
                .ValueGeneratedOnAdd();

            builder.Property(d => d.DossierNumber).HasMaxLength(100).IsRequired();
            builder.Property(d => d.ExternalCaseNumber).HasMaxLength(100).IsRequired();
            builder.Property(d => d.Address).HasMaxLength(500).IsRequired();
            builder.Property(d => d.DefaultImagePath).HasMaxLength(500).IsRequired(false);
            builder.Property(d => d.RenovationCode).HasMaxLength(100).IsRequired(false);
            builder.Property(d => d.PostalCode).HasMaxLength(10).IsRequired(false);
            builder.Property(d => d.RegistrationPlate).HasMaxLength(50).IsRequired(false);
            builder.Property(d => d.UsageType).HasConversion<int>().IsRequired();
            builder.Property(d => d.Latitude).HasColumnType("decimal(9,6)").IsRequired(false);
            builder.Property(d => d.Longitude).HasColumnType("decimal(9,6)").IsRequired(false);
            builder.Property(d => d.CreatedAt).IsRequired();
            builder.Property(d => d.ModifiedAt).IsRequired(false);
            builder.Property(d => d.DeletedAt).IsRequired(false);

            builder.HasOne(d => d.Classification)
                .WithMany(c => c.Dossiers)
                .HasForeignKey(d => d.ClassificationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(d => d.Owners)
                .WithOne(o => o.Dossier)
                .HasForeignKey(o => o.DossierId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(d => d.DynamicFieldValues)
                .WithOne(v => v.Dossier)
                .HasForeignKey(v => v.DossierId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(d => d.DossierNumber)
                .HasDatabaseName("IX_Dossiers_DossierNumber")
                .IsUnique();
            builder.HasIndex(d => d.ExternalCaseNumber)
                .HasDatabaseName("IX_Dossiers_ExternalCaseNumber");
            builder.HasIndex(d => d.ClassificationId)
                .HasDatabaseName("IX_Dossiers_ClassificationId");
            builder.HasIndex(d => d.DeletedAt)
                .HasDatabaseName("IX_Dossiers_DeletedAt");
        }
    }
}
