using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArchiveSystem.Infrastructure.Persistence.Configurations
{
    public class IdentityRoleConfiguration : IEntityTypeConfiguration<IdentityRole<long>>
    {
        public void Configure(EntityTypeBuilder<IdentityRole<long>> builder)
        {
            builder.ToTable("AspNetRoles");

            builder.Property(role => role.Name)
                .HasMaxLength(256);

            builder.Property(role => role.NormalizedName)
                .HasMaxLength(256);
        }
    }
}
