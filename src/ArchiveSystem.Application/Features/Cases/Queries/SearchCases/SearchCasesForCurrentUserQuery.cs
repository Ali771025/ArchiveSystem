using ArchiveSystem.Application.AccessControl;
using ArchiveSystem.Application.Common.Models.DynamicFields;
using ArchiveSystem.Domain.Enums;
using MediatR;

namespace ArchiveSystem.Application.Features.Cases.Queries.SearchCases
{
    public sealed class SearchCasesForCurrentUserQuery : IRequest<SearchCasesResultDto>
    {
        public long? ClassificationId { get; init; }
        public List<DynamicFilterDto> DynamicFilters { get; init; } = new();
        public string DynamicFilterLogic { get; init; } = "and";
        public List<DynamicFilterGroupDto> DynamicFilterGroups { get; init; } = new();
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20;
        public string SortBy { get; init; } = "updatedAt";
        public string SortDirection { get; init; } = "desc";
        public string? OwnerName { get; init; }
        public string? ExternalCaseNumber { get; init; }
        public string? CreatedBy { get; init; }
        public DateTime? FromDate { get; init; }
        public DateTime? ToDate { get; init; }
        public IReadOnlyCollection<long> Tags { get; init; } = Array.Empty<long>();
    }

    public sealed class SearchCasesForCurrentUserQueryHandler : IRequestHandler<SearchCasesForCurrentUserQuery, SearchCasesResultDto>
    {
        private readonly IAccessControlService _accessControlService;
        private readonly MediatR.IMediator _mediator;
        private readonly Microsoft.AspNetCore.Http.IHttpContextAccessor _httpContextAccessor;

        public SearchCasesForCurrentUserQueryHandler(
            IAccessControlService accessControlService,
            MediatR.IMediator mediator,
            Microsoft.AspNetCore.Http.IHttpContextAccessor httpContextAccessor)
        {
            _accessControlService = accessControlService;
            _mediator = mediator;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<SearchCasesResultDto> Handle(SearchCasesForCurrentUserQuery request, CancellationToken cancellationToken)
        {
            var userId = ParseUserId();
            var isAdmin = _httpContextAccessor.HttpContext?.User.IsInRole("Admin") == true ||
                          await _accessControlService.HasGlobalPermissionAsync(userId, "admin.access", cancellationToken);

            var allowedClassifications = isAdmin
                ? Array.Empty<long>()
                : await _accessControlService.GetAccessibleClassificationIdsAsync(
                    userId,
                    ClassificationPermissionMask.Read,
                    cancellationToken);

            return await _mediator.Send(new Application.Features.DynamicFields.Queries.SearchCases.SearchCasesQuery
            {
                ClassificationId = request.ClassificationId,
                BypassAuthorizationFilters = isAdmin,
                AllowedClassificationIds = allowedClassifications,
                DynamicFilters = request.DynamicFilters,
                DynamicFilterLogic = request.DynamicFilterLogic,
                DynamicFilterGroups = request.DynamicFilterGroups,
                Page = request.Page,
                PageSize = request.PageSize,
                SortBy = request.SortBy,
                OwnerName = request.OwnerName,
                ExternalCaseNumber = request.ExternalCaseNumber,
                CreatedBy = request.CreatedBy,
                FromDate = request.FromDate,
                ToDate = request.ToDate,
                Tags = request.Tags,
                SortDescending = !string.Equals(request.SortDirection, "asc", StringComparison.OrdinalIgnoreCase)
            }, cancellationToken);
        }

        private long ParseUserId()
        {
            var claimsPrincipal = _httpContextAccessor.HttpContext?.User;
            var userIdRaw =
                claimsPrincipal?.FindFirst("sub")?.Value ??
                claimsPrincipal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            return long.TryParse(userIdRaw, out var userId) ? userId : 0;
        }
    }
}
