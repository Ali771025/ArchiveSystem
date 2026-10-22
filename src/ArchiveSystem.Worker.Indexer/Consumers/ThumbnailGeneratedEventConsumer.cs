// src/ArchiveSystem.Worker.Indexer/Consumers/ThumbnailGeneratedEventConsumer.cs
using MassTransit;
using Microsoft.Extensions.Logging;
using ArchiveSystem.Domain.Events;
using ArchiveSystem.Domain.Repositories;

namespace ArchiveSystem.Worker.Indexer.Consumers
{
    public class ThumbnailGeneratedEventConsumer : IConsumer<ThumbnailGeneratedEvent>
    {
        private readonly IFileRepository _fileRepository;
        private readonly ILogger<ThumbnailGeneratedEventConsumer> _logger;

        public ThumbnailGeneratedEventConsumer(
            IFileRepository fileRepository,
            ILogger<ThumbnailGeneratedEventConsumer> logger)
        {
            _fileRepository = fileRepository;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<ThumbnailGeneratedEvent> context)
        {
            var message = context.Message;
            _logger.LogInformation("Processing Thumbnail Generated Event for file ID: {FileId}", message.FileId);

            try
            {
                // Fetch file metadata from SQL
                var file = await _fileRepository.GetByIdAsync(message.FileId);
                if (file == null)
                {
                    _logger.LogWarning("File not found in database: {FileId}", message.FileId);
                    return;
                }

                _logger.LogInformation(
                    "Thumbnail generated for file {FileId}, version {FileVersionId}, bytes {BytesLength}, dimensions {Width}x{Height}",
                    message.FileId,
                    message.FileVersionId,
                    message.BytesLength,
                    message.Width,
                    message.Height);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Thumbnail Generated Event for file ID: {FileId}", message.FileId);
                throw; // Re-throw to trigger retry mechanism
            }
        }
    }
}
