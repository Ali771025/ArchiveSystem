using ArchiveSystem.Application.Common.Exceptions;
using ArchiveSystem.Application.Features.Classifications.Security;
using ArchiveSystem.Application.Features.Classifications.Validation;
using ArchiveSystem.Domain.Repositories;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using AppValidationException = ArchiveSystem.Application.Common.Exceptions.ValidationException;

namespace ArchiveSystem.Application.Features.Classifications.Commands.DeleteClassification
{
    public sealed class DeleteClassificationCommandHandler : IRequestHandler<DeleteClassificationCommand, Unit>
    {
        private readonly IClassificationRepository _classificationRepository;
        private readonly IClassificationAccessAuthorizer _authorizer;
        private readonly IValidator<DeleteClassificationNodeRule> _validator;

        public DeleteClassificationCommandHandler(
            IClassificationRepository classificationRepository,
            IClassificationAccessAuthorizer authorizer,
            IValidator<DeleteClassificationNodeRule> validator)
        {
            _classificationRepository = classificationRepository;
            _authorizer = authorizer;
            _validator = validator;
        }

        public async Task<Unit> Handle(DeleteClassificationCommand request, CancellationToken cancellationToken)
        {
            await _authorizer.EnsureCanManageAsync(request.Id, cancellationToken);

            var classification = await _classificationRepository.GetByIdAsync(request.Id, cancellationToken);
            if (classification == null)
            {
                throw new NotFoundException($"Classification '{request.Id}' not found.");
            }

            if (classification.IsSystem)
            {
                throw new AppValidationException(new[]
                {
                    new ValidationFailure(nameof(request.Id), "دسته‌بندی سیستمی قابل حذف نیست.")
                });
            }

            if (await _classificationRepository.HasChildrenAsync(request.Id, cancellationToken))
            {
                throw new AppValidationException(new[]
                {
                    new ValidationFailure(nameof(request.Id), "Cannot delete: This node still has child classifications.")
                });
            }

            var validation = await _validator.ValidateAsync(
                new DeleteClassificationNodeRule(request.Id),
                cancellationToken);
            if (!validation.IsValid)
            {
                throw new AppValidationException(validation.Errors);
            }

            await _classificationRepository.DeleteAsync(classification, cancellationToken);
            await _classificationRepository.SaveChangesAsync(cancellationToken);
            _authorizer.InvalidateAllCaches();
            return Unit.Value;
        }
    }
}
