namespace ArchiveSystem.Application.AccessControl
{
    public interface IAccessControlLookupRepository
    {
        Task<IReadOnlyList<ClassificationNode>> GetClassificationNodesAsync(CancellationToken cancellationToken = default);
        Task<long?> GetClassificationIdByCaseIdAsync(long caseId, CancellationToken cancellationToken = default);
        Task<FileAccessDescriptor?> GetFileAccessDescriptorAsync(long fileId, CancellationToken cancellationToken = default);
    }
}
