using MediatR;

namespace ArchiveSystem.Application.Features.UserManagement.Commands
{
    public sealed record AssignRolesCommand(
        long UserId,
        IReadOnlyCollection<string> Roles,
        string? ModifiedBy) : IRequest<bool>;
}
