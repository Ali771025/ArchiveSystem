using ArchiveSystem.Application.Features.Owners.Models;
using FluentValidation;

namespace ArchiveSystem.Application.Features.Owners.Commands.CreateOwner;

public sealed class CreateOwnerCommandValidator : AbstractValidator<CreateOwnerCommand>
{
    public CreateOwnerCommandValidator(IValidator<CreateOwnerDto> dtoValidator)
    {
        RuleFor(x => x.Request)
            .NotNull()
            .SetValidator(dtoValidator);
    }
}
