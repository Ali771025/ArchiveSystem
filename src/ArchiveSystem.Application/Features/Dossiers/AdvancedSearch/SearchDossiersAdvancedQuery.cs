using ArchiveSystem.Application.Interfaces.Persistence;
using ArchiveSystem.Application.Common.Exceptions;
using ArchiveSystem.Application.Common.Interfaces.Security;
using ArchiveSystem.Domain.Enums;
using MediatR;

namespace ArchiveSystem.Application.Features.Dossiers.AdvancedSearch
{
    public sealed class SearchDossiersAdvancedQuery : IRequest<DossierAdvancedSearchResultDto>
    {
        public DossierAdvancedSearchRequestDto Request { get; init; } = new();
        public IReadOnlyCollection<long> AllowedClassificationIds { get; init; } = Array.Empty<long>();
    }

    public sealed class SearchDossiersAdvancedQueryHandler
        : IRequestHandler<SearchDossiersAdvancedQuery, DossierAdvancedSearchResultDto>
    {
        private readonly IDossierAdvancedSearchRepository _repository;
        private readonly IAccessControlService _accessControlService;
        private readonly ICurrentUserContext _currentUser;

        public SearchDossiersAdvancedQueryHandler(
            IDossierAdvancedSearchRepository repository,
            IAccessControlService accessControlService,
            ICurrentUserContext currentUser)
        {
            _repository = repository;
            _accessControlService = accessControlService;
            _currentUser = currentUser;
        }

        public async Task<DossierAdvancedSearchResultDto> Handle(
            SearchDossiersAdvancedQuery request,
            CancellationToken cancellationToken)
        {
            var allowedClassificationIds = request.AllowedClassificationIds.Count > 0
                ? request.AllowedClassificationIds
                : await _accessControlService.GetAccessibleClassificationIdsAsync(
                    _currentUser.UserId ?? throw new ForbiddenAccessException("Authenticated user context is required."),
                    ClassificationPermissionMask.Read,
                    cancellationToken);

            return await _repository.SearchAsync(
                request.Request,
                allowedClassificationIds,
                cancellationToken);
        }
    }
}
