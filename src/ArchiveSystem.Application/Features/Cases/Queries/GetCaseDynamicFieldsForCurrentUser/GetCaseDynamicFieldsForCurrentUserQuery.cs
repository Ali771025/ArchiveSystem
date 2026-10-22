using ArchiveSystem.Application.AccessControl;
using ArchiveSystem.Application.Common.Models.DynamicFields;
using ArchiveSystem.Application.Features.DynamicFields.Queries.GetCaseDynamicFields;
using MediatR;

namespace ArchiveSystem.Application.Features.Cases.Queries.GetCaseDynamicFieldsForCurrentUser
{
    public sealed class GetCaseDynamicFieldsForCurrentUserQuery : IRequest<CaseDynamicFieldsDto>
    {
        public long CaseId { get; init; }
    }

    public sealed class GetCaseDynamicFieldsForCurrentUserQueryHandler : IRequestHandler<GetCaseDynamicFieldsForCurrentUserQuery, CaseDynamicFieldsDto>
    {
        private readonly IAccessControlLookupRepository _lookupRepository;
        private readonly IAccessControlService _accessControlService;
        private readonly IMediator _mediator;
        private readonly Microsoft.AspNetCore.Http.IHttpContextAccessor _httpContextAccessor;

        public GetCaseDynamicFieldsForCurrentUserQueryHandler(
            IAccessControlLookupRepository lookupRepository,
            IAccessControlService accessControlService,
            IMediator mediator,
            Microsoft.AspNetCore.Http.IHttpContextAccessor httpContextAccessor)
        {
            _lookupRepository = lookupRepository;
            _accessControlService = accessControlService;
            _mediator = mediator;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<CaseDynamicFieldsDto> Handle(GetCaseDynamicFieldsForCurrentUserQuery request, CancellationToken cancellationToken)
        {
            var classificationId = await _lookupRepository.GetClassificationIdByCaseIdAsync(request.CaseId, cancellationToken);
            var userId = ParseUserId();
            if (!classificationId.HasValue ||
                !await _accessControlService.CanReadClassificationAsync(userId, classificationId.Value, cancellationToken))
            {
                throw new ArchiveSystem.Application.Common.Exceptions.UnauthorizedAccessException(
                    "You are not allowed to read case dynamic fields.");
            }

            return await _mediator.Send(new GetCaseDynamicFieldsQuery(request.CaseId), cancellationToken);
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
