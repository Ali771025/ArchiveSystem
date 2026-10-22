using ArchiveSystem.Application.Features.UserManagement.Common;
using MediatR;

namespace ArchiveSystem.Application.Features.UserManagement.Commands.CreateUser
{
    public sealed record CreateUserCommand(
        string Username,
        string Email,
        string? PhoneNumber,
        string FirstName,
        string LastName,
        string? NationalId,
        string? PersonnelCode,
        TimeSpan ActiveDuration,
        bool AutoBackupEnabled,
        bool TwoFactorEnabled,
        string Password,
        IReadOnlyCollection<string> Roles,
        string? CreatedBy) : IRequest<UserDetailsDto>;
}
