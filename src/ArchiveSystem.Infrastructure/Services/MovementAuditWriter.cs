using ArchiveSystem.Application.Features.Reports.Interfaces;
using ArchiveSystem.Application.Features.Reports.Models;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Infrastructure.Persistence;

namespace ArchiveSystem.Infrastructure.Services
{
    public class MovementAuditWriter : IAuditWriter
    {
        private const int MaxJsonLength = 2000;
        private readonly ArchiveDbContext _dbContext;

        public MovementAuditWriter(ArchiveDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task WriteAsync(MovementLogWriteModel model, CancellationToken cancellationToken = default)
        {
            var entity = new MovementLog(
                actorUserId: model.ActorUserId,
                actionType: TrimTo(model.ActionType, 100) ?? "UNKNOWN",
                entityType: TrimTo(model.EntityType, 100) ?? "Unknown",
                entityId: TrimTo(model.EntityId, 128) ?? "0",
                timestamp: model.Timestamp ?? DateTime.UtcNow,
                actorName: TrimTo(model.ActorName, 200),
                classificationId: model.ClassificationId,
                caseId: model.CaseId,
                fileId: model.FileId,
                fileVersionId: model.FileVersionId,
                tagId: model.TagId,
                beforeJson: TrimTo(model.BeforeJson, MaxJsonLength),
                afterJson: TrimTo(model.AfterJson, MaxJsonLength),
                ip: TrimTo(model.Ip, 64),
                userAgent: TrimTo(model.UserAgent, 512));

            await _dbContext.MovementLogs.AddAsync(entity, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        private static string? TrimTo(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.IsNullOrWhiteSpace(value) ? null : value;
            }

            return value.Length <= maxLength ? value : value[..maxLength];
        }
    }
}
