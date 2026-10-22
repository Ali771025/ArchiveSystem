using ArchiveSystem.Application.Features.UserManagement.Common;
using MediatR;

namespace ArchiveSystem.Application.Features.UserManagement.Commands
{
    public sealed record UpdateUserCommand(
        long UserId,
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
        string? LastModifiedBy) : IRequest<UserDetailsDto?>;
}
