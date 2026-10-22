using System.Text.RegularExpressions;
using ArchiveSystem.Worker.Ocr.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkiaSharp;
using PdfConversion = PDFtoImage.Conversion;
using PdfRenderOptions = PDFtoImage.RenderOptions;
using RapidEngine = RapidOcrNet.RapidOcr;
using RapidEngineOptions = RapidOcrNet.RapidOcrOptions;
using RapidEngineResult = RapidOcrNet.OcrResult;

namespace ArchiveSystem.Worker.Ocr.Services
{
    public sealed class RapidOcrService : IOcrService
    {
        private static readonly HashSet<string> SupportedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".bmp", ".gif", ".jfif", ".jpeg", ".jpg", ".png", ".tif", ".tiff", ".webp"
        };

        private readonly RapidOcrEngineOptions _options;
        private readonly ILogger<RapidOcrService> _logger;

        public RapidOcrService(
            IOptions<RapidOcrEngineOptions> options,
            ILogger<RapidOcrService> logger)
        {
            _options = options.Value;
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

            EnsureSupportedInput(fileName, contentType);
            var tempPath = await CopyToTempFileAsync(fileStream, fileName, contentType, cancellationToken);
            var startedAt = DateTime.UtcNow;

            try
            {
                var isPdf = IsPdf(fileName, contentType);
                var usedIdentityRegionPass = false;
                var usedPdfNativeText = false;
                var usedPdfOcr = false;
                var engineName = "rapidocrnet-3.0.0-paddleocr-v5-arabic-persian";
                IReadOnlyList<OcrPageResult> pages;
                if (isPdf)
                {
                    var pdf = await ReadPdfAsync(tempPath, cancellationToken);
                    pages = pdf.Pages;
                    usedIdentityRegionPass = pdf.UsedIdentityRegionPass;
                    usedPdfNativeText = pdf.UsedNativeText;
                    usedPdfOcr = pdf.UsedOcr;
                    engineName = pdf.Engine;
                }
                else
                {
                    var models = RapidOcrOfflineRuntime.EnsureValidated(_options, _logger);
                    using var sessionOptions = RapidEngine.GetDefaultSessionOptions(
                        Math.Clamp(_options.ThreadCount, 1, 16));
                    using var engine = new RapidEngine();
                    engine.InitModels(
                        models.Detection,
                        models.Classification,
                        models.Recognition,
                        models.Dictionary,
                        sessionOptions);
                    var image = ReadImage(engine, tempPath, 1, cancellationToken);
                    pages = new[] { image.Page };
                    usedIdentityRegionPass = image.UsedIdentityRegionPass;
                }

                var text = string.Join(
                    Environment.NewLine,
                    pages.Select(page => page.Text).Where(value => !string.IsNullOrWhiteSpace(value)));
                var confidence = pages.Count == 0 ? 0 : pages.Average(page => page.Confidence);
                var warnings = new List<string>();

                if (usedPdfOcr)
                {
                    warnings.Add("pdf-rasterized-with-pdftoimage");
                }

                if (usedPdfNativeText)
                {
                    warnings.Add("pdf-native-text-extracted");
                }

                if (usedPdfNativeText && usedPdfOcr)
                {
                    warnings.Add("pdf-hybrid-native-text-and-ocr");
                }

                if (usedIdentityRegionPass)
                {
                    warnings.Add("identity-card-region-pass");
                }

                if (confidence < Math.Clamp(_options.MinimumAcceptedConfidence, 0f, 1f))
                {
                    warnings.Add("ocr-low-confidence");
                }

                var result = new OcrResult
                {
                    Engine = engineName,
                    Language = "fa",
                    Confidence = confidence,
                    PageCount = Math.Max(1, pages.Count),
                    WordCount = CountWords(text),
                    ProcessingDurationMs = Math.Max(1, (long)(DateTime.UtcNow - startedAt).TotalMilliseconds),
                    HasText = !string.IsNullOrWhiteSpace(text),
                    LayoutProfile = pages.Count > 1 ? "multi-page-rtl" : "single-page-rtl",
                    Template = "Generic",
                    DocumentType = "Generic",
                    DocumentConfidence = 0,
                    Title = Path.GetFileNameWithoutExtension(fileName),
                    ReferenceCode = $"OCR-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..32],
                    DetectedLanguages = new[] { "fa", "ar", "en" },
                    Pages = pages,
                    Warnings = warnings,
                    Text = text
                };

                return _options.EnableIdentityDocumentMode
                    ? IdentityDocumentExtractor.Enrich(result)
                    : result;
            }
            finally
            {
                TryDeleteTempFile(tempPath);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Interoperability",
            "CA1416:Validate platform compatibility",
            Justification = "The worker explicitly guards and supports Windows, Linux, and macOS hosts.")]
        private async Task<PdfReadResult> ReadPdfAsync(
            string pdfPath,
            CancellationToken cancellationToken)
        {
            if (!OperatingSystem.IsWindows() &&
                !OperatingSystem.IsLinux() &&
                !OperatingSystem.IsMacOS())
            {
                throw new PlatformNotSupportedException(
                    "PDF OCR is supported on Windows, Linux, and macOS worker hosts.");
            }

            IReadOnlyList<PdfNativePageContent> nativePages = Array.Empty<PdfNativePageContent>();
            if (_options.EnablePdfNativeTextExtraction)
            {
                try
                {
                    nativePages = PdfNativeTextExtractor.Extract(
                        pdfPath,
                        _options.PdfNativeTextMinimumCharacters,
                        _options.PdfImageOcrMinimumCoverage);
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(
                        exception,
                        "Native PDF text extraction failed; all pages will use OCR fallback.");
                }
            }

            var pages = nativePages
                .Where(page => page.HasMeaningfulText)
                .ToDictionary(page => page.PageNumber, ToNativePageResult);
            var pagesForOcr = nativePages.Count == 0
                ? Array.Empty<PdfNativePageContent>()
                : nativePages
                    .Where(page =>
                        !page.HasMeaningfulText ||
                        (_options.OcrPdfImagesWhenNativeTextExists && page.HasSignificantImage))
                    .ToArray();
            var usedIdentityRegionPass = false;
            var usedOcr = nativePages.Count == 0 || pagesForOcr.Length > 0;

            if (!usedOcr)
            {
                return new PdfReadResult(
                    pages.Values.OrderBy(page => page.PageNumber).ToArray(),
                    false,
                    true,
                    false,
                    "pdfpig-0.1.15-native-text");
            }

            await using var stream = new FileStream(
                pdfPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 81920,
                useAsync: true);
            var renderOptions = new PdfRenderOptions(Dpi: Math.Clamp(_options.PdfDpi, 72, 600));
            var models = RapidOcrOfflineRuntime.EnsureValidated(_options, _logger);
            using var sessionOptions = RapidEngine.GetDefaultSessionOptions(
                Math.Clamp(_options.ThreadCount, 1, 16));
            using var engine = new RapidEngine();
            engine.InitModels(
                models.Detection,
                models.Classification,
                models.Recognition,
                models.Dictionary,
                sessionOptions);

            var selectedPageIndexes = pagesForOcr.Select(page => page.PageNumber - 1).ToArray();
            var renderedPageNumber = 0;
            var renderedPages = nativePages.Count == 0
                ? PdfConversion.ToImagesAsync(
                    stream,
                    leaveOpen: true,
                    password: null,
                    options: renderOptions,
                    cancellationToken: cancellationToken)
                : PdfConversion.ToImagesAsync(
                    stream,
                    selectedPageIndexes,
                    leaveOpen: true,
                    password: null,
                    options: renderOptions,
                    cancellationToken: cancellationToken);

            await foreach (var bitmap in renderedPages)
            {
                using (bitmap)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var pageNumber = nativePages.Count == 0
                        ? renderedPageNumber + 1
                        : pagesForOcr[renderedPageNumber].PageNumber;
                    var page = ReadBitmapWithIdentityRegion(engine, bitmap, pageNumber);
                    var nativePage = nativePages.FirstOrDefault(item => item.PageNumber == pageNumber);
                    pages[pageNumber] = nativePage is { HasMeaningfulText: true }
                        ? MergeNativeAndOcrPage(ToNativePageResult(nativePage), page.Page)
                        : page.Page;
                    usedIdentityRegionPass |= page.UsedIdentityRegionPass;
                    renderedPageNumber++;
                }
            }

            if (pages.Count == 0)
            {
                throw new InvalidOperationException("PDF contains no renderable pages.");
            }

            var usedNativeText = nativePages.Any(page => page.HasMeaningfulText);
            return new PdfReadResult(
                pages.Values.OrderBy(page => page.PageNumber).ToArray(),
                usedIdentityRegionPass,
                usedNativeText,
                true,
                usedNativeText
                    ? "rapidocrnet-3.0.0-paddleocr-v5-arabic-persian-pdfpig-hybrid"
                    : "rapidocrnet-3.0.0-paddleocr-v5-arabic-persian");
        }

        private static OcrPageResult ToNativePageResult(PdfNativePageContent page)
        {
            var lineHeight = page.Lines.Count == 0
                ? 1
                : Math.Max(1, page.Height / page.Lines.Count);
            var lines = page.Lines
                .Select((text, index) => new OcrLineResult
                {
                    Text = text,
                    Confidence = 1f,
                    Left = 0,
                    Top = index * lineHeight,
                    Width = page.Width,
                    Height = lineHeight
                })
                .ToArray();

            return new OcrPageResult
            {
                PageNumber = page.PageNumber,
                Confidence = 1f,
                Width = page.Width,
                Height = page.Height,
                Text = page.Text,
                Lines = page.Lines,
                LineElements = lines
            };
        }

        private static OcrPageResult MergeNativeAndOcrPage(
            OcrPageResult nativePage,
            OcrPageResult ocrPage)
        {
            var nativeText = PersianOcrText.Normalize(nativePage.Text, false);
            var additionalOcrLines = ocrPage.LineElements
                .Where(line => !IsDuplicateOcrLine(nativeText, line.Text))
                .ToArray();
            var lines = nativePage.LineElements.Concat(additionalOcrLines).ToArray();
            var textLines = nativePage.Lines
                .Concat(additionalOcrLines.Select(line => line.Text))
                .ToArray();

            return new OcrPageResult
            {
                PageNumber = nativePage.PageNumber,
                Confidence = additionalOcrLines.Length == 0
                    ? nativePage.Confidence
                    : (nativePage.Confidence + ocrPage.Confidence) / 2f,
                Width = Math.Max(nativePage.Width, ocrPage.Width),
                Height = Math.Max(nativePage.Height, ocrPage.Height),
                Text = string.Join(Environment.NewLine, textLines),
                Lines = textLines,
                LineElements = lines
            };
        }

        private static bool IsDuplicateOcrLine(string normalizedNativeText, string ocrLine)
        {
            var normalizedLine = PersianOcrText.Normalize(ocrLine, false);
            if (normalizedLine.Length < 2)
            {
                return true;
            }

            if (normalizedNativeText.Contains(normalizedLine, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var compactNative = Regex.Replace(normalizedNativeText, @"\s+", string.Empty);
            var compactLine = Regex.Replace(normalizedLine, @"\s+", string.Empty);
            return compactLine.Length >= 4 &&
                   compactNative.Contains(compactLine, StringComparison.OrdinalIgnoreCase);
        }

        private ImageReadResult ReadImage(
            RapidEngine engine,
            string imagePath,
            int pageNumber,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var bitmap = SKBitmap.Decode(imagePath)
                ?? throw new InvalidOperationException("OCR image could not be decoded.");
            return ReadBitmapWithIdentityRegion(engine, bitmap, pageNumber);
        }

        private ImageReadResult ReadBitmapWithIdentityRegion(
            RapidEngine engine,
            SKBitmap bitmap,
            int pageNumber)
        {
            var fullPage = ReadBitmap(engine, bitmap, pageNumber);

            if (!_options.EnableIdentityDocumentMode || !HasIdentityCardAspectRatio(bitmap))
            {
                return new ImageReadResult(fullPage, false);
            }

            var bounds = new SKRectI(
                (int)(bitmap.Width * 0.47f),
                (int)(bitmap.Height * 0.16f),
                (int)(bitmap.Width * 0.98f),
                (int)(bitmap.Height * 0.90f));
            using var crop = new SKBitmap();
            if (!bitmap.ExtractSubset(crop, bounds))
            {
                return new ImageReadResult(fullPage, false);
            }

            const int scale = 3;
            using var scaled = new SKBitmap(
                crop.Width * scale,
                crop.Height * scale,
                crop.ColorType,
                crop.AlphaType);
            using (var canvas = new SKCanvas(scaled))
            {
                canvas.Clear(SKColors.White);
                canvas.DrawBitmap(
                    crop,
                    new SKRect(0, 0, scaled.Width, scaled.Height),
                    new SKSamplingOptions(SKFilterMode.Linear));
            }

            var regionPage = ReadBitmap(
                engine,
                scaled,
                pageNumber,
                imgResize: Math.Max(1600, _options.ImgResize),
                textScore: Math.Min(0.35f, _options.TextScore));
            var shouldUseRegion = regionPage.LineElements.Count > fullPage.LineElements.Count &&
                                  IdentityMarkerScore(regionPage.Text) >= IdentityMarkerScore(fullPage.Text) &&
                                  IdentityMarkerScore(regionPage.Text) >= 2;

            return shouldUseRegion
                ? new ImageReadResult(RemapCroppedPage(regionPage, bitmap, bounds, scale), true)
                : new ImageReadResult(fullPage, false);
        }

        private OcrPageResult ReadBitmap(
            RapidEngine engine,
            SKBitmap bitmap,
            int pageNumber,
            int? imgResize = null,
            float? textScore = null)
        {
            var engineOptions = RapidEngineOptions.Default with
            {
                ImgResize = Math.Clamp(imgResize ?? _options.ImgResize, 512, 4096),
                TextScore = Math.Clamp(textScore ?? _options.TextScore, 0.05f, 0.99f),
                DoAngle = _options.DoAngle,
                ReturnWordBox = true
            };
            var raw = engine.Detect(bitmap, engineOptions);
            var lines = ToLineElements(raw)
                .OrderBy(line => line.Top)
                .ThenByDescending(line => line.Left)
                .ToArray();
            var text = string.Join(Environment.NewLine, lines.Select(line => line.Text));
            var confidence = lines.Length == 0 ? 0 : lines.Average(line => line.Confidence);

            return new OcrPageResult
            {
                PageNumber = pageNumber,
                Confidence = confidence,
                Width = bitmap.Width,
                Height = bitmap.Height,
                Text = text,
                Lines = lines.Select(line => line.Text).ToArray(),
                LineElements = lines
            };
        }

        private static bool HasIdentityCardAspectRatio(SKBitmap bitmap)
        {
            if (bitmap.Height <= 0)
            {
                return false;
            }

            var ratio = (double)bitmap.Width / bitmap.Height;
            return ratio is >= 1.35 and <= 1.85;
        }

        private static int IdentityMarkerScore(string text)
        {
            var normalized = PersianOcrText.Normalize(text, false);
            string[] markers = ["شماره ملی", "خانواد", "تولد", "پدر", "اعتبار"];
            return markers.Count(marker => normalized.Contains(marker, StringComparison.Ordinal));
        }

        private static OcrPageResult RemapCroppedPage(
            OcrPageResult page,
            SKBitmap original,
            SKRectI cropBounds,
            int scale)
        {
            var lines = page.LineElements
                .Select(line => line with
                {
                    Left = cropBounds.Left + line.Left / scale,
                    Top = cropBounds.Top + line.Top / scale,
                    Width = line.Width / scale,
                    Height = line.Height / scale
                })
                .ToArray();

            return new OcrPageResult
            {
                PageNumber = page.PageNumber,
                Confidence = page.Confidence,
                Width = original.Width,
                Height = original.Height,
                Text = page.Text,
                Lines = page.Lines,
                LineElements = lines
            };
        }

        private static IReadOnlyList<OcrLineResult> ToLineElements(RapidEngineResult result)
        {
            return result.TextBlocks
                .Select(block =>
                {
                    var points = block.BoxPoints;
                    var left = points.Length == 0 ? 0 : (int)Math.Floor(Convert.ToDouble(points.Min(point => point.X)));
                    var top = points.Length == 0 ? 0 : (int)Math.Floor(Convert.ToDouble(points.Min(point => point.Y)));
                    var right = points.Length == 0 ? left : (int)Math.Ceiling(Convert.ToDouble(points.Max(point => point.X)));
                    var bottom = points.Length == 0 ? top : (int)Math.Ceiling(Convert.ToDouble(points.Max(point => point.Y)));
                    var confidence = block.CharScores is { Length: > 0 }
                        ? block.CharScores.Average()
                        : 0;
                    var logicalText = PaddleArabicTextDecoder.RestoreLogicalOrder(block.Text);

                    return new OcrLineResult
                    {
                        Text = PersianOcrText.Normalize(logicalText, false),
                        Confidence = Math.Clamp(confidence, 0f, 1f),
                        Left = Math.Max(0, left),
                        Top = Math.Max(0, top),
                        Width = Math.Max(0, right - left),
                        Height = Math.Max(0, bottom - top)
                    };
                })
                .Where(line => !string.IsNullOrWhiteSpace(line.Text))
                .ToArray();
        }

        private async Task<string> CopyToTempFileAsync(
            Stream source,
            string fileName,
            string contentType,
            CancellationToken cancellationToken)
        {
            if (source.CanSeek)
            {
                source.Seek(0, SeekOrigin.Begin);
                ThrowIfInputTooLarge(source.Length);
            }

            var directory = string.IsNullOrWhiteSpace(_options.TempDirectory)
                ? Path.Combine(Path.GetTempPath(), "archive-rapidocr")
                : _options.TempDirectory.Trim();
            Directory.CreateDirectory(directory);

            var tempPath = Path.Combine(directory, $"{Guid.NewGuid():N}{ResolveExtension(fileName, contentType)}");
            long totalBytes = 0;

            try
            {
                await using var destination = new FileStream(
                    tempPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.Read,
                    bufferSize: 81920,
                    useAsync: true);
                var buffer = new byte[81920];

                while (true)
                {
                    var read = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                    if (read == 0)
                    {
                        break;
                    }

                    totalBytes += read;
                    ThrowIfInputTooLarge(totalBytes);
                    await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                }
            }
            catch
            {
                TryDeleteTempFile(tempPath);
                throw;
            }

            return tempPath;
        }

        private void ThrowIfInputTooLarge(long size)
        {
            if (_options.MaxInputBytes > 0 && size > _options.MaxInputBytes)
            {
                throw new InvalidOperationException(
                    $"OCR input is too large. Size={size} bytes, Limit={_options.MaxInputBytes} bytes.");
            }
        }

        private static bool IsPdf(string fileName, string contentType) =>
            contentType.Contains("pdf", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);

        private static void EnsureSupportedInput(string fileName, string contentType)
        {
            if (IsPdf(fileName, contentType) ||
                contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ||
                SupportedImageExtensions.Contains(Path.GetExtension(fileName)))
            {
                return;
            }

            throw new NotSupportedException(
                $"OCR supports PDF and image files only. File='{Path.GetFileName(fileName)}', ContentType='{contentType}'.");
        }

        private static string ResolveExtension(string fileName, string contentType)
        {
            var extension = Path.GetExtension(fileName);
            if (!string.IsNullOrWhiteSpace(extension)) return extension;
            if (contentType.Contains("pdf", StringComparison.OrdinalIgnoreCase)) return ".pdf";
            if (contentType.Contains("jpeg", StringComparison.OrdinalIgnoreCase) || contentType.Contains("jpg", StringComparison.OrdinalIgnoreCase)) return ".jpg";
            if (contentType.Contains("png", StringComparison.OrdinalIgnoreCase)) return ".png";
            if (contentType.Contains("tiff", StringComparison.OrdinalIgnoreCase) || contentType.Contains("tif", StringComparison.OrdinalIgnoreCase)) return ".tiff";
            return ".bin";
        }

        private static void TryDeleteTempFile(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch
            {
                // Best-effort cleanup; preserve the original OCR failure.
            }
        }

        private static int CountWords(string text) =>
            string.IsNullOrWhiteSpace(text) ? 0 : Regex.Matches(text, @"\S+").Count;

        private sealed record ImageReadResult(OcrPageResult Page, bool UsedIdentityRegionPass);

        private sealed record PdfReadResult(
            IReadOnlyList<OcrPageResult> Pages,
            bool UsedIdentityRegionPass,
            bool UsedNativeText,
            bool UsedOcr,
            string Engine);
    }
}
