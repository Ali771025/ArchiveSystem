// src/ArchiveSystem.Application/Interfaces/IAuditService.cs
using ArchiveSystem.Domain.Entities;
using FileEntity = ArchiveSystem.Domain.Entities.ArchiveFile;

namespace ArchiveSystem.Application.Interfaces
{
    public interface IAuditService
    {
        Task LogOperationAsync(long userId, string userName, string action, string tableName, string recordId, string? oldValue, string? newValue, string ipAddress, string userAgent, string severity = "Information");
        Task LogFileOperationAsync(long userId, string userName, FileEntity file, string action, string ipAddress, string userAgent);
        Task LogCaseOperationAsync(long userId, string userName, Case caseEntity, string action, string ipAddress, string userAgent);
    }
}
