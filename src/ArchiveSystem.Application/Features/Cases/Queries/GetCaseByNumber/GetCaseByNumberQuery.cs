using ArchiveSystem.Application.Features.Cases.Models;
using MediatR;

namespace ArchiveSystem.Application.Features.Cases.Queries.GetCaseByNumber
{
    public sealed class GetCaseByNumberQuery : IRequest<CaseDetailsDto>
    {
        public string CaseNumber { get; init; } = string.Empty;
    }
}
