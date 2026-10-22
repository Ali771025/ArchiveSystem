using MediatR;

namespace ArchiveSystem.Application.Features.Cases.Commands.DeleteCase
{
    public sealed class DeleteCaseCommand : IRequest<DeleteCaseResult>
    {
        public long CaseId { get; init; }
    }

    public sealed record DeleteCaseResult(
        long CaseId,
        long SourceClassificationId,
        long ArchiveClassificationId);
}
