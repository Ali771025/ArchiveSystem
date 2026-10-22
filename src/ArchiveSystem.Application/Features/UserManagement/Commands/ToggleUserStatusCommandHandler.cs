using ArchiveSystem.Application.Features.UserManagement.Interfaces;
using MediatR;

namespace ArchiveSystem.Application.Features.UserManagement.Commands
{
    public sealed class ToggleUserStatusCommandHandler : IRequestHandler<ToggleUserStatusCommand, bool>
    {
        private readonly IUserManagementService _userManagementService;

        public ToggleUserStatusCommandHandler(IUserManagementService userManagementService)
        {
            _userManagementService = userManagementService;
        }

        public Task<bool> Handle(ToggleUserStatusCommand request, CancellationToken cancellationToken)
        {
            return _userManagementService.SetUserStatusAsync(
                request.UserId,
                request.IsActive,
                request.ModifiedBy,
                cancellationToken);
        }
    }
}
