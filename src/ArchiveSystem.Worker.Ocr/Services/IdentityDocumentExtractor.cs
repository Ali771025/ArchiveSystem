using System.Text.RegularExpressions;

namespace ArchiveSystem.Worker.Ocr.Services
{
    public static class IdentityDocumentExtractor
    {
        private const string NationalIdCard = "NationalIdCard";
        private const string BirthCertificate = "BirthCertificate";
        private const string Generic = "Generic";

        private static readonly Regex DatePattern = new(
            @"(?<!\d)(?<year>1[234]\d{2})\s*[/\-.]\s*(?<month>0?[1-9]|1[0-2])\s*[/\-.]\s*(?<day>0?[1-9]|[12]\d|3[01])(?!\d)",
            RegexOptions.Compiled);

        private static readonly string[] AllLabels =
        {
            "شماره ملی", "کد ملی", "نام خانوادگی", "نام پدر", "تاریخ تولد", "پایان اعتبار",
            "نام", "خانوادگی", "شماره شناسنامه", "شماره سریال", "سری و سریال", "محل تولد", "محل صدور"
        };

        public static OcrResult Enrich(OcrResult source)
        {
            ArgumentNullException.ThrowIfNull(source);

            var text = PersianOcrText.Normalize(source.Text);
            var lines = BuildLines(source, text);
            var documentType = DetectDocumentType(text);
            if (documentType == Generic)
            {
                return source with
                {
                    Text = text,
                    DocumentType = Generic,
                    DocumentConfidence = 0,
                    Fields = Array.Empty<OcrFieldResult>()
                };
            }

            var fields = documentType == NationalIdCard
                ? ExtractNationalIdCard(lines, text)
                : ExtractBirthCertificate(lines, text);

            var warnings = source.Warnings.ToList();
            var nationalCode = fields.FirstOrDefault(field => field.Key == "nationalCode");
            if (nationalCode is null)
            {
                warnings.Add("identity-document-national-code-not-found");
            }
            else if (!nationalCode.IsValid)
            {
                warnings.Add("identity-document-national-code-invalid");
            }

            var invalidFields = fields.Count(field => !field.IsValid);
            if (invalidFields > 0)
            {
                warnings.Add("identity-document-fields-require-review");
            }

            var fieldConfidence = fields.Count == 0 ? 0 : fields.Average(field => field.Confidence);
            var typeConfidence = documentType == NationalIdCard ? 0.95f : 0.90f;
            var documentConfidence = NormalizeConfidence((typeConfidence + (float)fieldConfidence) / 2f);

            return source with
            {
                Text = text,
                DocumentType = documentType,
                DocumentConfidence = documentConfidence,
                Template = documentType,
                LayoutProfile = documentType == NationalIdCard ? "iran-national-id-card-rtl" : "iran-birth-certificate-rtl",
                Fields = fields,
                Warnings = warnings.Distinct(StringComparer.Ordinal).ToArray()
            };
        }

        public static bool IsValidIranianNationalCode(string? value)
        {
            var digits = PersianOcrText.DigitsOnly(value);
            if (digits.Length != 10 || digits.Distinct().Count() == 1)
            {
                return false;
            }

            var sum = 0;
            for (var index = 0; index < 9; index++)
            {
                sum += (digits[index] - '0') * (10 - index);
            }

            var remainder = sum % 11;
            var expectedCheckDigit = remainder < 2 ? remainder : 11 - remainder;
            return digits[9] - '0' == expectedCheckDigit;
        }

        private static IReadOnlyList<OcrFieldResult> ExtractNationalIdCard(
            IReadOnlyList<RecognizedLine> lines,
            string text)
        {
            var fields = new List<OcrFieldResult>();
            AddIfFound(fields, ExtractNationalCode(lines, text));
            AddIfFound(fields, ExtractNameField(lines, "firstName", "نام", new[] { "نام" }, "نام خانوادگی", "خانوادگی", "نام پدر"));
            AddIfFound(fields, ExtractNameField(lines, "lastName", "نام خانوادگی", new[] { "نام خانوادگی", "خانوادگی" }));
            AddIfFound(fields, ExtractDateField(lines, "birthDate", "تاریخ تولد", new[] { "تاریخ تولد", "تولد" }));
            AddIfFound(fields, ExtractNameField(lines, "fatherName", "نام پدر", new[] { "نام پدر" }));
            AddIfFound(fields, ExtractDateField(lines, "expiryDate", "پایان اعتبار", new[] { "پایان اعتبار", "اعتبار" }));
            return fields;
        }

        private static IReadOnlyList<OcrFieldResult> ExtractBirthCertificate(
            IReadOnlyList<RecognizedLine> lines,
            string text)
        {
            var fields = new List<OcrFieldResult>();
            AddIfFound(fields, ExtractNationalCode(lines, text));
            AddIfFound(fields, ExtractNameField(lines, "firstName", "نام", new[] { "نام" }, "نام خانوادگی", "خانوادگی", "نام پدر"));
            AddIfFound(fields, ExtractNameField(lines, "lastName", "نام خانوادگی", new[] { "نام خانوادگی", "خانوادگی" }));
            AddIfFound(fields, ExtractNameField(lines, "fatherName", "نام پدر", new[] { "نام پدر" }));
            AddIfFound(fields, ExtractDateField(lines, "birthDate", "تاریخ تولد", new[] { "تاریخ تولد", "تولد" }));
            AddIfFound(fields, ExtractDigitsField(lines, "certificateNumber", "شماره شناسنامه", new[] { "شماره شناسنامه" }, 1, 10));
            AddIfFound(fields, ExtractDigitsField(lines, "serialNumber", "سری و سریال", new[] { "شماره سریال", "سری و سریال", "سریال" }, 3, 16));
            AddIfFound(fields, ExtractNameField(lines, "birthPlace", "محل تولد", new[] { "محل تولد", "محل صدور" }));
            return fields;
        }

        private static OcrFieldResult? ExtractNationalCode(IReadOnlyList<RecognizedLine> lines, string text)
        {
            var labelled = FindByLabels(lines, new[] { "شماره ملی", "کد ملی" }, IsNationalCodeCandidate);
            var candidates = new List<FieldCandidate>();
            if (labelled is not null)
            {
                candidates.Add(labelled);
            }

            foreach (var line in lines)
            {
                var digits = PersianOcrText.DigitsOnly(line.Text);
                if (digits.Length == 10)
                {
                    candidates.Add(new FieldCandidate(digits, line.Confidence, line.PageNumber, line));
                }
            }

            foreach (Match match in Regex.Matches(text, @"(?<!\d)(?:\d[\s\-]*){10}(?!\d)"))
            {
                var digits = PersianOcrText.DigitsOnly(match.Value);
                if (digits.Length == 10)
                {
                    candidates.Add(new FieldCandidate(digits, 0.65f, 1, null));
                }
            }

            var best = candidates
                .OrderByDescending(candidate => IsValidIranianNationalCode(candidate.Value))
                .ThenByDescending(candidate => candidate.Confidence)
                .FirstOrDefault();
            if (best is null)
            {
                return null;
            }

            var normalized = PersianOcrText.DigitsOnly(best.Value);
            var isValid = IsValidIranianNationalCode(normalized);
            return CreateField(
                "nationalCode",
                "شماره ملی",
                normalized,
                normalized,
                best,
                isValid,
                isValid ? null : "رقم کنترل شماره ملی معتبر نیست.");
        }

        private static OcrFieldResult? ExtractDateField(
            IReadOnlyList<RecognizedLine> lines,
            string key,
            string label,
            IReadOnlyList<string> labels)
        {
            var candidate = FindByLabels(lines, labels, value => DatePattern.IsMatch(value));
            if (candidate is null)
            {
                return null;
            }

            var match = DatePattern.Match(PersianOcrText.Normalize(candidate.Value, false));
            if (!match.Success)
            {
                return null;
            }

            var normalized = $"{match.Groups["year"].Value}/{int.Parse(match.Groups["month"].Value):00}/{int.Parse(match.Groups["day"].Value):00}";
            var valid = IsValidJalaliDate(normalized);
            return CreateField(key, label, normalized, normalized, candidate, valid, valid ? null : "تاریخ استخراج‌شده معتبر نیست.");
        }

        private static OcrFieldResult? ExtractNameField(
            IReadOnlyList<RecognizedLine> lines,
            string key,
            string label,
            IReadOnlyList<string> labels,
            params string[] excludedLabels)
        {
            var candidate = FindByLabels(lines, labels, value => IsNameCandidate(value, excludedLabels));
            if (candidate is null)
            {
                return null;
            }

            var normalized = CleanName(candidate.Value, labels.Concat(excludedLabels));
            var valid = IsNameCandidate(normalized, Array.Empty<string>());
            return CreateField(key, label, normalized, normalized, candidate, valid, valid ? null : "نام استخراج‌شده نیازمند بازبینی است.");
        }

        private static OcrFieldResult? ExtractDigitsField(
            IReadOnlyList<RecognizedLine> lines,
            string key,
            string label,
            IReadOnlyList<string> labels,
            int minimumLength,
            int maximumLength)
        {
            var candidate = FindByLabels(lines, labels, value =>
            {
                var digits = PersianOcrText.DigitsOnly(value);
                return digits.Length >= minimumLength && digits.Length <= maximumLength;
            });
            if (candidate is null)
            {
                return null;
            }

            var normalized = PersianOcrText.DigitsOnly(candidate.Value);
            return CreateField(key, label, normalized, normalized, candidate, true, null);
        }

        private static FieldCandidate? FindByLabels(
            IReadOnlyList<RecognizedLine> lines,
            IReadOnlyList<string> labels,
            Func<string, bool> isCandidate)
        {
            var normalizedLabels = labels.Select(label => PersianOcrText.Normalize(label, false)).ToArray();
            for (var index = 0; index < lines.Count; index++)
            {
                var line = lines[index];
                var matchedLabel = normalizedLabels.FirstOrDefault(label => line.Text.Contains(label, StringComparison.Ordinal));
                if (matchedLabel is null)
                {
                    continue;
                }

                var inlineValue = CleanValue(line.Text.Replace(matchedLabel, "", StringComparison.Ordinal));
                if (isCandidate(inlineValue))
                {
                    return new FieldCandidate(inlineValue, line.Confidence, line.PageNumber, line);
                }

                var geometric = lines
                    .Where(candidate => candidate.PageNumber == line.PageNumber && candidate != line)
                    .Where(candidate => !ContainsKnownLabel(candidate.Text))
                    .Where(candidate => IsSameVisualRow(line, candidate))
                    .Where(candidate => isCandidate(CleanValue(candidate.Text)))
                    .OrderBy(candidate => Math.Abs(candidate.CenterY - line.CenterY))
                    .ThenBy(candidate => Math.Abs(candidate.Left - line.Left))
                    .FirstOrDefault();
                if (geometric is not null)
                {
                    return new FieldCandidate(CleanValue(geometric.Text), geometric.Confidence, geometric.PageNumber, geometric);
                }

                for (var offset = 1; offset <= 2 && index + offset < lines.Count; offset++)
                {
                    var adjacent = lines[index + offset];
                    var adjacentValue = CleanValue(adjacent.Text);
                    if (adjacent.PageNumber == line.PageNumber && !ContainsKnownLabel(adjacentValue) && isCandidate(adjacentValue))
                    {
                        return new FieldCandidate(adjacentValue, adjacent.Confidence, adjacent.PageNumber, adjacent);
                    }
                }
            }

            return null;
        }

        private static IReadOnlyList<RecognizedLine> BuildLines(OcrResult source, string normalizedText)
        {
            var result = new List<RecognizedLine>();
            foreach (var page in source.Pages)
            {
                if (page.LineElements.Count > 0)
                {
                    result.AddRange(page.LineElements
                        .Select(line => new RecognizedLine(
                            NormalizeLikelyIdentityLabel(line.Text),
                            NormalizeConfidence(line.Confidence),
                            page.PageNumber,
                            line.Left,
                            line.Top,
                            line.Width,
                            line.Height))
                        .Where(line => !string.IsNullOrWhiteSpace(line.Text)));
                    continue;
                }

                result.AddRange(page.Lines
                    .Select((line, index) => new RecognizedLine(
                        NormalizeLikelyIdentityLabel(line),
                        NormalizeConfidence(page.Confidence),
                        page.PageNumber,
                        0,
                        index * 24,
                        0,
                        20))
                    .Where(line => !string.IsNullOrWhiteSpace(line.Text)));
            }

            if (result.Count == 0)
            {
                result.AddRange(normalizedText
                    .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                    .Select((line, index) => new RecognizedLine(line, NormalizeConfidence(source.Confidence), 1, 0, index * 24, 0, 20)));
            }

            return result
                .GroupBy(line => $"{line.PageNumber}:{line.Top / 3}:{line.Text}", StringComparer.Ordinal)
                .Select(group => group.OrderByDescending(line => line.Confidence).First())
                .OrderBy(line => line.PageNumber)
                .ThenBy(line => line.Top)
                .ThenByDescending(line => line.Left)
                .ToArray();
        }

        private static string DetectDocumentType(string text)
        {
            var normalized = PersianOcrText.Normalize(text, false);
            var hasNationalCardMarker = normalized.Contains("کارت هوشمند ملی", StringComparison.Ordinal) ||
                                        normalized.Contains("پایان اعتبار", StringComparison.Ordinal);
            var hasBirthCertificateMarker = normalized.Contains("شناسنامه", StringComparison.Ordinal) ||
                                            normalized.Contains("سری و سریال", StringComparison.Ordinal);

            if (hasNationalCardMarker)
            {
                return NationalIdCard;
            }

            if (hasBirthCertificateMarker)
            {
                return BirthCertificate;
            }

            var nationalCardScore = Score(normalized, "کارت هوشمند ملی", "پایان اعتبار", "شماره ملی", "نام خانوادگی");
            var birthCertificateScore = Score(normalized, "شناسنامه", "شماره شناسنامه", "محل صدور", "سری و سریال");

            if (nationalCardScore >= 2 && nationalCardScore >= birthCertificateScore)
            {
                return NationalIdCard;
            }

            return birthCertificateScore >= 1 ? BirthCertificate : Generic;
        }

        private static int Score(string text, params string[] markers) =>
            markers.Count(marker => text.Contains(PersianOcrText.Normalize(marker, false), StringComparison.Ordinal));

        private static bool IsNationalCodeCandidate(string value) => PersianOcrText.DigitsOnly(value).Length == 10;

        private static bool IsNameCandidate(string value, IReadOnlyCollection<string> excludedLabels)
        {
            var normalized = CleanName(value, excludedLabels);
            var persianCharacters = normalized.Count(character => character is >= '\u0600' and <= '\u06FF');
            return persianCharacters >= 2 && !DatePattern.IsMatch(normalized) && PersianOcrText.DigitsOnly(normalized).Length == 0;
        }

        private static string CleanName(string value, IEnumerable<string> labels)
        {
            var normalized = PersianOcrText.Normalize(value, false);
            foreach (var label in labels.OrderByDescending(item => item.Length))
            {
                normalized = normalized.Replace(PersianOcrText.Normalize(label, false), " ", StringComparison.Ordinal);
            }

            normalized = Regex.Replace(normalized, @"[^\u0600-\u06FF\s]", " ");
            return Regex.Replace(normalized, @"\s+", " ").Trim(' ', ':', '-', '؛');
        }

        private static string CleanValue(string value) =>
            PersianOcrText.Normalize(value, false).Trim(' ', ':', '-', 'ـ', '؛', '|');

        private static string NormalizeLikelyIdentityLabel(string value)
        {
            var normalized = PersianOcrText.Normalize(value, false);
            var lettersOnly = Regex.Replace(normalized, @"[^\u0600-\u06FF]", string.Empty);

            if (lettersOnly == "تم" && normalized.Contains('='))
            {
                return "نام";
            }

            if (lettersOnly is "تامپدر" or "تامیدر")
            {
                return "نام پدر";
            }

            return normalized.Replace("خانوادکی", "خانوادگی", StringComparison.Ordinal);
        }

        private static bool ContainsKnownLabel(string value)
        {
            var normalized = PersianOcrText.Normalize(value, false);
            return AllLabels.Any(label => normalized.Contains(PersianOcrText.Normalize(label, false), StringComparison.Ordinal));
        }

        private static bool IsSameVisualRow(RecognizedLine label, RecognizedLine candidate)
        {
            if (label.Width == 0 || candidate.Width == 0)
            {
                return Math.Abs(label.CenterY - candidate.CenterY) <= 30;
            }

            var tolerance = Math.Max(14, Math.Max(label.Height, candidate.Height));
            return Math.Abs(label.CenterY - candidate.CenterY) <= tolerance;
        }

        private static bool IsValidJalaliDate(string value)
        {
            var match = DatePattern.Match(value);
            if (!match.Success ||
                !int.TryParse(match.Groups["year"].Value, out var year) ||
                !int.TryParse(match.Groups["month"].Value, out var month) ||
                !int.TryParse(match.Groups["day"].Value, out var day))
            {
                return false;
            }

            if (year is < 1200 or > 1499 || month is < 1 or > 12 || day < 1)
            {
                return false;
            }

            var maximumDay = month <= 6 ? 31 : month <= 11 ? 30 : 30;
            return day <= maximumDay;
        }

        private static OcrFieldResult CreateField(
            string key,
            string label,
            string value,
            string normalizedValue,
            FieldCandidate candidate,
            bool isValid,
            string? validationMessage) => new()
            {
                Key = key,
                Label = label,
                Value = value,
                NormalizedValue = normalizedValue,
                Confidence = NormalizeConfidence(candidate.Confidence),
                IsValid = isValid,
                PageNumber = candidate.PageNumber,
                ValidationMessage = validationMessage
            };

        private static float NormalizeConfidence(float value)
        {
            if (value > 1f)
            {
                value /= 100f;
            }

            return Math.Clamp(value, 0f, 1f);
        }

        private static void AddIfFound(ICollection<OcrFieldResult> fields, OcrFieldResult? field)
        {
            if (field is not null && !string.IsNullOrWhiteSpace(field.NormalizedValue))
            {
                fields.Add(field);
            }
        }

        private sealed record FieldCandidate(
            string Value,
            float Confidence,
            int PageNumber,
            RecognizedLine? Source);

        private sealed record RecognizedLine(
            string Text,
            float Confidence,
            int PageNumber,
            int Left,
            int Top,
            int Width,
            int Height)
        {
            public int CenterY => Top + (Height / 2);
        }
    }
}
