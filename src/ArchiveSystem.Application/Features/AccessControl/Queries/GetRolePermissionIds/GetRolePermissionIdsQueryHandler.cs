using ArchiveSystem.Application.Common.Interfaces.Repositories;
using MediatR;

namespace ArchiveSystem.Application.Features.AccessControl.Queries.GetRolePermissionIds
{
    public sealed class GetRolePermissionIdsQueryHandler : IRequestHandler<GetRolePermissionIdsQuery, IReadOnlyCollection<long>>
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IPermissionRepository _permissionRepository;

        public GetRolePermissionIdsQueryHandler(IRoleRepository roleRepository, IPermissionRepository permissionRepository)
        {
            _roleRepository = roleRepository;
            _permissionRepository = permissionRepository;
        }

        public async Task<IReadOnlyCollection<long>> Handle(GetRolePermissionIdsQuery request, CancellationToken cancellationToken)
        {
            var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
            if (role is null)
            {
                return Array.Empty<long>();
            }

            var permissions = await _permissionRepository.GetByRoleIdAsync(request.RoleId, cancellationToken);
            return permissions.Select(x => x.Id).ToArray();
        }
    }
}
