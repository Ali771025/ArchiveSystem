using System.Text;

namespace ArchiveSystem.Worker.Ocr.Services
{
    public static class PaddleArabicTextDecoder
    {
        public static string RestoreLogicalOrder(string? prediction)
        {
            if (string.IsNullOrEmpty(prediction) || !ContainsArabicLetter(prediction))
            {
                return prediction ?? string.Empty;
            }

            // PaddleOCR's Arabic decoder reverses recognition segments after CTC
            // decoding. RapidOcrNet uses a language-neutral decoder, so reproduce
            // that model-specific step here. Arabic/Persian digits stay grouped to
            // preserve national codes and dates in their recognized order.
            var segments = new List<string>();
            var preservedRun = new StringBuilder();

            foreach (var character in prediction)
            {
                if (IsPreservedCharacter(character))
                {
                    preservedRun.Append(character);
                    continue;
                }

                FlushRun(segments, preservedRun);
                segments.Add(character.ToString());
            }

            FlushRun(segments, preservedRun);
            segments.Reverse();
            return RestoreMissingLetterNumberBoundary(string.Concat(segments)).Trim();
        }

        private static bool ContainsArabicLetter(string value) => value.Any(IsArabicLetter);

        private static bool IsArabicLetter(char character) =>
            character is >= '\u0600' and <= '\u06FF' &&
            character is not (>= '\u0660' and <= '\u0669') &&
            character is not (>= '\u06F0' and <= '\u06F9');

        private static bool IsDigit(char character) =>
            character is >= '0' and <= '9' or
                >= '\u0660' and <= '\u0669' or
                >= '\u06F0' and <= '\u06F9';

        private static string RestoreMissingLetterNumberBoundary(string value)
        {
            var result = new StringBuilder(value.Length + 2);
            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                if (index > 0 &&
                    ((IsArabicLetter(value[index - 1]) && IsDigit(character)) ||
                     (IsDigit(value[index - 1]) && IsArabicLetter(character))))
                {
                    result.Append(' ');
                }

                result.Append(character);
            }

            return result.ToString();
        }

        private static bool IsPreservedCharacter(char character) =>
            character is >= 'a' and <= 'z' or
                >= 'A' and <= 'Z' or
                >= '0' and <= '9' or
                >= '\u0660' and <= '\u0669' or
                >= '\u06F0' and <= '\u06F9' or
                ' ' or ':' or '*' or '.' or '/' or '%' or '+' or '-';

        private static void FlushRun(ICollection<string> segments, StringBuilder run)
        {
            if (run.Length == 0)
            {
                return;
            }

            segments.Add(run.ToString());
            run.Clear();
        }
    }
}
