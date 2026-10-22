using ArchiveSystem.Worker.Thumbnail.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Tiff;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace ArchiveSystem.Worker.Thumbnail.Services
{
    public sealed class ThumbnailGenerator : IThumbnailGenerator
    {
        private static readonly HashSet<string> SupportedInputFormats = new(StringComparer.OrdinalIgnoreCase)
        {
            "JPEG",
            "PNG",
            "WEBP",
            "BMP",
            "GIF",
            "TIFF"
        };

        private readonly ThumbnailOptions _options;
        private readonly ILogger<ThumbnailGenerator> _logger;

        public ThumbnailGenerator(
            IOptions<ThumbnailOptions> options,
            ILogger<ThumbnailGenerator> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task<ThumbnailGenerationResult> GenerateThumbnailAsync(
            Stream originalImageStream,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(originalImageStream);

            var maxDimension = Math.Max(32, _options.MaxDimension);

            using var image = await Image.LoadAsync(originalImageStream, cancellationToken);
            var decodedFormat = image.Metadata.DecodedImageFormat;
            if (decodedFormat == null || !SupportedInputFormats.Contains(decodedFormat.Name))
            {
                var detected = decodedFormat?.Name ?? "unknown";
                throw new NotSupportedException($"Unsupported image input format '{detected}'.");
            }

            image.Mutate(ctx => ctx.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(maxDimension, maxDimension)
            }));

            var (encoder, contentType) = ResolveOutputEncoder(_options.OutputFormat, _options.JpegQuality);
            await using var output = new MemoryStream();
            await image.SaveAsync(output, encoder, cancellationToken);

            return new ThumbnailGenerationResult(
                output.ToArray(),
                contentType,
                image.Width,
                image.Height);
        }

        private (IImageEncoder Encoder, string ContentType) ResolveOutputEncoder(string outputFormat, int jpegQuality)
        {
            var normalized = (outputFormat ?? string.Empty).Trim().ToLowerInvariant();
            var clampedQuality = Math.Clamp(jpegQuality, 1, 100);

            return normalized switch
            {
                "jpg" or "jpeg" => (new JpegEncoder { Quality = clampedQuality }, "image/jpeg"),
                "png" => (new PngEncoder { CompressionLevel = PngCompressionLevel.BestCompression }, "image/png"),
                "webp" => (new WebpEncoder { Quality = clampedQuality }, "image/webp"),
                "bmp" => (new BmpEncoder(), "image/bmp"),
                "gif" => (new GifEncoder(), "image/gif"),
                "tif" or "tiff" => (new TiffEncoder(), "image/tiff"),
                _ => FallbackToJpeg(normalized, clampedQuality)
            };
        }

        private (IImageEncoder Encoder, string ContentType) FallbackToJpeg(string requestedFormat, int quality)
        {
            _logger.LogWarning(
                "Unsupported thumbnail output format '{OutputFormat}'. Falling back to jpeg.",
                requestedFormat);

            return (new JpegEncoder { Quality = quality }, "image/jpeg");
        }
    }
}
