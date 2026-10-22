using ArchiveSystem.Application.Common.Interfaces.Repositories;
using ArchiveSystem.Application.Features.AccessControl.DTOs;
using MediatR;

namespace ArchiveSystem.Application.Features.AccessControl.Queries.GetPermissions
{
    public sealed class GetPermissionsQueryHandler : IRequestHandler<GetPermissionsQuery, IReadOnlyList<PermissionDto>>
    {
        private readonly IPermissionRepository _permissionRepository;

        public GetPermissionsQueryHandler(IPermissionRepository permissionRepository)
        {
            _permissionRepository = permissionRepository;
        }

        public async Task<IReadOnlyList<PermissionDto>> Handle(GetPermissionsQuery request, CancellationToken cancellationToken)
        {
            var permissions = await _permissionRepository.GetAllAsync(cancellationToken);
            return permissions
                .OrderBy(x => x.Key)
                .Select(x => new PermissionDto(x.Id, x.Key, x.Description))
                .ToArray();
        }
    }
}
