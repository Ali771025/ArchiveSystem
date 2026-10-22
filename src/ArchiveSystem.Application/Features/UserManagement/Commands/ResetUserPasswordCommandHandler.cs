using ArchiveSystem.Application.Features.UserManagement.Interfaces;
using MediatR;

namespace ArchiveSystem.Application.Features.UserManagement.Commands
{
    public sealed class ResetUserPasswordCommandHandler : IRequestHandler<ResetUserPasswordCommand, bool>
    {
        private readonly IUserManagementService _userManagementService;

        public ResetUserPasswordCommandHandler(IUserManagementService userManagementService)
        {
            _userManagementService = userManagementService;
        }

        public Task<bool> Handle(ResetUserPasswordCommand request, CancellationToken cancellationToken)
        {
            return _userManagementService.ResetPasswordAsync(
                request.UserId,
                request.NewPassword,
                request.LastModifiedBy,
                cancellationToken);
        }
    }
}
