namespace ArchiveSystem.Domain.Identity
{
    public abstract class AuditableEntity
    {
        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastModified { get; set; }
        public string? LastModifiedBy { get; set; }
    }
}
