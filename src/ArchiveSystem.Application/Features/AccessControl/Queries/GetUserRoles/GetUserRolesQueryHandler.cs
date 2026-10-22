using ArchiveSystem.Application.Features.AccessControl.DTOs;
using ArchiveSystem.Application.Features.AccessControl.Interfaces;
using MediatR;

namespace ArchiveSystem.Application.Features.AccessControl.Queries.GetUserRoles
{
    public sealed class GetUserRolesQueryHandler : IRequestHandler<GetUserRolesQuery, IReadOnlyList<RoleDto>>
    {
        private readonly IIdentityRoleService _identityRoleService;

        public GetUserRolesQueryHandler(IIdentityRoleService identityRoleService)
        {
            _identityRoleService = identityRoleService;
        }

        public async Task<IReadOnlyList<RoleDto>> Handle(GetUserRolesQuery request, CancellationToken cancellationToken)
        {
            return await _identityRoleService.GetUserRolesAsync(request.UserId, cancellationToken);
        }
    }
}
