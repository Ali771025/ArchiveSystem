using System.Text.RegularExpressions;

namespace ArchiveSystem.Application.Features.Cases.Commands
{
    internal static partial class CaseNumberValidation
    {
        public const string InvalidMessage =
            "شناسه مرجع باید با قالب مجاز ثبت شود؛ نمونه معتبر: EXT-1000 یا A-1404/001.";

        public static bool IsValid(string? value)
        {
            return !string.IsNullOrWhiteSpace(value) && CaseNumberPattern().IsMatch(value.Trim());
        }

        [GeneratedRegex(@"^(?:[\p{L}][\p{L}\p{N}]*(?:[-_/\.][\p{L}\p{N}]+){0,2}|[\p{N}]+(?:[-_\.][\p{L}\p{N}]+)?)$", RegexOptions.CultureInvariant)]
        private static partial Regex CaseNumberPattern();
    }
}
