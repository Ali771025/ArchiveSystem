using ArchiveSystem.Application.Features.AccessControl.DTOs;
using MediatR;

namespace ArchiveSystem.Application.Features.AccessControl.Commands.UpdateClassificationAcl
{
    public sealed record UpdateClassificationAclCommand(long ClassificationId, IReadOnlyCollection<ClassificationAclEntryDto> Entries) : IRequest<Unit>;
}
