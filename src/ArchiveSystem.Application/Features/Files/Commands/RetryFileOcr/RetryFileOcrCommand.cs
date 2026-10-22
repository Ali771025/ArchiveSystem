using MediatR;

namespace ArchiveSystem.Application.Features.Files.Commands.RetryFileOcr
{
    public sealed record RetryFileOcrCommand(long FileId, long VersionId) : IRequest;
}
