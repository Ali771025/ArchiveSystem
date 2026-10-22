using ArchiveSystem.Application.Common.Exceptions;
using ArchiveSystem.Application.Features.Classifications.Security;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Domain.Repositories;
using FluentValidation.Results;
using MediatR;

namespace ArchiveSystem.Application.Features.Classifications.Commands.MoveClassification
{
    public sealed class MoveClassificationCommandHandler : IRequestHandler<MoveClassificationCommand, Unit>
    {
        private readonly IClassificationRepository _classificationRepository;
        private readonly IClassificationAccessAuthorizer _authorizer;

        public MoveClassificationCommandHandler(
            IClassificationRepository classificationRepository,
            IClassificationAccessAuthorizer authorizer)
        {
            _classificationRepository = classificationRepository;
            _authorizer = authorizer;
        }

        public async Task<Unit> Handle(MoveClassificationCommand request, CancellationToken cancellationToken)
        {
            await _authorizer.EnsureCanManageAsync(request.Id, cancellationToken);

            if (request.NewParentId == request.Id)
            {
                throw new ValidationException(new[]
                {
                    new ValidationFailure(nameof(request.NewParentId), "A classification cannot be moved under itself.")
                });
            }

            if (request.NewParentId.HasValue)
            {
                await _authorizer.EnsureCanManageAsync(request.NewParentId.Value, cancellationToken);
            }

            var classification = await _classificationRepository.GetByIdAsync(request.Id, cancellationToken);
            if (classification == null)
            {
                throw new NotFoundException($"Classification '{request.Id}' not found.");
            }

            if (classification.IsSystem)
            {
                throw new ValidationException(new[]
                {
                    new ValidationFailure(nameof(request.Id), "دسته‌بندی سیستمی قابل جابه‌جایی نیست.")
                });
            }

            var oldPath = classification.Path;
            var oldLevel = classification.Level;

            Classification? parent = null;
            if (request.NewParentId.HasValue)
            {
                parent = await _classificationRepository.GetByIdAsync(request.NewParentId.Value, cancellationToken);
                if (parent == null || !parent.IsActive)
                {
                    throw new NotFoundException($"Parent classification '{request.NewParentId}' not found.");
                }
            }

            if (await _classificationRepository.ExistsActiveSiblingWithTitleAsync(
                    request.NewParentId,
                    classification.Title,
                    request.Id,
                    cancellationToken))
            {
                throw new ConflictException("A category with the same title already exists under the target parent.");
            }

            try
            {
                if (parent != null)
                {
                    classification.SetParent(parent);
                }
                else
                {
                    classification.RemoveFromParent();
                }
            }
            catch (ArgumentException exception)
            {
                throw new ValidationException(new[]
                {
                    new ValidationFailure(nameof(request.NewParentId), exception.Message)
                });
            }

            var newPath = classification.Path;
            var levelDelta = classification.Level - oldLevel;
            await _classificationRepository.UpdateAsync(classification, cancellationToken);

            var descendants = await _classificationRepository.GetByPathPrefixAsync(oldPath, cancellationToken);
            foreach (var descendant in descendants.Where(x => x.Id != request.Id))
            {
                if (!descendant.Path.StartsWith(oldPath, StringComparison.Ordinal))
                {
                    continue;
                }

                descendant.RebaseHierarchy($"{newPath}{descendant.Path[oldPath.Length..]}", descendant.Level + levelDelta);
                await _classificationRepository.UpdateAsync(descendant, cancellationToken);
            }

            await _classificationRepository.SaveChangesAsync(cancellationToken);
            _authorizer.InvalidateAllCaches();
            return Unit.Value;
        }
    }
}
