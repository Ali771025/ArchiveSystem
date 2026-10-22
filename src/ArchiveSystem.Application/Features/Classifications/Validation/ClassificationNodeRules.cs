using ArchiveSystem.Domain.Repositories;
using FluentValidation;

namespace ArchiveSystem.Application.Features.Classifications.Validation
{
    public sealed record AssignDossierToClassificationRule(long ClassificationId);

    public sealed class AssignDossierToClassificationRuleValidator : AbstractValidator<AssignDossierToClassificationRule>
    {
        public AssignDossierToClassificationRuleValidator(IClassificationRepository classificationRepository)
        {
            RuleFor(x => x.ClassificationId)
                .GreaterThan(0)
                .WithMessage("ClassificationId must be greater than 0.");

            RuleFor(x => x.ClassificationId)
                .MustAsync(async (classificationId, cancellationToken) =>
                    !await classificationRepository.HasChildrenAsync(classificationId, cancellationToken))
                .WithMessage("Dossiers can only be attached to end-level categories.");
        }
    }

    public sealed record DeleteClassificationNodeRule(long ClassificationId);

    public sealed class DeleteClassificationNodeRuleValidator : AbstractValidator<DeleteClassificationNodeRule>
    {
        public DeleteClassificationNodeRuleValidator(IClassificationRepository classificationRepository)
        {
            RuleFor(x => x.ClassificationId)
                .GreaterThan(0)
                .WithMessage("ClassificationId must be greater than 0.");

            RuleFor(x => x.ClassificationId)
                .MustAsync(async (classificationId, cancellationToken) =>
                    !await classificationRepository.HasChildrenAsync(classificationId, cancellationToken))
                .WithMessage("Cannot delete: This node still has child classifications.");

            RuleFor(x => x.ClassificationId)
                .MustAsync(async (classificationId, cancellationToken) =>
                    !await classificationRepository.HasActiveCasesAsync(classificationId, cancellationToken))
                .WithMessage("Cannot delete: This node contains cases.");

            RuleFor(x => x.ClassificationId)
                .MustAsync(async (classificationId, cancellationToken) =>
                    !await classificationRepository.HasActiveDossiersAsync(classificationId, cancellationToken))
                .WithMessage("Cannot delete: This node contains dossiers.");
        }
    }
}
