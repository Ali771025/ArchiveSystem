using MediatR;

namespace ArchiveSystem.Application.Features.UserManagement.Commands
{
    public sealed record ResetUserPasswordCommand(
        long UserId,
        string NewPassword,
        string? LastModifiedBy) : IRequest<bool>;
}
