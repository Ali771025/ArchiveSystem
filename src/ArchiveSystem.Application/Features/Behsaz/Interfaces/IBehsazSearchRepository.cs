using ArchiveSystem.Application.Common.Models;
using ArchiveSystem.Application.Features.Behsaz.Queries.GetBehsazRecords;

namespace ArchiveSystem.Application.Features.Behsaz.Interfaces
{
    public interface IBehsazSearchRepository
    {
        Task<PaginatedResult<BehsazRecordViewModel>> SearchAsync(GetBehsazRecordsQuery query, CancellationToken cancellationToken);
    }
}
