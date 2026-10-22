using ArchiveSystem.Domain.Enums;
using FluentValidation;

namespace ArchiveSystem.Application.Features.AccessControl.Commands.UpdateClassificationAcl
{
    public sealed class UpdateClassificationAclCommandValidator : AbstractValidator<UpdateClassificationAclCommand>
    {
        public UpdateClassificationAclCommandValidator()
        {
            RuleFor(x => x.ClassificationId)
                .GreaterThan(0)
                .WithMessage("ClassificationId must be greater than 0.");

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
                        .Must(mask => mask == ClassificationPermissionMask.None
                            || mask.HasFlag(ClassificationPermissionMask.Read)
                            || mask.HasFlag(ClassificationPermissionMask.Write)
                            || mask.HasFlag(ClassificationPermissionMask.Manage)
                            || mask.HasFlag(ClassificationPermissionMask.Move))
                        .WithMessage("PermissionMask must be a valid classification permission mask.");

                    entry.RuleFor(x => x.InheritMode)
                        .IsInEnum()
                        .WithMessage("InheritMode must be a valid enum value.");
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
