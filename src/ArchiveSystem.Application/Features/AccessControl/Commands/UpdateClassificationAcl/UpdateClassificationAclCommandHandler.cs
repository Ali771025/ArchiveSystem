using ArchiveSystem.Application.Common.Interfaces.Repositories;
using ArchiveSystem.Application.Common.Interfaces.Security;
using ArchiveSystem.Domain.Entities;
using MediatR;

namespace ArchiveSystem.Application.Features.AccessControl.Commands.UpdateClassificationAcl
{
    public sealed class UpdateClassificationAclCommandHandler : IRequestHandler<UpdateClassificationAclCommand, Unit>
    {
        private readonly IClassificationAclRepository _classificationAclRepository;
        private readonly IAccessControlService _accessControlService;

        public UpdateClassificationAclCommandHandler(
            IClassificationAclRepository classificationAclRepository,
            IAccessControlService accessControlService)
        {
            _classificationAclRepository = classificationAclRepository;
            _accessControlService = accessControlService;
        }

        public async Task<Unit> Handle(UpdateClassificationAclCommand request, CancellationToken cancellationToken)
        {
            var mapped = request.Entries
                .Select(x => new ClassificationAclEntry(
                    x.Id,
                    x.SubjectType,
                    x.SubjectId,
                    request.ClassificationId,
                    x.PermissionMask,
                    x.IsExplicit,
                    x.InheritMode))
                .ToArray();

            await _classificationAclRepository.ReplaceForClassificationAsync(
                request.ClassificationId,
                mapped,
                cancellationToken);

            var cascadeSubjectKeys = request.Entries
                .Where(entry => entry.Cascade)
                .Select(entry => (entry.SubjectType, entry.SubjectId))
                .ToHashSet();
            var cascadingEntries = mapped
                .Where(entry => cascadeSubjectKeys.Contains((entry.SubjectType, entry.SubjectId)))
                .ToArray();
            if (cascadingEntries.Length > 0)
            {
                await _classificationAclRepository.OverrideForDescendantsAsync(
                    request.ClassificationId,
                    cascadingEntries,
                    cancellationToken);
            }

            _accessControlService.InvalidateAllCaches();

            return Unit.Value;
        }
    }
}
