namespace ArchiveSystem.Domain.Enums
{
    public enum ExportJobStatus : byte
    {
        Pending = 1,
        Running = 2,
        Completed = 3,
        Failed = 4,
        Expired = 5
    }
}
