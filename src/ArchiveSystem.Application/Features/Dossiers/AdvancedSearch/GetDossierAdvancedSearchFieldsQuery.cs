using ArchiveSystem.Application.Interfaces.Persistence;
using ArchiveSystem.Application.Common.Exceptions;
using ArchiveSystem.Application.Common.Interfaces.Security;
using ArchiveSystem.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ArchiveSystem.Application.Features.Dossiers.AdvancedSearch
{
    public sealed class GetDossierAdvancedSearchFieldsQuery : IRequest<DossierAdvancedSearchFieldCatalogDto>
    {
        public long? ClassificationId { get; init; }
        public IReadOnlyCollection<long> AllowedClassificationIds { get; init; } = Array.Empty<long>();
    }

    public sealed class GetDossierAdvancedSearchFieldsQueryHandler
        : IRequestHandler<GetDossierAdvancedSearchFieldsQuery, DossierAdvancedSearchFieldCatalogDto>
    {
        private readonly IDossierAdvancedSearchRepository _repository;
        private readonly IAccessControlService _accessControlService;
        private readonly ICurrentUserContext _currentUser;
        private readonly IArchiveDbContext _dbContext;

        public GetDossierAdvancedSearchFieldsQueryHandler(
            IDossierAdvancedSearchRepository repository,
            IAccessControlService accessControlService,
            ICurrentUserContext currentUser,
            IArchiveDbContext dbContext)
        {
            _repository = repository;
            _accessControlService = accessControlService;
            _currentUser = currentUser;
            _dbContext = dbContext;
        }

        public async Task<DossierAdvancedSearchFieldCatalogDto> Handle(
            GetDossierAdvancedSearchFieldsQuery request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUser.UserId
                ?? throw new ForbiddenAccessException("Authenticated user context is required.");

            var allowedClassificationIds = request.AllowedClassificationIds.Count > 0
                ? request.AllowedClassificationIds
                : await _accessControlService.GetAccessibleClassificationIdsAsync(
                    userId,
                    ClassificationPermissionMask.Read,
                    cancellationToken);

            if ((_currentUser.IsInRole("Admin") ||
                 await _accessControlService.HasGlobalPermissionAsync(userId, "admin.access", cancellationToken))
                && allowedClassificationIds.Count == 0)
            {
                allowedClassificationIds = await _dbContext.Classifications
                    .AsNoTracking()
                    .Where(node => node.IsActive)
                    .Select(node => node.Id)
                    .ToArrayAsync(cancellationToken);
            }

            if (request.ClassificationId.HasValue &&
                !allowedClassificationIds.Contains(request.ClassificationId.Value))
            {
                throw new ForbiddenAccessException("User is not allowed to read this classification.");
            }

            return await _repository.GetAvailableFieldsAsync(
                allowedClassificationIds,
                request.ClassificationId,
                cancellationToken);
        }
    }
}
