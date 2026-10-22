using ArchiveSystem.Application.Common.Models;
using ArchiveSystem.Application.Common.Interfaces.Security;
using ArchiveSystem.Application.Interfaces.Persistence;
using ArchiveSystem.Domain.Enums;
using MediatR;

namespace ArchiveSystem.Application.Features.Files.Queries.SearchFiles
{
    public class SearchFilesQueryHandler : IRequestHandler<SearchFilesQuery, PaginatedResult<FileDto>>
    {
        private readonly IFileSearchRepository _fileSearchRepository;
        private readonly ArchiveSystem.Application.Common.Interfaces.Security.IAccessControlService _accessControlService;
        private readonly ICurrentUserContext _currentUserContext;

        public SearchFilesQueryHandler(
            IFileSearchRepository fileSearchRepository,
            ArchiveSystem.Application.Common.Interfaces.Security.IAccessControlService accessControlService,
            ICurrentUserContext currentUserContext)
        {
            _fileSearchRepository = fileSearchRepository;
            _accessControlService = accessControlService;
            _currentUserContext = currentUserContext;
        }

        public async Task<PaginatedResult<FileDto>> Handle(SearchFilesQuery request, CancellationToken cancellationToken)
        {
            request.Page = request.Page < 1 ? 1 : request.Page;
            request.PageSize = request.PageSize switch
            {
                < 1 => 20,
                > 100 => 100,
                _ => request.PageSize
            };

            var userId = _currentUserContext.UserId ?? 0;
            var isAdmin = _currentUserContext.IsInRole("Admin") ||
                          await _accessControlService.HasGlobalPermissionAsync(userId, "admin.access", cancellationToken);

            var allowedClassificationIds = isAdmin
                ? Array.Empty<long>()
                : await _accessControlService.GetAccessibleClassificationIdsAsync(
                    userId,
                    ClassificationPermissionMask.Read,
                    cancellationToken);
            return await _fileSearchRepository.SearchAsync(
                request.Term,
                request.CaseId,
                request.FromDate,
                request.ToDate,
                request.Page,
                request.PageSize,
                request.SortBy,
                request.SortAscending,
                allowedClassificationIds,
                isAdmin,
                cancellationToken);
        }
    }
}
