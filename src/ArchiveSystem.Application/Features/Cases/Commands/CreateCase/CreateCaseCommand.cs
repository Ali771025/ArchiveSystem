using ArchiveSystem.Application.Features.Cases.Models;
using MediatR;

namespace ArchiveSystem.Application.Features.Cases.Commands.CreateCase
{
    public sealed class CreateCaseCommand : IRequest<CaseDetailsDto>
    {
        public long ClassificationId { get; set; }
        public string? ExternalCaseNumber { get; set; }
        public string Address { get; set; } = string.Empty;
        public string? PostalCode { get; set; }
        public List<CreateCaseOwnerDto> Owners { get; set; } = new();
    }

    public sealed class CreateCaseOwnerDto
    {
        public long Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string NationalCode { get; set; } = string.Empty;
        public string? TaxpayerId { get; set; }
        public string Phone { get; set; } = string.Empty;
    }
}
