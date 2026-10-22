// src/ArchiveSystem.Application/Features/Audit/Handlers/FileDownloadedEventHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Domain.Events;
using ArchiveSystem.Domain.Repositories;

namespace ArchiveSystem.Application.Features.Audit.Handlers
{
    public class FileDownloadedEventHandler : INotificationHandler<FileDownloadedEvent>
    {
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly ILogger<FileDownloadedEventHandler> _logger;

        public FileDownloadedEventHandler(IAuditLogRepository auditLogRepository, ILogger<FileDownloadedEventHandler> logger)
        {
            _auditLogRepository = auditLogRepository;
            _logger = logger;
        }

        public async Task Handle(FileDownloadedEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                var auditLog = new AuditLog(
                    userId: notification.UserId,
                    userName: "Unknown",
                    action: "Download",
                    tableName: "Files",
                    recordId: notification.FileId.ToString(),
                    oldValue: null,
                    newValue: "File download initiated",
                    ipAddress: notification.IpAddress,
                    userAgent: notification.UserAgent
                );

                await _auditLogRepository.AddAsync(auditLog, cancellationToken);
                await _auditLogRepository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Audit log created for file download: User {UserId}, File {FileId}", 
                    notification.UserId, notification.FileId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create audit log for file download: User {UserId}, File {FileId}", 
                    notification.UserId, notification.FileId);
            }
        }
    }
}
