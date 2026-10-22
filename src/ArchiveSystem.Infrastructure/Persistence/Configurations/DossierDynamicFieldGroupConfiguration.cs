using ArchiveSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArchiveSystem.Infrastructure.Persistence.Configurations
{
    public class DossierDynamicFieldGroupConfiguration : IEntityTypeConfiguration<DossierDynamicFieldGroup>
    {
        public void Configure(EntityTypeBuilder<DossierDynamicFieldGroup> builder)
        {
            builder.ToTable("DossierDynamicFieldGroups");

            builder.HasKey(g => g.Id);
            builder.Property(g => g.Id).ValueGeneratedNever();

            builder.Property(g => g.Title).HasMaxLength(150).IsRequired();
            builder.Property(g => g.DisplayOrder).IsRequired();
            builder.Property(g => g.CreatedAt).IsRequired();
            builder.Property(g => g.ModifiedAt).IsRequired(false);
            builder.Property(g => g.DeletedAt).IsRequired(false);

            builder.HasMany(g => g.Fields)
                .WithOne(f => f.Group)
                .HasForeignKey(f => f.GroupId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(g => g.DisplayOrder)
                .HasDatabaseName("IX_DossierDynamicFieldGroups_DisplayOrder");
            builder.HasIndex(g => g.DeletedAt)
                .HasDatabaseName("IX_DossierDynamicFieldGroups_DeletedAt");
        }
    }
}
