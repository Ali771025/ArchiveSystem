// src/ArchiveSystem.Domain/Entities/File.cs
using System.Text.Json;
using ArchiveSystem.Domain.Enums;
using ArchiveSystem.Domain.ValueObjects;

namespace ArchiveSystem.Domain.Entities
{
    public class ArchiveFile : BaseEntity<long>, IAggregateRoot
    {
        public string Name { get; private set; }
        public FileSize Size { get; private set; } = null!;
        public FileExtension Extension { get; private set; } = null!;
        public string ContentType { get; private set; }
        public string StoragePath { get; private set; }
        public FileStatus Status { get; private set; }
        public long? CaseId { get; private set; }
        public DateTime? DeletedAt { get; private set; }
        public string? Hash { get; private set; }
        public long? LatestVersionId { get; private set; }
        
        // Tags (JSON array)
        public string Tags { get; private set; } = "[]";
        
        // Soft Delete
        public bool IsDeleted => DeletedAt.HasValue;

        // Navigation property
        public Case? Case { get; private set; }
        public ICollection<FileVersion> Versions { get; private set; } = new List<FileVersion>();
        public ICollection<FileTag> FileTags { get; private set; } = new List<FileTag>();

        // Private parameterless constructor for ORMs
        private ArchiveFile() : base()
        {
            Name = string.Empty;
            ContentType = string.Empty;
            StoragePath = string.Empty;
            Tags = "[]";
        }

        public ArchiveFile(long id, string name, FileSize size, FileExtension extension, string contentType, string storagePath, long? caseId = null)
            : base(id)
        {
            ValidateName(name);
            ValidateStoragePath(storagePath);

            Name = name;
            Size = size ?? throw new ArgumentNullException(nameof(size));
            Extension = extension ?? throw new ArgumentNullException(nameof(extension));
            ContentType = contentType ?? throw new ArgumentNullException(nameof(contentType));
            StoragePath = storagePath;
            Status = FileStatus.Pending;
            CaseId = caseId;
            Tags = "[]";
        }

        public void UpdateStatus(FileStatus status)
        {
            Status = status;
            MarkAsModified();
        }

        public void AssignToCase(long caseId)
        {
            CaseId = caseId;
            MarkAsModified();
        }

        public void RemoveFromCase()
        {
            CaseId = null;
            MarkAsModified();
        }

        public void SoftDelete()
        {
            Status = FileStatus.Deleted;
            DeletedAt = DateTime.UtcNow;
            MarkAsModified();
        }

        public void Restore()
        {
            if (Status == FileStatus.Deleted)
            {
                Status = FileStatus.Ready;
                DeletedAt = null;
                MarkAsModified();
            }
        }

        public bool IsValidExtension(params string[] allowedExtensions)
        {
            var normalizedExtensions = allowedExtensions.Select(ext => ext.StartsWith(".") ? ext.ToLowerInvariant() : $".{ext}".ToLowerInvariant());
            return normalizedExtensions.Contains(Extension.Value);
        }

        public void SetHash(string hash)
        {
            if (string.IsNullOrWhiteSpace(hash))
                throw new ArgumentException("Hash cannot be null or empty.", nameof(hash));

            Hash = hash;
            MarkAsModified();
        }

        public void ApplyLatestVersionMetadata(long latestVersionId, string objectKey, long sizeBytes, string contentType, string sha256Checksum)
        {
            if (latestVersionId <= 0)
                throw new ArgumentOutOfRangeException(nameof(latestVersionId), "Latest version ID must be positive.");

            if (string.IsNullOrWhiteSpace(objectKey))
                throw new ArgumentException("Object key cannot be null or empty.", nameof(objectKey));

            if (sizeBytes < 0)
                throw new ArgumentOutOfRangeException(nameof(sizeBytes), "File size cannot be negative.");

            if (string.IsNullOrWhiteSpace(contentType))
                throw new ArgumentException("Content type cannot be null or empty.", nameof(contentType));

            if (string.IsNullOrWhiteSpace(sha256Checksum))
                throw new ArgumentException("SHA-256 checksum cannot be null or empty.", nameof(sha256Checksum));

            LatestVersionId = latestVersionId;
            StoragePath = objectKey;
            Size = FileSize.Create(sizeBytes);
            ContentType = contentType;
            Hash = sha256Checksum;
            Status = FileStatus.Pending;
            MarkAsModified();
        }

        public void AddTag(string tag)
        {
            var tags = JsonSerializer.Deserialize<List<string>>(Tags) ?? new List<string>();
            if (!tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
            {
                tags.Add(tag);
                Tags = JsonSerializer.Serialize(tags);
                MarkAsModified();
            }
        }

        public void RemoveTag(string tag)
        {
            var tags = JsonSerializer.Deserialize<List<string>>(Tags) ?? new List<string>();
            if (tags.Remove(tag))
            {
                Tags = JsonSerializer.Serialize(tags);
                MarkAsModified();
            }
        }

        public void SetTags(IEnumerable<string> tags)
        {
            var tagList = tags.ToList();
            Tags = JsonSerializer.Serialize(tagList);
            MarkAsModified();
        }

        private static void ValidateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("File name cannot be null or empty.", nameof(name));

            if (name.Length > 255)
                throw new ArgumentException("File name cannot exceed 255 characters.", nameof(name));
        }

        private static void ValidateStoragePath(string storagePath)
        {
            if (string.IsNullOrWhiteSpace(storagePath))
                throw new ArgumentException("Storage path cannot be null or empty.", nameof(storagePath));

            if (storagePath.Length > 1000)
                throw new ArgumentException("Storage path cannot exceed 1000 characters.", nameof(storagePath));
        }
    }
}
