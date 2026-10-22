using ArchiveSystem.Application.Common.Interfaces.Security;
using ArchiveSystem.Application.Features.AccessControl.DTOs;
using ArchiveSystem.Application.Features.AccessControl.Interfaces;
using MediatR;

namespace ArchiveSystem.Application.Features.AccessControl.Commands.UpdateRole
{
    public sealed class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand, RoleDto>
    {
        private readonly IIdentityRoleService _identityRoleService;
        private readonly IAccessControlService _accessControlService;

        public UpdateRoleCommandHandler(IIdentityRoleService identityRoleService, IAccessControlService accessControlService)
        {
            _identityRoleService = identityRoleService;
            _accessControlService = accessControlService;
        }

        public async Task<RoleDto> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
        {
            var role = await _identityRoleService.UpdateRoleAsync(request.RoleId, request.Name, cancellationToken);
            _accessControlService.InvalidateAllCaches();
            return role;
        }
    }
}
