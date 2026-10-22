using ArchiveSystem.Application.Options;
using ArchiveSystem.Domain.Repositories;
using FluentValidation;
using Microsoft.Extensions.Options;
using System.IO;

namespace ArchiveSystem.Application.Features.Files.UploadPipeline.Commands
{
    public class InitiateUploadSessionCommandValidator : AbstractValidator<InitiateUploadSessionCommand>
    {
        public InitiateUploadSessionCommandValidator(
            IOptions<FileUploadOptions> options,
            IClassificationRepository classificationRepository)
        {
            var uploadOptions = options.Value;

            RuleFor(x => x.OriginalFileName)
                .NotEmpty()
                .MaximumLength(255)
                .Must(HaveSafeFileName)
                .WithMessage("File name is not valid.")
                .Must(fileName => !HasBlockedExtension(fileName, uploadOptions.BlockedExtensions))
                .WithMessage("File extension is not allowed.");

            RuleFor(x => x.ContentType)
                .NotEmpty()
                .MaximumLength(200);

            RuleFor(x => x.ExpectedSizeBytes)
                .GreaterThan(0)
                .LessThanOrEqualTo(uploadOptions.MaxFileSizeBytes);

            RuleFor(x => x.ExpectedSha256)
                .NotEmpty()
                .Matches("^[a-fA-F0-9]{64}$")
                .WithMessage("ExpectedSha256 must be a valid SHA-256 hex string.");

            RuleFor(x => x.ContentType)
                .Must(contentType =>
                {
                    if (uploadOptions.AllowedMimeTypes == null || uploadOptions.AllowedMimeTypes.Length == 0)
                    {
                        return true;
                    }

                    return uploadOptions.AllowedMimeTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase);
                })
                .WithMessage("Content type is not allowed.");

            When(x => x.ClassificationId.HasValue, () =>
            {
                RuleFor(x => x.ClassificationId!.Value)
                    .MustAsync(async (classificationId, cancellationToken) =>
                        !await classificationRepository.HasChildrenAsync(classificationId, cancellationToken))
                    .WithMessage("Documents can only be attached to final sub-categories.");
            });
        }

        private static bool HaveSafeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return false;
            }

            var trimmed = fileName.Trim();
            var leafName = Path.GetFileName(trimmed);
            return string.Equals(trimmed, leafName, StringComparison.Ordinal)
                   && trimmed.IndexOfAny(Path.GetInvalidFileNameChars()) < 0
                   && !trimmed.Contains("..", StringComparison.Ordinal);
        }

        private static bool HasBlockedExtension(string fileName, IReadOnlyCollection<string>? blockedExtensions)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return false;
            }

            var normalizedBlocked = (blockedExtensions ?? Array.Empty<string>())
                .Where(extension => !string.IsNullOrWhiteSpace(extension))
                .Select(extension => extension.StartsWith('.') ? extension : $".{extension}")
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (normalizedBlocked.Count == 0)
            {
                return false;
            }

            var leafName = Path.GetFileName(fileName.Trim());
            while (!string.IsNullOrWhiteSpace(leafName))
            {
                var extension = Path.GetExtension(leafName);
                if (string.IsNullOrWhiteSpace(extension))
                {
                    return false;
                }

                if (normalizedBlocked.Contains(extension))
                {
                    return true;
                }

                leafName = Path.GetFileNameWithoutExtension(leafName);
            }

            return false;
        }
    }
}
