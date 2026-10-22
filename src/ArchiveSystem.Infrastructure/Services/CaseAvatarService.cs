using ArchiveSystem.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

namespace ArchiveSystem.Infrastructure.Services
{
    public sealed class CaseAvatarService : ICaseAvatarService
    {
        private const int StandardSize = 500;
        private const int ThumbnailSize = 50;

        private readonly IWebHostEnvironment _webHostEnvironment;

        public CaseAvatarService(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<string> UploadAvatarAsync(long caseId, IFormFile file, CancellationToken cancellationToken = default)
        {
            var webRoot = _webHostEnvironment.WebRootPath;
            if (string.IsNullOrWhiteSpace(webRoot))
            {
                webRoot = Path.Combine(_webHostEnvironment.ContentRootPath, "wwwroot");
            }

            var relativeFolder = Path.Combine("uploads", "case-avatars", caseId.ToString());
            var absoluteFolder = Path.Combine(webRoot, relativeFolder);
            Directory.CreateDirectory(absoluteFolder);

            await using var input = file.OpenReadStream();
            using var image = await Image.LoadAsync(input, cancellationToken);
            image.Mutate(x => x.AutoOrient());

            var targetExtension = ResolveTargetExtension(file.FileName);
            var fileToken = Guid.NewGuid().ToString("N");
            var standardFileName = $"{fileToken}{targetExtension}";
            var thumbnailFileName = $"{fileToken}_thumb{targetExtension}";
            var standardPath = Path.Combine(absoluteFolder, standardFileName);
            var thumbnailPath = Path.Combine(absoluteFolder, thumbnailFileName);

            await SaveVariantAsync(image, StandardSize, standardPath, cancellationToken, quality: 85);
            await SaveVariantAsync(image, ThumbnailSize, thumbnailPath, cancellationToken, quality: 55);

            var relativePath = "/" + Path.Combine(relativeFolder, standardFileName).Replace("\\", "/");
            return relativePath;
        }

        private static async Task SaveVariantAsync(
            Image sourceImage,
            int targetSize,
            string outputPath,
            CancellationToken cancellationToken,
            int quality)
        {
            using var clone = sourceImage.Clone(ctx =>
                ctx.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Crop,
                    Position = AnchorPositionMode.Center,
                    Size = new Size(targetSize, targetSize)
                }));

            var extension = Path.GetExtension(outputPath).ToLowerInvariant();
            if (extension == ".png")
            {
                await clone.SaveAsync(outputPath, new PngEncoder(), cancellationToken);
                return;
            }

            await clone.SaveAsync(outputPath, new JpegEncoder { Quality = quality }, cancellationToken);
        }

        private static string ResolveTargetExtension(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension == ".png" ? ".png" : ".jpg";
        }
    }
}
