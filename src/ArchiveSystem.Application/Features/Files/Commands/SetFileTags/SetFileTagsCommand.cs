using MediatR;

namespace ArchiveSystem.Application.Features.Files.Commands.SetFileTags
{
    public sealed class SetFileTagsCommand : IRequest<Unit>
    {
        public long FileId { get; init; }
        public IReadOnlyCollection<long> TagIds { get; init; } = Array.Empty<long>();
    }
}
