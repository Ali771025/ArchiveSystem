using ArchiveSystem.Application.Features.AccessControl.DTOs;
using MediatR;

namespace ArchiveSystem.Application.Features.AccessControl.Queries.GetTagAcl
{
    public sealed record GetTagAclQuery(long TagId) : IRequest<IReadOnlyList<TagAclEntryDto>>;
}
