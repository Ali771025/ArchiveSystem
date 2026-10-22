using ArchiveSystem.Application.Common.Models.MovementLogs;
using FluentValidation;

namespace ArchiveSystem.Application.Features.MovementLogs.Commands.CreatePhysicalMovementLog;

public sealed class CreatePhysicalMovementLogCommandValidator
    : AbstractValidator<CreatePhysicalMovementLogCommand>
{
    public CreatePhysicalMovementLogCommandValidator(IValidator<CreateMovementLogDto> dtoValidator)
    {
        RuleFor(x => x.Dto)
            .NotNull()
            .SetValidator(dtoValidator);
    }
}
