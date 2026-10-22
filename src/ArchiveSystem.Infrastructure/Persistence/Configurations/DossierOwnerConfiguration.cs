using ArchiveSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArchiveSystem.Infrastructure.Persistence.Configurations
{
    public class DossierOwnerConfiguration : IEntityTypeConfiguration<DossierOwner>
    {
        public void Configure(EntityTypeBuilder<DossierOwner> builder)
        {
            builder.ToTable("DossierOwners");

            builder.HasKey(o => o.Id);
            builder.Property(o => o.Id)
                .UseIdentityColumn()
                .ValueGeneratedOnAdd();

            builder.Property(o => o.OwnerId).IsRequired();
            builder.Property(o => o.CreatedAt).IsRequired();
            builder.Property(o => o.ModifiedAt).IsRequired(false);
            builder.Property(o => o.DeletedAt).IsRequired(false);

            builder.HasOne(o => o.Dossier)
                .WithMany(d => d.Owners)
                .HasForeignKey(o => o.DossierId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(o => o.Owner)
                .WithMany(ow => ow.DossierLinks)
                .HasForeignKey(o => o.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(o => o.DossierId)
                .HasDatabaseName("IX_DossierOwners_DossierId");
            builder.HasIndex(o => o.OwnerId)
                .HasDatabaseName("IX_DossierOwners_OwnerId");
            builder.HasIndex(o => new { o.DossierId, o.OwnerId })
                .HasDatabaseName("IX_DossierOwners_DossierId_OwnerId_Active")
                .IsUnique()
                .HasFilter("[DeletedAt] IS NULL");
            builder.HasIndex(o => o.DeletedAt)
                .HasDatabaseName("IX_DossierOwners_DeletedAt");
        }
    }
}
