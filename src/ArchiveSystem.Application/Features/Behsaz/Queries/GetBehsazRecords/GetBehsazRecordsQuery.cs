using ArchiveSystem.Application.Common.Models;
using ArchiveSystem.Application.Features.Behsaz.Interfaces;
using ArchiveSystem.Application.Features.Behsaz.Models;
using MediatR;

namespace ArchiveSystem.Application.Features.Behsaz.Queries.GetBehsazRecords
{
    public sealed class BehsazSearchValue
    {
        public string PropertyName { get; init; } = string.Empty;
        public string PropertyValue { get; init; } = string.Empty;
    }

    public sealed class GetBehsazRecordsQuery : IRequest<PaginatedResult<BehsazRecordViewModel>>
    {
        public int? TINFBuildingID { get; init; }
        public int? TBASPeopleGroupID { get; init; } // 1 = Real, 2 = Legal
        public int InstaurationFilterType { get; init; } = 1;
        public int? InstaurationArea { get; init; }
        public int? InstaurationZone { get; init; }
        public int? InstaurationBlock { get; init; }
        public int? InstaurationLand { get; init; }
        public int? InstaurationBuilding { get; init; }
        public int? InstaurationApartment { get; init; }
        public int? InstaurationBusiness { get; init; }
        public string? ManualNumber { get; init; }
        public string? Number { get; init; }
        public decimal? GhateCode { get; init; }
        public int PageSize { get; init; } = 20;
        public int PageIndex { get; init; } = 0;
        public int? UserCode { get; init; }
        public IReadOnlyList<BehsazSearchValue> SearchValues { get; init; } = Array.Empty<BehsazSearchValue>();
    }

    public sealed class BehsazRecordViewModel : BehsazRecordDto
    {
    }

    public sealed class GetBehsazRecordsQueryHandler : IRequestHandler<GetBehsazRecordsQuery, PaginatedResult<BehsazRecordViewModel>>
    {
        private readonly IBehsazSearchRepository _behsazSearchRepository;

        public GetBehsazRecordsQueryHandler(IBehsazSearchRepository behsazSearchRepository)
        {
            _behsazSearchRepository = behsazSearchRepository;
        }

        public Task<PaginatedResult<BehsazRecordViewModel>> Handle(GetBehsazRecordsQuery request, CancellationToken cancellationToken)
        {
            return _behsazSearchRepository.SearchAsync(request, cancellationToken);
        }
    }
}
