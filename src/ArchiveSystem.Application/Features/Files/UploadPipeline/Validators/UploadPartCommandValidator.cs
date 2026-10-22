using ArchiveSystem.Application.Options;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace ArchiveSystem.Application.Features.Files.UploadPipeline.Commands
{
    public class UploadPartCommandValidator : AbstractValidator<UploadPartCommand>
    {
        public UploadPartCommandValidator(IOptions<FileUploadOptions> options)
        {
            var uploadOptions = options.Value;

            RuleFor(x => x.SessionId)
                .NotEqual(Guid.Empty);

            RuleFor(x => x.PartNumber)
                .GreaterThan(0);

            RuleFor(x => x.ContentLength)
                .GreaterThan(0)
                .LessThanOrEqualTo(uploadOptions.MaxPartSizeBytes);

            RuleFor(x => x.Content)
                .NotNull();
        }
    }
}
