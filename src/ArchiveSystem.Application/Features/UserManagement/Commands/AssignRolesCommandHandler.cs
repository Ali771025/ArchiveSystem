using ArchiveSystem.Application.Features.UserManagement.Interfaces;
using MediatR;

namespace ArchiveSystem.Application.Features.UserManagement.Commands
{
    public sealed class AssignRolesCommandHandler : IRequestHandler<AssignRolesCommand, bool>
    {
        private readonly IUserManagementService _userManagementService;

        public AssignRolesCommandHandler(IUserManagementService userManagementService)
        {
            _userManagementService = userManagementService;
        }

        public Task<bool> Handle(AssignRolesCommand request, CancellationToken cancellationToken)
        {
            return _userManagementService.AssignRolesAsync(
                request.UserId,
                request.Roles,
                request.ModifiedBy,
                cancellationToken);
        }
    }
}
