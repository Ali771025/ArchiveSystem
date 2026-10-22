using System.Text;
using System.Text.RegularExpressions;

namespace ArchiveSystem.Domain.Common
{
    public static partial class PersianTextNormalizer
    {
        public static string CleanTagName(string value)
        {
            var normalized = value.Normalize(NormalizationForm.FormKC)
                .Replace('ي', 'ی')
                .Replace('ى', 'ی')
                .Replace('ك', 'ک');

            return ExtraWhitespace().Replace(normalized, " ").Trim();
        }

        public static string NormalizeTagName(string value) =>
            CleanTagName(value).ToLowerInvariant();

        public static string NormalizeForSearch(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var normalized = value.Normalize(NormalizationForm.FormKC)
                .Replace('ي', 'ی')
                .Replace('ى', 'ی')
                .Replace('ك', 'ک')
                .Replace('\u0640', ' ')
                .Replace('\u200C', ' ')
                .Replace('\u200D', ' ')
                .Replace('\u200E', ' ')
                .Replace('\u200F', ' ');

            return ExtraWhitespace().Replace(normalized, " ").Trim().ToLowerInvariant();
        }

        [GeneratedRegex(@"\s+")]
        private static partial Regex ExtraWhitespace();
    }
}
