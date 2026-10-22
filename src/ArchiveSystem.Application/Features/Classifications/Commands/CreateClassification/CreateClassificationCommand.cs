using ArchiveSystem.Application.Features.Classifications.Models;
using MediatR;

namespace ArchiveSystem.Application.Features.Classifications.Commands.CreateClassification
{
    public sealed class CreateClassificationCommand : IRequest<ClassificationMutationResultDto>
    {
        public string Title { get; init; } = string.Empty;
        public long? ParentId { get; init; }
        public string? Description { get; init; }
        public string? ColorCode { get; init; }
        public string? Icon { get; init; }
    }
}
