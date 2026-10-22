using ArchiveSystem.Application.Common.Interfaces.Repositories;
using ArchiveSystem.Application.Common.Interfaces.Security;
using ArchiveSystem.Domain.Entities;
using MediatR;

namespace ArchiveSystem.Application.Features.AccessControl.Commands.UpdateTagAcl
{
    public sealed class UpdateTagAclCommandHandler : IRequestHandler<UpdateTagAclCommand, Unit>
    {
        private readonly ITagAclRepository _tagAclRepository;
        private readonly IAccessControlService _accessControlService;

        public UpdateTagAclCommandHandler(ITagAclRepository tagAclRepository, IAccessControlService accessControlService)
        {
            _tagAclRepository = tagAclRepository;
            _accessControlService = accessControlService;
        }

        public async Task<Unit> Handle(UpdateTagAclCommand request, CancellationToken cancellationToken)
        {
            var mapped = request.Entries
                .Select(x => new TagAclEntry(
                    x.Id,
                    x.SubjectType,
                    x.SubjectId,
                    request.TagId,
                    x.PermissionMask))
                .ToArray();

            await _tagAclRepository.ReplaceForTagAsync(request.TagId, mapped, cancellationToken);
            _accessControlService.InvalidateAllCaches();

            return Unit.Value;
        }
    }
}
