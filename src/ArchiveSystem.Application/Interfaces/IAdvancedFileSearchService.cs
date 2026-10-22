using ArchiveSystem.Application.Common.Models.AdvancedSearch;

namespace ArchiveSystem.Application.Interfaces
{
    public interface IAdvancedFileSearchService
    {
        Task<PagedSearchResultDto<SearchResultItemDto>> AdvancedSearchAsync(
            AdvancedSearchQueryDto query,
            CancellationToken cancellationToken = default);
    }
}
