using ArchiveSystem.Domain.Enums;
using FluentValidation;

namespace ArchiveSystem.Application.Features.AccessControl.Commands.UpdateTagAcl
{
    public sealed class UpdateTagAclCommandValidator : AbstractValidator<UpdateTagAclCommand>
    {
        public UpdateTagAclCommandValidator()
        {
            RuleFor(x => x.TagId)
                .GreaterThan(0)
                .WithMessage("TagId must be greater than 0.");

            RuleFor(x => x.Entries)
                .NotNull()
                .WithMessage("Entries is required.");

            RuleForEach(x => x.Entries)
                .ChildRules(entry =>
                {
                    entry.RuleFor(x => x.SubjectId)
                        .GreaterThan(0)
                        .WithMessage("SubjectId must be greater than 0.");

                    entry.RuleFor(x => x.SubjectType)
                        .IsInEnum()
                        .WithMessage("SubjectType must be a valid enum value.");

                    entry.RuleFor(x => x.PermissionMask)
                        .Must(mask => mask == TagPermissionMask.None
                            || mask.HasFlag(TagPermissionMask.View)
                            || mask.HasFlag(TagPermissionMask.Assign))
                        .WithMessage("PermissionMask must be a valid tag permission mask.");
                });

            RuleFor(x => x.Entries)
                .Must(entries => entries
                    .Select(e => (e.SubjectType, e.SubjectId))
                    .Distinct()
                    .Count() == entries.Count)
                .When(x => x.Entries is not null)
                .WithMessage("Entries cannot contain duplicate subject assignments.");
        }
    }
}
