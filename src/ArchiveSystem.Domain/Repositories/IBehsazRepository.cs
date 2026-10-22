using ArchiveSystem.Domain.Common;
using ArchiveSystem.Domain.Integrations;

namespace ArchiveSystem.Domain.Repositories
{
    public interface IBehsazRepository
    {
        Task<PagedResult<BehsazSearchResultDto>> SearchAsync(
            BehsazSearchCriteria criteria,
            CancellationToken cancellationToken = default);
    }
}
