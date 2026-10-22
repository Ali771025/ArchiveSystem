namespace ArchiveSystem.Domain.Entities
{
    public class UploadPart : BaseEntity<long>
    {
        public Guid SessionId { get; private set; }
        public int PartNumber { get; private set; }
        public string ETag { get; private set; }
        public long SizeBytes { get; private set; }
        public DateTime UploadedAt { get; private set; }

        public UploadSession Session { get; private set; } = null!;

        private UploadPart() : base()
        {
            ETag = string.Empty;
            UploadedAt = DateTime.UtcNow;
        }

        public UploadPart(long id, Guid sessionId, int partNumber, string eTag, long sizeBytes)
            : base(id)
        {
            if (sessionId == Guid.Empty)
                throw new ArgumentException("Session ID is required.", nameof(sessionId));
            if (partNumber <= 0)
                throw new ArgumentOutOfRangeException(nameof(partNumber));
            if (string.IsNullOrWhiteSpace(eTag))
                throw new ArgumentException("ETag is required.", nameof(eTag));
            if (sizeBytes <= 0)
                throw new ArgumentOutOfRangeException(nameof(sizeBytes));

            SessionId = sessionId;
            PartNumber = partNumber;
            ETag = eTag;
            SizeBytes = sizeBytes;
            UploadedAt = DateTime.UtcNow;
        }

        public void Replace(string eTag, long sizeBytes)
        {
            if (string.IsNullOrWhiteSpace(eTag))
                throw new ArgumentException("ETag is required.", nameof(eTag));
            if (sizeBytes <= 0)
                throw new ArgumentOutOfRangeException(nameof(sizeBytes));

            ETag = eTag;
            SizeBytes = sizeBytes;
            UploadedAt = DateTime.UtcNow;
            MarkAsModified();
        }
    }
}
