using FluentValidation;

namespace ArchiveSystem.Application.Features.DynamicFields.Commands.UpsertCaseDynamicFields
{
    public sealed class UpsertCaseDynamicFieldsCommandValidator : AbstractValidator<UpsertCaseDynamicFieldsCommand>
    {
        public UpsertCaseDynamicFieldsCommandValidator()
        {
            RuleFor(x => x.CaseId)
                .GreaterThan(0);

            RuleFor(x => x.Values)
                .NotNull();
        }
    }
}
