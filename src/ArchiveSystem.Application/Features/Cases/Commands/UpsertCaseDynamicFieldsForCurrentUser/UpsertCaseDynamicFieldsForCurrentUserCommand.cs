using ArchiveSystem.Application.AccessControl;
using ArchiveSystem.Application.Common.Models.DynamicFields;
using ArchiveSystem.Application.Features.DynamicFields.Commands.UpsertCaseDynamicFields;
using MediatR;

namespace ArchiveSystem.Application.Features.Cases.Commands.UpsertCaseDynamicFieldsForCurrentUser
{
    public sealed class UpsertCaseDynamicFieldsForCurrentUserCommand : IRequest<CaseDynamicFieldsDto>
    {
        public long CaseId { get; init; }
        public List<CaseDynamicFieldValueDto> Values { get; init; } = new();
    }

    public sealed class UpsertCaseDynamicFieldsForCurrentUserCommandHandler : IRequestHandler<UpsertCaseDynamicFieldsForCurrentUserCommand, CaseDynamicFieldsDto>
    {
        private readonly IAccessControlLookupRepository _lookupRepository;
        private readonly IAccessControlService _accessControlService;
        private readonly IMediator _mediator;
        private readonly Microsoft.AspNetCore.Http.IHttpContextAccessor _httpContextAccessor;

        public UpsertCaseDynamicFieldsForCurrentUserCommandHandler(
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

        public async Task<CaseDynamicFieldsDto> Handle(UpsertCaseDynamicFieldsForCurrentUserCommand request, CancellationToken cancellationToken)
        {
            var classificationId = await _lookupRepository.GetClassificationIdByCaseIdAsync(request.CaseId, cancellationToken);
            var userId = ParseUserId();
            if (!classificationId.HasValue ||
                !await _accessControlService.CanWriteClassificationAsync(userId, classificationId.Value, cancellationToken))
            {
                throw new ArchiveSystem.Application.Common.Exceptions.UnauthorizedAccessException(
                    "شما دسترسی به ویرایش اطلاعات تکمیلی ندارید");
            }

            return await _mediator.Send(new UpsertCaseDynamicFieldsCommand
            {
                CaseId = request.CaseId,
                Values = request.Values
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
