using System.Text;
using System.Text.RegularExpressions;

namespace ArchiveSystem.Worker.Ocr.Services
{
    public static class PersianOcrText
    {
        private static readonly Regex Whitespace = new(@"[\t\u00A0 ]+", RegexOptions.Compiled);

        public static string Normalize(string? value, bool preserveLines = true)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(value.Length);
            foreach (var character in value.Normalize(NormalizationForm.FormKC))
            {
                builder.Append(character switch
                {
                    'ي' or 'ى' => 'ی',
                    'ك' => 'ک',
                    'ة' => 'ه',
                    'ۀ' => 'ه',
                    'ؤ' => 'و',
                    'أ' or 'إ' => 'ا',
                    '٠' or '۰' => '0',
                    '١' or '۱' => '1',
                    '٢' or '۲' => '2',
                    '٣' or '۳' => '3',
                    '٤' or '۴' => '4',
                    '٥' or '۵' => '5',
                    '٦' or '۶' => '6',
                    '٧' or '۷' => '7',
                    '٨' or '۸' => '8',
                    '٩' or '۹' => '9',
                    '\u200c' => ' ',
                    '\u200e' or '\u200f' or '\u202a' or '\u202b' or '\u202c' or '\u202d' or '\u202e' => '\0',
                    _ => character
                });
            }

            var normalized = builder.Replace("\0", string.Empty).ToString().Replace("\r\n", "\n");
            normalized = Whitespace.Replace(normalized, " ");
            if (!preserveLines)
            {
                normalized = normalized.Replace('\n', ' ');
                normalized = Whitespace.Replace(normalized, " ");
            }

            return string.Join('\n', normalized
                .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
        }

        public static string DigitsOnly(string? value) =>
            new(Normalize(value, false).Where(char.IsAsciiDigit).ToArray());
    }
}
