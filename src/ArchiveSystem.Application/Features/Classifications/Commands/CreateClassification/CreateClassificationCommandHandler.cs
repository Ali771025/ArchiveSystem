using ArchiveSystem.Application.Common.Exceptions;
using ArchiveSystem.Application.Authorization;
using ArchiveSystem.Application.Common.Interfaces.Repositories;
using ArchiveSystem.Application.Features.Classifications.Models;
using ArchiveSystem.Application.Features.Classifications.Security;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Domain.Enums;
using ArchiveSystem.Domain.Repositories;
using MediatR;

namespace ArchiveSystem.Application.Features.Classifications.Commands.CreateClassification
{
    public sealed class CreateClassificationCommandHandler
        : IRequestHandler<CreateClassificationCommand, ClassificationMutationResultDto>
    {
        private readonly IClassificationRepository _classificationRepository;
        private readonly IClassificationAclRepository _classificationAclRepository;
        private readonly IClassificationAccessAuthorizer _authorizer;

        public CreateClassificationCommandHandler(
            IClassificationRepository classificationRepository,
            IClassificationAclRepository classificationAclRepository,
            IClassificationAccessAuthorizer authorizer)
        {
            _classificationRepository = classificationRepository;
            _classificationAclRepository = classificationAclRepository;
            _authorizer = authorizer;
        }

        public async Task<ClassificationMutationResultDto> Handle(
            CreateClassificationCommand request,
            CancellationToken cancellationToken)
        {
            await _authorizer.EnsureHasPermissionAsync(Permissions.Categories.Create, cancellationToken);

            var title = request.Title?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ValidationException("Category title is required.");
            }

            if (request.ParentId.HasValue)
            {
                await _authorizer.EnsureCanWriteAsync(request.ParentId.Value, cancellationToken);
            }

            if (await _classificationRepository.ExistsActiveSiblingWithTitleAsync(
                    request.ParentId,
                    title,
                    null,
                    cancellationToken))
            {
                throw new ConflictException("A category with the same title already exists at this level.");
            }

            var entity = new Classification(0, title, null, request.Description, request.ColorCode, request.Icon);
            await _classificationRepository.AddAsync(entity, cancellationToken);
            await _classificationRepository.SaveChangesAsync(cancellationToken);

            if (request.ParentId.HasValue)
            {
                var parent = await _classificationRepository.GetByIdAsync(request.ParentId.Value, cancellationToken);
                if (parent == null || !parent.IsActive)
                {
                    throw new NotFoundException($"Parent classification '{request.ParentId}' not found.");
                }

                entity.SetParent(parent);
            }
            else
            {
                entity.RemoveFromParent();
            }

            await _classificationRepository.UpdateAsync(entity, cancellationToken);
            await _classificationRepository.SaveChangesAsync(cancellationToken);

            await _classificationAclRepository.GrantUserAccessAsync(
                _authorizer.CurrentUserId,
                entity.Id,
                ClassificationPermissionMask.Read |
                ClassificationPermissionMask.Write |
                ClassificationPermissionMask.Move,
                cancellationToken);

            _authorizer.InvalidateAllCaches();

            return new ClassificationMutationResultDto
            {
                Id = entity.Id,
                Title = entity.Title,
                ParentId = entity.ParentId
            };
        }
    }
}
