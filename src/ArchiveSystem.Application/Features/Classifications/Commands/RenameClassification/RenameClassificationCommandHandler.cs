using ArchiveSystem.Application.Common.Exceptions;
using ArchiveSystem.Application.Authorization;
using ArchiveSystem.Application.Features.Classifications.Models;
using ArchiveSystem.Application.Features.Classifications.Security;
using ArchiveSystem.Domain.Repositories;
using FluentValidation.Results;
using MediatR;

namespace ArchiveSystem.Application.Features.Classifications.Commands.RenameClassification
{
    public sealed class RenameClassificationCommandHandler
        : IRequestHandler<RenameClassificationCommand, ClassificationMutationResultDto>
    {
        private readonly IClassificationRepository _classificationRepository;
        private readonly IClassificationAccessAuthorizer _authorizer;

        public RenameClassificationCommandHandler(
            IClassificationRepository classificationRepository,
            IClassificationAccessAuthorizer authorizer)
        {
            _classificationRepository = classificationRepository;
            _authorizer = authorizer;
        }

        public async Task<ClassificationMutationResultDto> Handle(
            RenameClassificationCommand request,
            CancellationToken cancellationToken)
        {
            await _authorizer.EnsureHasPermissionAsync(Permissions.Categories.Edit, cancellationToken);
            await _authorizer.EnsureCanManageAsync(request.Id, cancellationToken);

            var classification = await _classificationRepository.GetByIdAsync(request.Id, cancellationToken);
            if (classification == null)
            {
                throw new NotFoundException($"Classification '{request.Id}' not found.");
            }

            if (classification.IsSystem)
            {
                throw new ValidationException(new[]
                {
                    new ValidationFailure(nameof(request.Id), "دسته‌بندی سیستمی قابل تغییر نام نیست.")
                });
            }

            var title = request.Title?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ValidationException("Category title is required.");
            }

            if (await _classificationRepository.ExistsActiveSiblingWithTitleAsync(
                    classification.ParentId,
                    title,
                    request.Id,
                    cancellationToken))
            {
                throw new ConflictException("A category with the same title already exists at this level.");
            }

            classification.UpdateTitle(title);
            await _classificationRepository.UpdateAsync(classification, cancellationToken);
            await _classificationRepository.SaveChangesAsync(cancellationToken);
            _authorizer.InvalidateAllCaches();

            return new ClassificationMutationResultDto
            {
                Id = classification.Id,
                Title = classification.Title,
                ParentId = classification.ParentId
            };
        }
    }
}
