namespace ArchiveSystem.Application.Interfaces.Storage
{
    public interface IFileContentStore
    {
        Task<string> ComputeSha256Async(string bucketName, string objectKey, CancellationToken cancellationToken = default);
    }
}
