using ArchiveSystem.Application.Features.AccessControl.DTOs;
using MediatR;

namespace ArchiveSystem.Application.Features.AccessControl.Commands.UpdateTagAcl
{
    public sealed record UpdateTagAclCommand(long TagId, IReadOnlyCollection<TagAclEntryDto> Entries) : IRequest<Unit>;
}
