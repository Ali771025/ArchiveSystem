using ArchiveSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArchiveSystem.Infrastructure.Persistence.Configurations
{
    public class MovementLogConfiguration : IEntityTypeConfiguration<MovementLog>
    {
        public void Configure(EntityTypeBuilder<MovementLog> builder)
        {
            builder.ToTable("MovementLogs");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id)
                .UseIdentityColumn()
                .ValueGeneratedOnAdd();

            builder.Property(x => x.Timestamp).IsRequired();
            builder.Property(x => x.ActorUserId).IsRequired();
            builder.Property(x => x.ActorName).HasMaxLength(200).IsRequired(false);
            builder.Property(x => x.ActionType).HasMaxLength(100).IsRequired();
            builder.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
            builder.Property(x => x.EntityId).HasMaxLength(128).IsRequired();
            builder.Property(x => x.ClassificationId).IsRequired(false);
            builder.Property(x => x.CaseId).IsRequired(false);
            builder.Property(x => x.FileId).IsRequired(false);
            builder.Property(x => x.FileVersionId).IsRequired(false);
            builder.Property(x => x.TagId).IsRequired(false);
            builder.Property(x => x.BeforeJson).HasMaxLength(2000).IsRequired(false);
            builder.Property(x => x.AfterJson).HasMaxLength(2000).IsRequired(false);
            builder.Property(x => x.Ip).HasMaxLength(64).IsRequired(false);
            builder.Property(x => x.UserAgent).HasMaxLength(512).IsRequired(false);
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.ModifiedAt).IsRequired(false);

            builder.HasIndex(x => x.Timestamp).HasDatabaseName("IX_MovementLogs_Timestamp");
            builder.HasIndex(x => new { x.ActorUserId, x.Timestamp }).HasDatabaseName("IX_MovementLogs_ActorUserId_Timestamp");
            builder.HasIndex(x => new { x.ActionType, x.Timestamp }).HasDatabaseName("IX_MovementLogs_ActionType_Timestamp");
            builder.HasIndex(x => new { x.ClassificationId, x.Timestamp }).HasDatabaseName("IX_MovementLogs_ClassificationId_Timestamp");
            builder.HasIndex(x => new { x.CaseId, x.Timestamp }).HasDatabaseName("IX_MovementLogs_CaseId_Timestamp");
            builder.HasIndex(x => new { x.FileId, x.Timestamp }).HasDatabaseName("IX_MovementLogs_FileId_Timestamp");
            builder.HasIndex(x => new { x.TagId, x.Timestamp }).HasDatabaseName("IX_MovementLogs_TagId_Timestamp");
        }
    }
}
