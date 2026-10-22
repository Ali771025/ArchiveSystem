using ArchiveSystem.Application.Features.AccessControl.DTOs;
using ArchiveSystem.Application.Features.AccessControl.Interfaces;
using MediatR;

namespace ArchiveSystem.Application.Features.AccessControl.Queries.GetRoleById
{
    public sealed class GetRoleByIdQueryHandler : IRequestHandler<GetRoleByIdQuery, RoleDto?>
    {
        private readonly IIdentityRoleService _identityRoleService;

        public GetRoleByIdQueryHandler(IIdentityRoleService identityRoleService)
        {
            _identityRoleService = identityRoleService;
        }

        public async Task<RoleDto?> Handle(GetRoleByIdQuery request, CancellationToken cancellationToken)
        {
            return await _identityRoleService.GetRoleByIdAsync(request.RoleId, cancellationToken);
        }
    }
}
