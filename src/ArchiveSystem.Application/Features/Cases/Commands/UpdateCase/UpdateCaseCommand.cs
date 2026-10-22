using ArchiveSystem.Application.Features.Cases.Models;
using MediatR;

namespace ArchiveSystem.Application.Features.Cases.Commands.UpdateCase
{
    public sealed class UpdateCaseCommand : IRequest<CaseDetailsDto>
    {
        public long CaseId { get; init; }
        public long? ClassificationId { get; init; }
        public string? ExternalCaseNumber { get; init; }
        public string Address { get; init; } = string.Empty;
        public string? PostalCode { get; init; }
        public List<UpdateCaseOwnerDto> Owners { get; init; } = new();
    }

    public sealed class UpdateCaseOwnerDto
    {
        public long Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string NationalCode { get; set; } = string.Empty;
        public string? TaxpayerId { get; set; }
        public string Phone { get; set; } = string.Empty;
    }
}
