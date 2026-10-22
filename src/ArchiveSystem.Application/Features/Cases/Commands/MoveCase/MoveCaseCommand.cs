using MediatR;

namespace ArchiveSystem.Application.Features.Cases.Commands.MoveCase
{
    public sealed class MoveCaseCommand : IRequest<MoveCaseResult>
    {
        public long CaseId { get; init; }
        public long? SourceClassificationId { get; init; }
        public long ClassificationId { get; init; }
    }

    public sealed record MoveCaseResult(
        long CaseId,
        long SourceClassificationId,
        long TargetClassificationId);
}
