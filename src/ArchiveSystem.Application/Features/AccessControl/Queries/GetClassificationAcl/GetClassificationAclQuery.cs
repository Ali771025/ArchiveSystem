using ArchiveSystem.Application.Features.AccessControl.DTOs;
using MediatR;

namespace ArchiveSystem.Application.Features.AccessControl.Queries.GetClassificationAcl
{
    public sealed record GetClassificationAclQuery(long ClassificationId) : IRequest<IReadOnlyList<ClassificationAclEntryDto>>;
}
