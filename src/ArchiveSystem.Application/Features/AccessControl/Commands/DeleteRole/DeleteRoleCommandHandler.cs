using ArchiveSystem.Application.Common.Interfaces.Security;
using ArchiveSystem.Application.Features.AccessControl.Interfaces;
using MediatR;

namespace ArchiveSystem.Application.Features.AccessControl.Commands.DeleteRole
{
    public sealed class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand, Unit>
    {
        private readonly IIdentityRoleService _identityRoleService;
        private readonly IAccessControlService _accessControlService;

        public DeleteRoleCommandHandler(IIdentityRoleService identityRoleService, IAccessControlService accessControlService)
        {
            _identityRoleService = identityRoleService;
            _accessControlService = accessControlService;
        }

        public async Task<Unit> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
        {
            await _identityRoleService.DeleteRoleAsync(request.RoleId, cancellationToken);
            _accessControlService.InvalidateAllCaches();

            return Unit.Value;
        }
    }
}
