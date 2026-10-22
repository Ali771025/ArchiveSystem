using ArchiveSystem.Application.Features.Owners.Models;
using FluentValidation;

namespace ArchiveSystem.Application.Features.Owners.Commands.UpdateOwner;

public sealed class UpdateOwnerCommandValidator : AbstractValidator<UpdateOwnerCommand>
{
    public UpdateOwnerCommandValidator(IValidator<CreateOwnerDto> dtoValidator)
    {
        RuleFor(x => x.OwnerId).GreaterThan(0);
        RuleFor(x => x.Request)
            .NotNull()
            .SetValidator(dtoValidator);
    }
}
