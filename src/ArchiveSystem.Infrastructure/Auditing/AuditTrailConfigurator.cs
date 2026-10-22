using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Infrastructure.Identity;
using ArchiveSystem.Infrastructure.Persistence;
using Audit.Core;
using Audit.EntityFramework;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using Audit.EntityFramework.ConfigurationApi;

using AuditCoreConfiguration = Audit.Core.Configuration;
using AuditEventEntry = Audit.EntityFramework.EventEntry;
using AuditEventEntryChange = Audit.EntityFramework.EventEntryChange;


namespace ArchiveSystem.Infrastructure.Auditing
{
    public static class AuditTrailConfigurator
    {
        private static readonly HashSet<string> DefaultSensitiveFields = new(StringComparer.OrdinalIgnoreCase)
        {
            "Password",
            "PasswordHash",
            "SecurityStamp",
            "ConcurrencyStamp",
            "AccessFailedCount",
            "TwoFactorEnabled",
            "AuthenticatorKey",
            "RecoveryCodes"
        };

        public static void Configure(IConfiguration configuration)
        {
            var configuredSensitiveFields = configuration
                .GetSection("Auditing:IgnoredFields")
                .Get<string[]>();
            var sensitiveFields = configuredSensitiveFields is { Length: > 0 }
                ? new HashSet<string>(configuredSensitiveFields, StringComparer.OrdinalIgnoreCase)
                : DefaultSensitiveFields;

            AuditCoreConfiguration.Setup()
               .UseEntityFramework(ef => ef
                   .UseDbContext<ArchiveDbContext>()
                    .AuditTypeExplicitMapper(map => map
                        .Map<Dossier, AuditTrailLog>()
                        .Map<ArchiveSystem.Domain.Entities.ArchiveFile, AuditTrailLog>()
                        .Map<ApplicationUser, AuditTrailLog>()
                        .AuditEntityAction<AuditTrailLog>((eventEntry, entityEntry, auditEntity) =>
                        {
                            var changedColumnNames = entityEntry.Changes?
                                .Where(change => !sensitiveFields.Contains(change.ColumnName))
                                .Select(change => change.ColumnName)
                                .Distinct(StringComparer.OrdinalIgnoreCase)
                                .ToArray() ?? Array.Empty<string>();

                            var oldValues = BuildValueSet(
                                entityEntry.Changes,
                                change => change.OriginalValue,
                                changedColumnNames,
                                sensitiveFields);

                            var newValues = BuildValueSet(
                                entityEntry.Changes,
                                change => change.NewValue,
                                changedColumnNames,
                                sensitiveFields);

                            auditEntity.EventType = $"{entityEntry.Table}_{NormalizeAction(entityEntry.Action)}";
                            auditEntity.TableName = entityEntry.Table;
                            auditEntity.RecordId = ExtractRecordId(entityEntry);
                            auditEntity.UserId = eventEntry.CustomFields != null &&
                                eventEntry.CustomFields.TryGetValue("UserId", out var userId)
                                ? userId?.ToString()
                                : null;
                            auditEntity.ActionDate = DateTime.UtcNow;
                            auditEntity.OldValues = oldValues.Count > 0 ? JsonSerializer.Serialize(oldValues) : null;
                            auditEntity.NewValues = newValues.Count > 0 ? JsonSerializer.Serialize(newValues) : null;
                            auditEntity.AffectedColumns = changedColumnNames.Length > 0
                                ? JsonSerializer.Serialize(changedColumnNames)
                                : null;
                        })));

            AuditCoreConfiguration.AuditDisabled = false;
            AuditCoreConfiguration.CreationPolicy = Audit.Core.EventCreationPolicy.InsertOnEnd;
        }

        private static Dictionary<string, object?> BuildValueSet(
            IEnumerable<AuditEventEntryChange>? changes,
            Func<AuditEventEntryChange, object?> selector,
            IEnumerable<string> columnNames,
            ISet<string> sensitiveFields)
        {
            var allowed = new HashSet<string>(columnNames, StringComparer.OrdinalIgnoreCase);
            var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            if (changes == null)
            {
                return result;
            }

            foreach (var change in changes)
            {
                if (!allowed.Contains(change.ColumnName) || sensitiveFields.Contains(change.ColumnName))
                {
                    continue;
                }

                result[change.ColumnName] = selector(change);
            }

            return result;
        }

        private static string NormalizeAction(string action)
        {
            return action switch
            {
                "Insert" => "Create",
                "Update" => "Update",
                "Delete" => "Delete",
                _ => action
            };
        }

        private static string ExtractRecordId(AuditEventEntry entry)
        {
            if (entry.PrimaryKey == null || entry.PrimaryKey.Count == 0)
            {
                return string.Empty;
            }

            return string.Join("|", entry.PrimaryKey
                .OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
                .Select(kvp => kvp.Value?.ToString() ?? string.Empty));
        }
    }
}
