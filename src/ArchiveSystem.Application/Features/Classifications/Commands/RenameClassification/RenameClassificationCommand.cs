using ArchiveSystem.Application.Features.Classifications.Models;
using MediatR;

namespace ArchiveSystem.Application.Features.Classifications.Commands.RenameClassification
{
    public sealed record RenameClassificationCommand(long Id, string Title) : IRequest<ClassificationMutationResultDto>;
}
