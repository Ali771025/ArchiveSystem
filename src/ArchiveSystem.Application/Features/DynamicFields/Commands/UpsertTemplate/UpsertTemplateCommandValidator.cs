using FluentValidation;

namespace ArchiveSystem.Application.Features.DynamicFields.Commands.UpsertTemplate
{
    public sealed class UpsertTemplateCommandValidator : AbstractValidator<UpsertTemplateCommand>
    {
        public UpsertTemplateCommandValidator()
        {
            RuleFor(x => x.ClassificationId)
                .GreaterThan(0);

            RuleFor(x => x.Template)
                .NotNull();

            RuleFor(x => x.Template.Name)
                .NotEmpty()
                .MaximumLength(200);
        }
    }
}
