using ArchiveSystem.Domain.Enums;
using ArchiveSystem.Domain.Events;

namespace ArchiveSystem.Domain.Entities
{
    public class UploadSession : BaseEntity<Guid>
    {
        public long? FileId { get; private set; }
        public long? CaseId { get; private set; }
        public string TenantId { get; private set; }
        public string OriginalFileName { get; private set; }
        public string ContentType { get; private set; }
        public long ExpectedSizeBytes { get; private set; }
        public string ExpectedSha256 { get; private set; }
        public int PartSizeBytes { get; private set; }
        public string BucketName { get; private set; }
        public string ObjectKey { get; private set; }
        public string MultipartUploadId { get; private set; }
        public UploadSessionStatus Status { get; private set; }
        public string? FailureReason { get; private set; }
        public DateTime? CompletedAt { get; private set; }
        public long? FinalizedVersionId { get; private set; }

        public ArchiveFile? File { get; private set; }
        public ICollection<UploadPart> Parts { get; private set; } = new List<UploadPart>();

        private UploadSession() : base()
        {
            TenantId = string.Empty;
            OriginalFileName = string.Empty;
            ContentType = string.Empty;
            ExpectedSha256 = string.Empty;
            BucketName = string.Empty;
            ObjectKey = string.Empty;
            MultipartUploadId = string.Empty;
        }

        public UploadSession(
            Guid id,
            long? fileId,
            long? caseId,
            string tenantId,
            string originalFileName,
            string contentType,
            long expectedSizeBytes,
            string expectedSha256,
            int partSizeBytes,
            string bucketName,
            string objectKey,
            string multipartUploadId)
            : base(id)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("Tenant ID is required.", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(originalFileName))
                throw new ArgumentException("Original file name is required.", nameof(originalFileName));
            if (string.IsNullOrWhiteSpace(contentType))
                throw new ArgumentException("Content type is required.", nameof(contentType));
            if (expectedSizeBytes <= 0)
                throw new ArgumentOutOfRangeException(nameof(expectedSizeBytes));
            if (string.IsNullOrWhiteSpace(expectedSha256))
                throw new ArgumentException("Expected SHA-256 is required.", nameof(expectedSha256));
            if (partSizeBytes <= 0)
                throw new ArgumentOutOfRangeException(nameof(partSizeBytes));
            if (string.IsNullOrWhiteSpace(bucketName))
                throw new ArgumentException("Bucket is required.", nameof(bucketName));
            if (string.IsNullOrWhiteSpace(objectKey))
                throw new ArgumentException("Object key is required.", nameof(objectKey));
            if (string.IsNullOrWhiteSpace(multipartUploadId))
                throw new ArgumentException("Multipart upload ID is required.", nameof(multipartUploadId));

            FileId = fileId;
            CaseId = caseId;
            TenantId = tenantId;
            OriginalFileName = originalFileName;
            ContentType = contentType;
            ExpectedSizeBytes = expectedSizeBytes;
            ExpectedSha256 = expectedSha256.ToLowerInvariant();
            PartSizeBytes = partSizeBytes;
            BucketName = bucketName;
            ObjectKey = objectKey;
            MultipartUploadId = multipartUploadId;
            Status = UploadSessionStatus.Initiated;
        }

        public void MarkUploading()
        {
            if (Status == UploadSessionStatus.Completed || Status == UploadSessionStatus.Aborted)
                return;

            Status = UploadSessionStatus.Uploading;
            FailureReason = null;
            MarkAsModified();
        }

        public void MarkFinalizing()
        {
            if (Status == UploadSessionStatus.Completed || Status == UploadSessionStatus.Aborted)
                return;

            Status = UploadSessionStatus.Finalizing;
            FailureReason = null;
            MarkAsModified();
        }

        public void MarkCompleted(long finalizedVersionId)
        {
            if (Status == UploadSessionStatus.Completed)
                return;

            Status = UploadSessionStatus.Completed;
            FinalizedVersionId = finalizedVersionId;
            CompletedAt = DateTime.UtcNow;
            FailureReason = null;
            MarkAsModified();

            AddDomainEvent(new UploadFinalizedEvent(Id, FileId, finalizedVersionId, ObjectKey));
        }

        public void MarkFailed(string reason)
        {
            Status = UploadSessionStatus.Failed;
            FailureReason = reason;
            MarkAsModified();
        }

        public void MarkAborted(string? reason = null)
        {
            Status = UploadSessionStatus.Aborted;
            FailureReason = reason;
            MarkAsModified();
        }
    }
}
