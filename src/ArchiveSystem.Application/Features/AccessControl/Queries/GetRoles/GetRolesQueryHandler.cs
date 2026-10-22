using ArchiveSystem.Application.Features.AccessControl.DTOs;
using ArchiveSystem.Application.Features.AccessControl.Interfaces;
using MediatR;

namespace ArchiveSystem.Application.Features.AccessControl.Queries.GetRoles
{
    public sealed class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, IReadOnlyList<RoleDto>>
    {
        private readonly IIdentityRoleService _identityRoleService;

        public GetRolesQueryHandler(IIdentityRoleService identityRoleService)
        {
            _identityRoleService = identityRoleService;
        }

        public async Task<IReadOnlyList<RoleDto>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
        {
            return await _identityRoleService.GetRolesAsync(cancellationToken);
        }
    }
}
