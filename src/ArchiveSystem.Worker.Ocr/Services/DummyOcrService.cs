// src/ArchiveSystem.Worker.Ocr/Services/DummyOcrService.cs
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace ArchiveSystem.Worker.Ocr.Services
{
    public class DummyOcrService : IOcrService
    {
        private static readonly string[] InvoiceTermsFa =
        {
            "فاکتور", "مبلغ", "مالیات", "مشتری", "پرداخت", "تاریخ", "جمع", "بدهی"
        };

        private static readonly string[] ContractTermsFa =
        {
            "قرارداد", "طرفین", "تعهد", "بند", "مدت", "امضا", "شرایط", "فسخ"
        };

        private static readonly string[] ArchiveTermsFa =
        {
            "آرشیو", "پرونده", "سند", "نسخه", "مرجع", "فراداده", "نگهداری", "طبقه‌بندی"
        };

        private static readonly string[] GenericTermsFa =
        {
            "سند", "متن", "محتوا", "بخش", "پاراگراف", "خلاصه", "بررسی", "اطلاعات"
        };

        private readonly ILogger<DummyOcrService> _logger;

        public DummyOcrService(ILogger<DummyOcrService> logger)
        {
            _logger = logger;
        }

        public async Task<OcrResult> ExtractAsync(
            Stream fileStream,
            string fileName,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(fileStream);
            fileName ??= string.Empty;
            contentType ??= "application/octet-stream";

            _logger.LogInformation(
                "Starting Farsi-aware dummy OCR for file: {FileName}, content type: {ContentType}",
                fileName,
                contentType);

            var descriptor = await BuildDescriptorAsync(fileStream, fileName, contentType, cancellationToken);
            await Task.Delay(descriptor.SimulatedDelay, cancellationToken);

            var pages = BuildPages(descriptor);
            var text = string.Join(Environment.NewLine + Environment.NewLine, pages.Select(p => p.Text)).Trim();
            var wordCount = CountWords(text);
            var result = new OcrResult
            {
                Engine = "dummy-advanced-fa",
                Language = descriptor.PrimaryLanguage,
                Confidence = descriptor.Confidence,
                PageCount = descriptor.PageCount,
                WordCount = wordCount,
                HasText = !string.IsNullOrWhiteSpace(text),
                LayoutProfile = descriptor.LayoutProfile,
                Template = descriptor.TemplateName,
                Title = descriptor.Title,
                ReferenceCode = descriptor.ReferenceCode,
                DetectedLanguages = descriptor.DetectedLanguages,
                Pages = pages,
                Warnings = descriptor.Warnings,
                Text = text
            };

            _logger.LogInformation(
                "Farsi-aware dummy OCR completed for file: {FileName}, characters: {CharCount}, words: {WordCount}, pages: {PageCount}",
                fileName,
                result.Text.Length,
                result.WordCount,
                result.PageCount);

            return result;
        }

        private static async Task<DocumentDescriptor> BuildDescriptorAsync(
            Stream fileStream,
            string fileName,
            string contentType,
            CancellationToken cancellationToken)
        {
            var sample = await ReadSampleAsync(fileStream, 4096, cancellationToken);
            var fileSize = ResolveFileSize(fileStream, sample.Length);
            var seed = ComputeSeed(fileName, contentType, fileSize, sample);
            var random = new Random(seed);
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var normalizedType = contentType.ToLowerInvariant();
            var template = SelectTemplate(fileName, normalizedType);
            var language = DetectPrimaryLanguage(fileName, sample);
            var pageCount = EstimatePageCount(fileSize, extension, normalizedType, random);
            var blockCount = Math.Max(2, Math.Min(8, pageCount * 2 + random.Next(0, 2)));
            var linesPerBlock = random.Next(2, 5);
            var wordsPerLine = random.Next(7, 14);
            var title = BuildTitle(fileName, template, language);
            var referenceCode = $"OCR-FA-{seed % 1000000:D6}";
            var simulatedDelay = TimeSpan.FromMilliseconds(250 + random.Next(150, 900));
            var confidence = (float)Math.Clamp(0.84 + (pageCount * 0.004) - (blockCount * 0.003), 0.73, 0.97);
            var layout = pageCount == 1 ? "single-page-rtl" : "multi-page-rtl";
            var warnings = BuildWarnings(normalizedType, extension, pageCount);

            return new DocumentDescriptor(
                fileName,
                contentType,
                extension,
                template,
                title,
                referenceCode,
                fileSize,
                pageCount,
                blockCount,
                linesPerBlock,
                wordsPerLine,
                simulatedDelay,
                random,
                language,
                BuildDetectedLanguages(language),
                confidence,
                layout,
                warnings);
        }

        private static List<OcrPageResult> BuildPages(DocumentDescriptor descriptor)
        {
            var pages = new List<OcrPageResult>(descriptor.PageCount);
            for (var page = 1; page <= descriptor.PageCount; page++)
            {
                var lines = new List<string>();
                lines.Add($"صفحه {page} | عنوان: {descriptor.Title}");
                lines.Add($"کد مرجع: {descriptor.ReferenceCode} | الگو: {descriptor.TemplateName}");

                for (var block = 1; block <= descriptor.BlockCountPerPage; block++)
                {
                    lines.Add($"بخش {page}.{block}: {BuildSectionHeading(descriptor, page, block)}");
                    for (var line = 0; line < descriptor.LinesPerBlock; line++)
                    {
                        lines.Add(BuildLine(descriptor, page, block, line));
                    }
                }

                lines.Add($"یادداشت OCR: متن تولیدشده آزمایشی است و برای تست یکپارچه‌سازی استفاده می‌شود.");
                var pageText = string.Join(Environment.NewLine, lines);
                pages.Add(new OcrPageResult
                {
                    PageNumber = page,
                    Confidence = MathF.Max(0.70f, descriptor.Confidence - ((page - 1) * 0.01f)),
                    Lines = lines,
                    Text = pageText
                });
            }

            return pages;
        }

        private static async Task<byte[]> ReadSampleAsync(Stream stream, int maxBytes, CancellationToken cancellationToken)
        {
            if (!stream.CanRead)
            {
                return Array.Empty<byte>();
            }

            var buffer = new byte[maxBytes];
            var totalRead = 0;

            if (stream.CanSeek)
            {
                stream.Seek(0, SeekOrigin.Begin);
            }

            while (totalRead < maxBytes)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(totalRead, maxBytes - totalRead), cancellationToken);
                if (read == 0)
                {
                    break;
                }

                totalRead += read;
            }

            if (stream.CanSeek)
            {
                stream.Seek(0, SeekOrigin.Begin);
            }

            return buffer[..totalRead];
        }

        private static long ResolveFileSize(Stream stream, int sampledLength)
        {
            if (stream.CanSeek)
            {
                return Math.Max(0, stream.Length);
            }

            return sampledLength;
        }

        private static int ComputeSeed(string fileName, string contentType, long fileSize, byte[] sample)
        {
            var material = Encoding.UTF8.GetBytes($"{fileName}|{contentType}|{fileSize}");
            var payload = new byte[material.Length + sample.Length];
            Buffer.BlockCopy(material, 0, payload, 0, material.Length);
            Buffer.BlockCopy(sample, 0, payload, material.Length, sample.Length);

            var hash = SHA256.HashData(payload);
            return BitConverter.ToInt32(hash, 0) & int.MaxValue;
        }

        private static string SelectTemplate(string fileName, string contentType)
        {
            var normalizedName = fileName.ToLowerInvariant();

            if (normalizedName.Contains("invoice") || normalizedName.Contains("receipt") || normalizedName.Contains("factor"))
            {
                return "Invoice";
            }

            if (normalizedName.Contains("contract") || normalizedName.Contains("agreement") || normalizedName.Contains("gharardad"))
            {
                return "Contract";
            }

            if (normalizedName.Contains("archive") || normalizedName.Contains("record") || normalizedName.Contains("metadata"))
            {
                return "Archive";
            }

            if (contentType.Contains("pdf"))
            {
                return "PdfDocument";
            }

            if (contentType.StartsWith("image/"))
            {
                return "ScannedImage";
            }

            return "Generic";
        }

        private static string DetectPrimaryLanguage(string fileName, byte[] sample)
        {
            if (ContainsPersianText(fileName) || ContainsPersianText(Encoding.UTF8.GetString(sample)))
            {
                return "fa";
            }

            return "fa";
        }

        private static bool ContainsPersianText(string value)
        {
            return value.Any(ch => ch is >= '\u0600' and <= '\u06FF');
        }

        private static IReadOnlyList<string> BuildDetectedLanguages(string primaryLanguage)
        {
            return primaryLanguage == "fa"
                ? new[] { "fa", "en" }
                : new[] { primaryLanguage };
        }

        private static IReadOnlyList<string> BuildWarnings(string contentType, string extension, int pageCount)
        {
            var warnings = new List<string>
            {
                "dummy-engine-output",
                "development-only"
            };

            if (contentType.StartsWith("image/"))
            {
                warnings.Add("simulated-image-ocr");
            }

            if (extension == ".pdf" || contentType.Contains("pdf"))
            {
                warnings.Add("simulated-pdf-layout");
            }

            if (pageCount > 5)
            {
                warnings.Add("large-document-estimate");
            }

            return warnings;
        }

        private static int EstimatePageCount(long fileSize, string extension, string contentType, Random random)
        {
            if (contentType.StartsWith("image/") || extension is ".png" or ".jpg" or ".jpeg" or ".bmp" or ".tif" or ".tiff")
            {
                return 1;
            }

            var basePages = (int)Math.Max(1, Math.Ceiling(fileSize / 85000d));
            if (contentType.Contains("pdf") || extension == ".pdf")
            {
                basePages += random.Next(0, 3);
            }

            return Math.Min(20, Math.Max(1, basePages));
        }

        private static string BuildTitle(string fileName, string template, string language)
        {
            var baseName = Path.GetFileNameWithoutExtension(fileName);
            if (string.IsNullOrWhiteSpace(baseName))
            {
                return language == "fa" ? $"سند {template}" : $"{template} Document";
            }

            var normalized = baseName.Replace('_', ' ').Replace('-', ' ').Trim();
            return normalized.Length == 0
                ? (language == "fa" ? $"سند {template}" : $"{template} Document")
                : normalized;
        }

        private static string BuildSectionHeading(DocumentDescriptor descriptor, int page, int block)
        {
            return descriptor.TemplateName switch
            {
                "Invoice" => $"خلاصه مالی و اقلام فاکتور {page}-{block}",
                "Contract" => $"بند قراردادی و تعهدات {page}-{block}",
                "Archive" => $"فراداده و اطلاعات آرشیوی {page}-{block}",
                "PdfDocument" => $"تحلیل سند و ساختار صفحه {page}-{block}",
                "ScannedImage" => $"استخراج متن از تصویر {page}-{block}",
                _ => $"بخش سند {page}-{block}"
            };
        }

        private static string BuildLine(DocumentDescriptor descriptor, int page, int block, int line)
        {
            var lexicon = GetLexicon(descriptor.TemplateName);
            var words = new List<string>(descriptor.WordsPerLine + 8)
            {
                $"صفحه{page}",
                $"بلوک{block}",
                $"خط{line + 1}",
                descriptor.TemplateName
            };

            for (var index = 0; index < descriptor.WordsPerLine; index++)
            {
                var token = lexicon[(page + block + line + index + descriptor.Random.Next(0, lexicon.Length)) % lexicon.Length];
                words.Add(token);
            }

            if (descriptor.TemplateName == "Invoice")
            {
                words.Add($"مبلغ:{descriptor.Random.Next(120, 9800)}000");
                words.Add($"مالیات:{descriptor.Random.Next(5, 18)}درصد");
            }
            else if (descriptor.TemplateName == "Contract")
            {
                words.Add($"مدت:{descriptor.Random.Next(6, 36)}ماه");
            }
            else if (descriptor.TemplateName == "Archive")
            {
                words.Add($"نگهداری:{descriptor.Random.Next(1, 15)}سال");
            }

            return string.Join(' ', words);
        }

        private static string[] GetLexicon(string templateName)
        {
            return templateName switch
            {
                "Invoice" => InvoiceTermsFa,
                "Contract" => ContractTermsFa,
                "Archive" => ArchiveTermsFa,
                _ => GenericTermsFa
            };
        }

        private static int CountWords(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return 0;
            }

            return text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length;
        }

        private sealed record DocumentDescriptor(
            string FileName,
            string ContentType,
            string Extension,
            string TemplateName,
            string Title,
            string ReferenceCode,
            long FileSize,
            int PageCount,
            int BlockCountPerPage,
            int LinesPerBlock,
            int WordsPerLine,
            TimeSpan SimulatedDelay,
            Random Random,
            string PrimaryLanguage,
            IReadOnlyList<string> DetectedLanguages,
            float Confidence,
            string LayoutProfile,
            IReadOnlyList<string> Warnings);
    }
}
