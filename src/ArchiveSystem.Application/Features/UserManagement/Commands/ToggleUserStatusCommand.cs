using MediatR;

namespace ArchiveSystem.Application.Features.UserManagement.Commands
{
    public sealed record ToggleUserStatusCommand(
        long UserId,
        bool IsActive,
        string? ModifiedBy) : IRequest<bool>;
}
