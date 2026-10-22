// src/ArchiveSystem.Application/Features/Files/Commands/UploadFile/UploadFileCommandValidator.cs
using FluentValidation;

namespace ArchiveSystem.Application.Features.Files.Commands.UploadFile
{
    public class UploadFileCommandValidator : AbstractValidator<UploadFileCommand>
    {
        public UploadFileCommandValidator()
        {
            RuleFor(x => x.FileName)
                .NotEmpty()
                .MaximumLength(255)
                .WithMessage("File name cannot be empty and must be less than 255 characters.");

            RuleFor(x => x.Size)
                .GreaterThan(0)
                .WithMessage("File size must be greater than 0.");

            RuleFor(x => x.ContentType)
                .NotEmpty()
                .WithMessage("Content type is required.");

            RuleFor(x => x.TusId)
                .NotEmpty()
                .WithMessage("Tus ID is required for resumable uploads.");

            RuleFor(x => x.OriginalFileName)
                .NotEmpty()
                .MaximumLength(255)
                .WithMessage("Original file name cannot be empty and must be less than 255 characters.");

            RuleFor(x => x.CaseId)
                .GreaterThan(0)
                .When(x => x.CaseId.HasValue)
                .WithMessage("Case ID must be greater than 0.");
        }
    }
}