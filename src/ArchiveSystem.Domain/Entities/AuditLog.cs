// src/ArchiveSystem.Domain/Entities/AuditLog.cs
namespace ArchiveSystem.Domain.Entities
{
    public class AuditLog : BaseEntity<long>
    {
        public long UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public string RecordId { get; set; } = string.Empty;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public string Severity { get; set; } = "Information";

        public AuditLog(long userId, string userName, string action, string tableName, string recordId, string? oldValue, string? newValue, string ipAddress, string userAgent, string severity = "Information")
        {
            UserId = userId;
            UserName = userName;
            Action = action;
            TableName = tableName;
            RecordId = recordId;
            OldValue = oldValue;
            NewValue = newValue;
            IpAddress = ipAddress;
            UserAgent = userAgent;
            Severity = severity;
        }
    }
}