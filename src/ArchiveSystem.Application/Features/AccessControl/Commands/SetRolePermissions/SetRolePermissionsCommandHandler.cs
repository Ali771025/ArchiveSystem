using ArchiveSystem.Application.Common.Exceptions;
using ArchiveSystem.Application.Common.Interfaces.Repositories;
using ArchiveSystem.Application.Common.Interfaces.Security;
using MediatR;

namespace ArchiveSystem.Application.Features.AccessControl.Commands.SetRolePermissions
{
    public sealed class SetRolePermissionsCommandHandler : IRequestHandler<SetRolePermissionsCommand, Unit>
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IPermissionRepository _permissionRepository;
        private readonly IUserRoleRepository _userRoleRepository;
        private readonly IAccessControlService _accessControlService;

        public SetRolePermissionsCommandHandler(
            IRoleRepository roleRepository,
            IPermissionRepository permissionRepository,
            IUserRoleRepository userRoleRepository,
            IAccessControlService accessControlService)
        {
            _roleRepository = roleRepository;
            _permissionRepository = permissionRepository;
            _userRoleRepository = userRoleRepository;
            _accessControlService = accessControlService;
        }

        public async Task<Unit> Handle(SetRolePermissionsCommand request, CancellationToken cancellationToken)
        {
            var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
            if (role is null)
            {
                throw new NotFoundException($"Role with ID {request.RoleId} was not found.");
            }

            await _permissionRepository.SetRolePermissionsAsync(request.RoleId, request.PermissionIds, cancellationToken);

            var affectedUserIds = await _userRoleRepository.GetUserIdsByRoleIdsAsync(new[] { request.RoleId }, cancellationToken);
            foreach (var userId in affectedUserIds)
            {
                _accessControlService.InvalidateUserCache(userId);
            }

            return Unit.Value;
        }
    }
}
