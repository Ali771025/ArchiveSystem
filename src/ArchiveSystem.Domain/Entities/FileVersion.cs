using ArchiveSystem.Domain.Enums;

namespace ArchiveSystem.Domain.Entities
{
    public class FileVersion : BaseEntity<long>
    {
        public long FileId { get; private set; }
        public int VersionNumber { get; private set; }
        public string StoragePath { get; private set; }
        public string FileName { get; private set; }
        public string ContentType { get; private set; }
        public long Size { get; private set; }
        public string Sha256 { get; private set; }
        public DateTime? DeletedAt { get; private set; }
        public bool IsDeleted => DeletedAt.HasValue;

        public byte[]? Thumbnail { get; private set; }
        public string? ThumbnailContentType { get; private set; }
        public int? ThumbnailWidth { get; private set; }
        public int? ThumbnailHeight { get; private set; }
        public ProcessingStatus ThumbnailStatus { get; private set; }
        public string? ThumbnailError { get; private set; }

        public string? OcrResult { get; private set; }
        public string? OcrText { get; private set; }
        public ProcessingStatus OcrStatus { get; private set; }
        public string? OcrError { get; private set; }
        public string? OcrLanguage { get; private set; }
        public int? OcrWordCount { get; private set; }

        public ArchiveFile File { get; private set; } = null!;

        private FileVersion() : base()
        {
            StoragePath = string.Empty;
            FileName = string.Empty;
            ContentType = string.Empty;
            Sha256 = string.Empty;
            ThumbnailStatus = ProcessingStatus.Pending;
            OcrStatus = ProcessingStatus.Pending;
        }

        public FileVersion(
            long id,
            long fileId,
            int versionNumber,
            string storagePath,
            string fileName,
            string contentType,
            long size,
            string sha256)
            : base(id)
        {
            if (fileId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(fileId));
            }

            if (versionNumber <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(versionNumber));
            }

            if (string.IsNullOrWhiteSpace(storagePath))
            {
                throw new ArgumentException("Storage path is required.", nameof(storagePath));
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("File name is required.", nameof(fileName));
            }

            if (string.IsNullOrWhiteSpace(contentType))
            {
                throw new ArgumentException("Content type is required.", nameof(contentType));
            }

            if (size < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            if (string.IsNullOrWhiteSpace(sha256))
            {
                throw new ArgumentException("SHA-256 checksum is required.", nameof(sha256));
            }

            FileId = fileId;
            VersionNumber = versionNumber;
            StoragePath = storagePath;
            FileName = fileName;
            ContentType = contentType;
            Size = size;
            Sha256 = sha256.ToLowerInvariant();
            ThumbnailStatus = ProcessingStatus.Pending;
            OcrStatus = ProcessingStatus.Pending;
        }

        public void SoftDelete()
        {
            if (IsDeleted)
            {
                return;
            }

            DeletedAt = DateTime.UtcNow;
            MarkAsModified();
        }

        public bool TryStartThumbnailProcessing()
        {
            if (IsDeleted || ThumbnailStatus is ProcessingStatus.Processing or ProcessingStatus.Completed)
            {
                return false;
            }

            ThumbnailStatus = ProcessingStatus.Processing;
            ThumbnailError = null;
            MarkAsModified();
            return true;
        }

        public void CompleteThumbnail(byte[] thumbnail, string contentType, int width, int height)
        {
            if (ThumbnailStatus == ProcessingStatus.Completed)
            {
                return;
            }

            if (thumbnail == null || thumbnail.Length == 0)
            {
                throw new ArgumentException("Thumbnail bytes are required.", nameof(thumbnail));
            }

            if (string.IsNullOrWhiteSpace(contentType))
            {
                throw new ArgumentException("Thumbnail content type is required.", nameof(contentType));
            }

            if (width <= 0 || height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width), "Thumbnail dimensions must be positive.");
            }

            Thumbnail = thumbnail;
            ThumbnailContentType = contentType;
            ThumbnailWidth = width;
            ThumbnailHeight = height;
            ThumbnailStatus = ProcessingStatus.Completed;
            ThumbnailError = null;
            MarkAsModified();
        }

        public void FailThumbnail(string error)
        {
            ThumbnailStatus = ProcessingStatus.Failed;
            ThumbnailError = TrimError(error);
            MarkAsModified();
        }

        public bool TryStartOcrProcessing()
        {
            if (IsDeleted || OcrStatus is ProcessingStatus.Processing or ProcessingStatus.Completed)
            {
                return false;
            }

            OcrStatus = ProcessingStatus.Processing;
            OcrError = null;
            MarkAsModified();
            return true;
        }

        public void CompleteOcr(string ocrResultJson, string ocrText, string language, int wordCount)
        {
            if (OcrStatus == ProcessingStatus.Completed)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(ocrResultJson))
            {
                throw new ArgumentException("OCR JSON is required.", nameof(ocrResultJson));
            }

            if (string.IsNullOrWhiteSpace(language))
            {
                throw new ArgumentException("OCR language is required.", nameof(language));
            }

            if (wordCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(wordCount));
            }

            OcrResult = ocrResultJson;
            OcrText = ocrText ?? string.Empty;
            OcrLanguage = language;
            OcrWordCount = wordCount;
            OcrStatus = ProcessingStatus.Completed;
            OcrError = null;
            MarkAsModified();
        }

        public void FailOcr(string error)
        {
            OcrStatus = ProcessingStatus.Failed;
            OcrError = TrimError(error);
            MarkAsModified();
        }

        private static string TrimError(string? error)
        {
            if (string.IsNullOrWhiteSpace(error))
            {
                return "Unknown processing error";
            }

            return error.Length <= 512 ? error : error[..512];
        }
    }
}
