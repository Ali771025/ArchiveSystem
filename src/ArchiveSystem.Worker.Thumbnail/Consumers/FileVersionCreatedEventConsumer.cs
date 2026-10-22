using ArchiveSystem.Domain.Events;
using ArchiveSystem.Worker.Thumbnail.Services;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace ArchiveSystem.Worker.Thumbnail.Consumers
{
    public class FileVersionCreatedEventConsumer : IConsumer<FileVersionCreatedEvent>
    {
        private readonly ThumbnailProcessingService _processingService;
        private readonly ILogger<FileVersionCreatedEventConsumer> _logger;

        public FileVersionCreatedEventConsumer(
            ThumbnailProcessingService processingService,
            ILogger<FileVersionCreatedEventConsumer> logger)
        {
            _processingService = processingService;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<FileVersionCreatedEvent> context)
        {
            _logger.LogInformation(
                "Received FileVersionCreatedEvent for file {FileId}, version {FileVersionId}",
                context.Message.FileId,
                context.Message.FileVersionId);

            await _processingService.ProcessFileVersionAsync(context.Message.FileVersionId, context.CancellationToken);
        }
    }
}
