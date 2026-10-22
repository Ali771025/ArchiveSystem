using FluentValidation;

namespace ArchiveSystem.Application.Features.Files.UploadPipeline.Commands
{
    public class FinalizeUploadCommandValidator : AbstractValidator<FinalizeUploadCommand>
    {
        public FinalizeUploadCommandValidator()
        {
            RuleFor(x => x.SessionId)
                .NotEqual(Guid.Empty);
        }
    }
}
