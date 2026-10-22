using ArchiveSystem.Application.Features.Cases.Models;
using MediatR;

namespace ArchiveSystem.Application.Features.Cases.Queries.GetCaseById
{
    public sealed class GetCaseByIdQuery : IRequest<CaseDetailsDto>
    {
        public long CaseId { get; init; }
    }
}
