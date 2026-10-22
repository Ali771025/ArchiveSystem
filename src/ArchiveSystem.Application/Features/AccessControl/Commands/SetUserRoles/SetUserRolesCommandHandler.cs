using AccessControlUserRoleRepository = ArchiveSystem.Application.AccessControl.IUserRoleRepository;
using ArchiveSystem.Application.Common.Interfaces.Security;
using ArchiveSystem.Application.Features.AccessControl.Interfaces;
using MediatR;

namespace ArchiveSystem.Application.Features.AccessControl.Commands.SetUserRoles
{
    public sealed class SetUserRolesCommandHandler : IRequestHandler<SetUserRolesCommand, Unit>
    {
        private readonly IIdentityRoleService _identityRoleService;
        private readonly AccessControlUserRoleRepository _userRoleRepository;
        private readonly IAccessControlService _accessControlService;

        public SetUserRolesCommandHandler(
            IIdentityRoleService identityRoleService,
            AccessControlUserRoleRepository userRoleRepository,
            IAccessControlService accessControlService)
        {
            _identityRoleService = identityRoleService;
            _userRoleRepository = userRoleRepository;
            _accessControlService = accessControlService;
        }

        public async Task<Unit> Handle(SetUserRolesCommand request, CancellationToken cancellationToken)
        {
            await _identityRoleService.SetUserRolesAsync(request.UserId, request.RoleIds, cancellationToken);
            await _userRoleRepository.SetUserRolesAsync(request.UserId, request.RoleIds, cancellationToken);
            _accessControlService.InvalidateUserCache(request.UserId);

            return Unit.Value;
        }
    }
}
