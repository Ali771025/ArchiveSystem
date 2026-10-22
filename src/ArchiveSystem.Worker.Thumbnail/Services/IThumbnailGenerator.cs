namespace ArchiveSystem.Worker.Thumbnail.Services
{
    public interface IThumbnailGenerator
    {
        Task<ThumbnailGenerationResult> GenerateThumbnailAsync(
            Stream originalImageStream,
            CancellationToken cancellationToken = default);
    }

    public sealed record ThumbnailGenerationResult(
        byte[] Bytes,
        string ContentType,
        int Width,
        int Height);
}
