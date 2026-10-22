using ArchiveSystem.Application.Common.Exceptions;
using ArchiveSystem.Application.Features.UserManagement.Common;
using ArchiveSystem.Application.Features.UserManagement.Interfaces;
using MediatR;

namespace ArchiveSystem.Application.Features.UserManagement.Commands.CreateUser
{
    public sealed class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, UserDetailsDto>
    {
        private readonly IUserManagementService _userManagementService;

        public CreateUserCommandHandler(IUserManagementService userManagementService)
        {
            _userManagementService = userManagementService;
        }

        public async Task<UserDetailsDto> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManagementService.CreateUserAsync(
                new CreateUserManagementRequest(
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
                    request.Password,
                    request.Roles,
                    request.CreatedBy),
                cancellationToken);

            if (user is null)
            {
                throw new ConflictException("Unable to create user.");
            }

            return user;
        }
    }
}
