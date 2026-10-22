using ArchiveSystem.Domain.Entities;

namespace ArchiveSystem.Application.Features.Files.Security
{
    public interface IFileAccessAuthorizer
    {
        Task EnsureCanViewFileAsync(long fileId, CancellationToken cancellationToken);
        Task EnsureCanWriteFileAsync(long fileId, CancellationToken cancellationToken);
        Task EnsureCanWriteSessionAsync(UploadSession session, CancellationToken cancellationToken);
        Task<long?> ResolveInitiateUploadClassificationAsync(
            long? caseId,
            long? classificationId,
            CancellationToken cancellationToken);
        Task EnsureCanAssignTagsAsync(long fileId, IReadOnlyCollection<long> tagIds, CancellationToken cancellationToken);
        long CurrentUserId { get; }
    }
}
