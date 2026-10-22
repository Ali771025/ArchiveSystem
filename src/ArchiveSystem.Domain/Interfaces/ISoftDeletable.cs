namespace ArchiveSystem.Domain.Interfaces
{
    public interface ISoftDeletable
    {
        DateTime? DeletedAt { get; }
        bool IsDeleted { get; }
        void SoftDelete();
        void Restore();
    }
}
