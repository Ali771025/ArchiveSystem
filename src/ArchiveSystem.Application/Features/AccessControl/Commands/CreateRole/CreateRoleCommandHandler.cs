using ArchiveSystem.Application.Common.Interfaces.Security;
using ArchiveSystem.Application.Features.AccessControl.DTOs;
using ArchiveSystem.Application.Features.AccessControl.Interfaces;
using MediatR;

namespace ArchiveSystem.Application.Features.AccessControl.Commands.CreateRole
{
    public sealed class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, RoleDto>
    {
        private readonly IIdentityRoleService _identityRoleService;
        private readonly IAccessControlService _accessControlService;

        public CreateRoleCommandHandler(IIdentityRoleService identityRoleService, IAccessControlService accessControlService)
        {
            _identityRoleService = identityRoleService;
            _accessControlService = accessControlService;
        }

        public async Task<RoleDto> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
        {
            var role = await _identityRoleService.CreateRoleAsync(request.Name, request.IsSystem, cancellationToken);
            _accessControlService.InvalidateAllCaches();
            return role;
        }
    }
}
