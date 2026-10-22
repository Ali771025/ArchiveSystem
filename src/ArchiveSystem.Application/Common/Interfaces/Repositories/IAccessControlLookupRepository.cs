using ArchiveSystem.Application.Features.AccessControl.Models;

namespace ArchiveSystem.Application.Common.Interfaces.Repositories
{
    public interface IAccessControlLookupRepository
    {
        Task<IReadOnlyList<ClassificationNode>> GetClassificationNodesAsync(CancellationToken cancellationToken = default);
        Task<long?> GetClassificationIdByCaseIdAsync(long caseId, CancellationToken cancellationToken = default);
        Task<FileAccessDescriptor?> GetFileAccessDescriptorAsync(long fileId, CancellationToken cancellationToken = default);
    }
}
