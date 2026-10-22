using ArchiveSystem.Application.Common.Models;

namespace ArchiveSystem.Application.Interfaces.Persistence
{
    public interface IFileSearchRepository
    {
        Task<PaginatedResult<FileDto>> SearchAsync(
            string? term,
            long? caseId,
            DateTime? fromDate,
            DateTime? toDate,
            int page,
            int pageSize,
            string? sortBy,
            bool sortAscending,
            IReadOnlyCollection<long> allowedClassificationIds,
            bool bypassAuthorizationFilters,
            CancellationToken cancellationToken = default);
    }
}
