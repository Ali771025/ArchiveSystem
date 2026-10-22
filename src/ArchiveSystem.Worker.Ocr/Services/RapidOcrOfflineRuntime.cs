using ArchiveSystem.Worker.Ocr.Options;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace ArchiveSystem.Worker.Ocr.Services
{
    internal static class RapidOcrOfflineRuntime
    {
        private const string ExpectedRecognitionSha256 =
            "5b62055fc6209fa3bb247a9a2a7a9d5100c30868bad8a2fa49ed062f64b83021";
        private const string ExpectedDictionarySha256 =
            "7f92f7dbb9b75a4787a83bfb4f6d14a8ab515525130c9d40a9036f61cf6999e9";

        public static bool TryValidate(
            RapidOcrEngineOptions options,
            ILogger logger,
            out string failureReason)
        {
            var paths = ResolveModelPaths(options);
            var missing = paths.All
                .Where(model => !File.Exists(model.Path))
                .Select(model => $"{model.Name}: {model.Path}")
                .ToArray();

            if (missing.Length > 0)
            {
                failureReason = $"فایل‌های مدل آفلاین OCR یافت نشدند: {string.Join("; ", missing)}";
                return false;
            }

            var recognitionSha256 = ComputeSha256(paths.Recognition);
            var dictionarySha256 = ComputeSha256(paths.Dictionary);
            if (!recognitionSha256.Equals(ExpectedRecognitionSha256, StringComparison.OrdinalIgnoreCase) ||
                !dictionarySha256.Equals(ExpectedDictionarySha256, StringComparison.OrdinalIgnoreCase))
            {
                failureReason =
                    "هش مدل یا دیکشنری فارسی OCR معتبر نیست. فایل‌ها را از بسته مدل تاییدشده جایگزین کنید.";
                return false;
            }

            logger.LogInformation(
                "RapidOCR offline models validated. Detection={DetectionModel}; Classification={ClassificationModel}; Recognition={RecognitionModel}; Dictionary={Dictionary}",
                paths.Detection,
                paths.Classification,
                paths.Recognition,
                paths.Dictionary);
            failureReason = string.Empty;
            return true;
        }

        public static RapidOcrModelPaths EnsureValidated(
            RapidOcrEngineOptions options,
            ILogger logger)
        {
            if (!TryValidate(options, logger, out var failureReason))
            {
                throw new InvalidOperationException(failureReason);
            }

            return ResolveModelPaths(options);
        }

        internal static RapidOcrModelPaths ResolveModelPaths(RapidOcrEngineOptions options) => new(
            Resolve(options.DetectionModelPath),
            Resolve(options.ClassificationModelPath),
            Resolve(options.RecognitionModelPath),
            Resolve(options.DictionaryPath));

        private static string Resolve(string configuredPath)
        {
            if (string.IsNullOrWhiteSpace(configuredPath))
            {
                return AppContext.BaseDirectory;
            }

            return Path.GetFullPath(
                Path.IsPathRooted(configuredPath)
                    ? configuredPath
                    : Path.Combine(AppContext.BaseDirectory, configuredPath));
        }

        private static string ComputeSha256(string path)
        {
            using var stream = File.OpenRead(path);
            return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
        }
    }

    internal sealed record RapidOcrModelPaths(
        string Detection,
        string Classification,
        string Recognition,
        string Dictionary)
    {
        public IReadOnlyList<(string Name, string Path)> All =>
        [
            ("Detection", Detection),
            ("Classification", Classification),
            ("Recognition", Recognition),
            ("Dictionary", Dictionary)
        ];
    }
}
