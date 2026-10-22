// src/ArchiveSystem.Infrastructure/Services/AuditService.cs
using Microsoft.EntityFrameworkCore;
using ArchiveSystem.Application.Interfaces;
using ArchiveSystem.Application.Features.Reports.Interfaces;
using ArchiveSystem.Application.Features.Reports.Models;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Domain.Enums;
using FileEntity = ArchiveSystem.Domain.Entities.ArchiveFile;

namespace ArchiveSystem.Infrastructure.Services
{
    public class AuditService : IAuditService
    {
        private readonly ArchiveDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAuditWriter _auditWriter;

        public AuditService(ArchiveDbContext context, IHttpContextAccessor httpContextAccessor, IAuditWriter auditWriter)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _auditWriter = auditWriter;
        }

        public async Task LogOperationAsync(
            long userId, 
            string userName, 
            string action, 
            string tableName, 
            string recordId, 
            string? oldValue, 
            string? newValue, 
            string ipAddress, 
            string userAgent, 
            string severity = "Information")
        {
            var auditLog = new AuditLog(
                userId, 
                userName, 
                action, 
                tableName, 
                recordId, 
                oldValue, 
                newValue, 
                ipAddress, 
                userAgent, 
                severity
            );

            await _context.AuditLogs.AddAsync(auditLog);
            await _context.SaveChangesAsync();

            await _auditWriter.WriteAsync(
                BuildMovementLog(
                    userId,
                    userName,
                    action,
                    tableName,
                    recordId,
                    oldValue,
                    newValue,
                    ipAddress,
                    userAgent),
                CancellationToken.None);
        }

        public async Task LogFileOperationAsync(long userId, string userName, FileEntity file, string action, string ipAddress, string userAgent)
        {
            var oldValue = await GetFileSnapshotAsync(file.Id);
            var newValue = SerializeFile(file);
            
            await LogOperationAsync(
                userId, 
                userName, 
                action, 
                "Files", 
                file.Id.ToString(), 
                oldValue, 
                newValue, 
                ipAddress, 
                userAgent
            );
        }

        public async Task LogCaseOperationAsync(long userId, string userName, Case caseEntity, string action, string ipAddress, string userAgent)
        {
            var oldValue = await GetCaseSnapshotAsync(caseEntity.Id);
            var newValue = SerializeCase(caseEntity);
            
            await LogOperationAsync(
                userId, 
                userName, 
                action, 
                "Cases", 
                caseEntity.Id.ToString(), 
                oldValue, 
                newValue, 
                ipAddress, 
                userAgent
            );
        }

        private async Task<string?> GetFileSnapshotAsync(long fileId)
        {
            var file = await _context.Files.FindAsync(fileId);
            return file != null ? SerializeFile(file) : null;
        }

        private async Task<string?> GetCaseSnapshotAsync(long caseId)
        {
            var caseEntity = await _context.Cases.FindAsync(caseId);
            return caseEntity != null ? SerializeCase(caseEntity) : null;
        }

        private string SerializeFile(FileEntity file)
        {
            return System.Text.Json.JsonSerializer.Serialize(new
            {
                Id = file.Id,
                Name = file.Name,
                Size = file.Size.Bytes,
                Extension = file.Extension.Value,
                ContentType = file.ContentType,
                StoragePath = file.StoragePath,
                Status = file.Status,
                CaseId = file.CaseId,
                ClassificationId = file.Case?.ClassificationId,
                CreatedAt = file.CreatedAt,
                ModifiedAt = file.ModifiedAt,
                Tags = file.Tags
            });
        }

        private string SerializeCase(Case caseEntity)
        {
            return System.Text.Json.JsonSerializer.Serialize(new
            {
                Id = caseEntity.Id,
                CaseNumber = caseEntity.CaseNumber,
                Title = caseEntity.Title,
                ClassificationId = caseEntity.ClassificationId,
                Owners = string.Join("، ", ArchiveSystem.Application.Common.OwnerDisplayNameParser.ParseNames(caseEntity.Owners)),
                Location = caseEntity.Location,
                ClosedAt = caseEntity.ClosedAt,
                CreatedAt = caseEntity.CreatedAt,
                ModifiedAt = caseEntity.ModifiedAt,
                DeletedAt = caseEntity.DeletedAt
            });
        }

        private static MovementLogWriteModel BuildMovementLog(
            long userId,
            string userName,
            string action,
            string tableName,
            string recordId,
            string? oldValue,
            string? newValue,
            string ipAddress,
            string userAgent)
        {
            var model = new MovementLogWriteModel
            {
                ActorUserId = userId,
                ActorName = userName,
                ActionType = action,
                EntityType = tableName,
                EntityId = recordId,
                BeforeJson = oldValue,
                AfterJson = newValue,
                Ip = ipAddress,
                UserAgent = userAgent
            };

            if (long.TryParse(recordId, out var id))
            {
                if (string.Equals(tableName, "Cases", StringComparison.OrdinalIgnoreCase))
                {
                    model.CaseId = id;
                    model.ClassificationId = ExtractLongProperty(newValue, "ClassificationId") ?? ExtractLongProperty(oldValue, "ClassificationId");
                }
                else if (string.Equals(tableName, "Files", StringComparison.OrdinalIgnoreCase))
                {
                    model.FileId = id;
                    model.CaseId = ExtractLongProperty(newValue, "CaseId") ?? ExtractLongProperty(oldValue, "CaseId");
                    model.ClassificationId = ExtractLongProperty(newValue, "ClassificationId") ?? ExtractLongProperty(oldValue, "ClassificationId");
                }
                else if (string.Equals(tableName, "FileVersions", StringComparison.OrdinalIgnoreCase))
                {
                    model.FileVersionId = id;
                    model.FileId = ExtractLongProperty(newValue, "FileId") ?? ExtractLongProperty(oldValue, "FileId");
                    model.CaseId = ExtractLongProperty(newValue, "CaseId") ?? ExtractLongProperty(oldValue, "CaseId");
                    model.ClassificationId = ExtractLongProperty(newValue, "ClassificationId") ?? ExtractLongProperty(oldValue, "ClassificationId");
                }
            }

            return model;
        }

        private static long? ExtractLongProperty(string? json, string propertyName)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                using var document = System.Text.Json.JsonDocument.Parse(json);
                var property = document.RootElement
                    .EnumerateObject()
                    .FirstOrDefault(candidate => string.Equals(candidate.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                    .Value;
                if (property.ValueKind == System.Text.Json.JsonValueKind.Undefined)
                {
                    return null;
                }

                return property.ValueKind switch
                {
                    System.Text.Json.JsonValueKind.Number when property.TryGetInt64(out var value) => value,
                    System.Text.Json.JsonValueKind.String when long.TryParse(property.GetString(), out var value) => value,
                    _ => null
                };
            }
            catch
            {
                return null;
            }
        }
    }
}
