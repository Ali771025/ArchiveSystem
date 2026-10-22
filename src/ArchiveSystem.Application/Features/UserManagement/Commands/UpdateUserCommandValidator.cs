using FluentValidation;

namespace ArchiveSystem.Application.Features.UserManagement.Commands
{
    public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
    {
        public UpdateUserCommandValidator()
        {
            RuleFor(x => x.UserId)
                .GreaterThan(0);

            RuleFor(x => x.Username)
                .NotEmpty()
                .MinimumLength(3)
                .MaximumLength(100);

            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(256);

            RuleFor(x => x.PhoneNumber)
                .Matches("^09[0-9]{9}$")
                .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber))
                .WithMessage("شماره همراه باید با 09 شروع شود و دقیقاً 11 رقم باشد.");

            RuleFor(x => x.PhoneNumber)
                .NotEmpty()
                .When(x => x.TwoFactorEnabled)
                .WithMessage("برای فعال‌سازی ورود پیامکی، شماره همراه الزامی است.");

            RuleFor(x => x.FirstName)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(x => x.LastName)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(x => x.NationalId)
                .Must(IsValidNationalId)
                .When(x => !string.IsNullOrWhiteSpace(x.NationalId))
                .WithMessage("NationalId is invalid.");

            RuleFor(x => x.PersonnelCode)
                .MaximumLength(50)
                .When(x => !string.IsNullOrWhiteSpace(x.PersonnelCode));

            RuleFor(x => x.ActiveDuration)
                .GreaterThanOrEqualTo(TimeSpan.Zero)
                .LessThanOrEqualTo(TimeSpan.FromHours(24));
        }

        private static bool IsValidNationalId(string? nationalId)
        {
            if (string.IsNullOrWhiteSpace(nationalId))
            {
                return false;
            }

            var code = new string(nationalId.Trim().Where(char.IsDigit).ToArray());
            if (code.Length != 10)
            {
                return false;
            }

            if (code.Distinct().Count() == 1)
            {
                return false;
            }

            var check = code[9] - '0';
            var sum = 0;
            for (var i = 0; i < 9; i++)
            {
                sum += (code[i] - '0') * (10 - i);
            }

            var remainder = sum % 11;
            return (remainder < 2 && check == remainder) || (remainder >= 2 && check == 11 - remainder);
        }
    }
}
