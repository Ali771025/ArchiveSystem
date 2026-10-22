using ArchiveSystem.Application.Features.UserManagement.Common;
using ArchiveSystem.Application.Features.UserManagement.Interfaces;
using MediatR;

namespace ArchiveSystem.Application.Features.UserManagement.Commands
{
    public sealed class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, UserDetailsDto?>
    {
        private readonly IUserManagementService _userManagementService;

        public UpdateUserCommandHandler(IUserManagementService userManagementService)
        {
            _userManagementService = userManagementService;
        }

        public Task<UserDetailsDto?> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            return _userManagementService.UpdateUserAsync(
                new UpdateUserManagementRequest(
                    request.UserId,
                    request.Username,
                    request.Email,
                    request.PhoneNumber,
                    request.FirstName,
                    request.LastName,
                    request.NationalId,
                    request.PersonnelCode,
                    request.ActiveDuration,
                    request.AutoBackupEnabled,
                    request.TwoFactorEnabled,
                    request.LastModifiedBy),
                cancellationToken);
        }
    }
}
