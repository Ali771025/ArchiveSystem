// src/ArchiveSystem.Worker.Ocr/Services/IOcrService.cs
namespace ArchiveSystem.Worker.Ocr.Services
{
    public interface IOcrService
    {
        Task<OcrResult> ExtractAsync(
            Stream fileStream,
            string fileName,
            string contentType,
            CancellationToken cancellationToken = default);
    }
}
