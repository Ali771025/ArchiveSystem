namespace ArchiveSystem.Domain.Enums
{
    public enum UploadSessionStatus
    {
        Initiated = 0,
        Uploading = 1,
        Finalizing = 2,
        Completed = 3,
        Aborted = 4,
        Failed = 5
    }
}
