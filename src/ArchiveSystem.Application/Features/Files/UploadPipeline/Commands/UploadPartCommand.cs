using MediatR;

namespace ArchiveSystem.Application.Features.Files.UploadPipeline.Commands
{
    public class UploadPartCommand : IRequest<UploadPartResponse>
    {
        public Guid SessionId { get; init; }
        public int PartNumber { get; init; }
        public Stream Content { get; init; } = Stream.Null;
        public long ContentLength { get; init; }
    }

    public class UploadPartResponse
    {
        public int PartNumber { get; init; }
        public string ETag { get; init; } = string.Empty;
    }
}
