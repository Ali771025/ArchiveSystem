using ArchiveSystem.Domain.Events;
using ArchiveSystem.Worker.Ocr.Services;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace ArchiveSystem.Worker.Ocr.Consumers
{
    public class FileVersionCreatedEventConsumer : IConsumer<FileVersionCreatedEvent>
    {
        private readonly OcrProcessingService _processingService;
        private readonly ILogger<FileVersionCreatedEventConsumer> _logger;

        public FileVersionCreatedEventConsumer(
            OcrProcessingService processingService,
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

            await _processingService.ProcessFileVersionAsync(
                context.Message.FileVersionId,
                context.CancellationToken,
                rethrowOnFailure: true);
        }
    }
}
