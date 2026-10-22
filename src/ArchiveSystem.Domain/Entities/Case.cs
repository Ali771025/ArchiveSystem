// src/ArchiveSystem.Domain/Entities/Case.cs

namespace ArchiveSystem.Domain.Entities
{
    public class Case : BaseEntity<long>, IAggregateRoot
    {
        private static readonly System.Text.RegularExpressions.Regex IranianPostalCodePattern =
            new("^[0-9]{10}$", System.Text.RegularExpressions.RegexOptions.Compiled);

        public string CaseNumber { get; private set; }
        public string? ExternalCaseNumber { get; private set; }
        public string Title { get; private set; }
        public string? AvatarImagePath { get; private set; }
        public long ClassificationId { get; private set; }
        public long? CreatedByUserId { get; private set; }
        public string? CreatedByUserName { get; private set; }
        public string Owners { get; private set; } // JSON array of owner IDs/names
        public string Location { get; private set; }
        public string? PostalCode { get; private set; }
        public DateTime? ClosedAt { get; private set; }
        public bool IsClosed => ClosedAt.HasValue;

        // Soft Delete
        public DateTime? DeletedAt { get; private set; }
        public bool IsDeleted => DeletedAt.HasValue;

        // Navigation properties
        public Classification Classification { get; private set; } = null!;
        public ICollection<ArchiveFile> Files { get; private set; } = new List<ArchiveFile>();
        public ICollection<CaseFieldValueString> DynamicStringValues { get; private set; } = new List<CaseFieldValueString>();
        public ICollection<CaseFieldValueNumber> DynamicNumberValues { get; private set; } = new List<CaseFieldValueNumber>();
        public ICollection<CaseFieldValueDate> DynamicDateValues { get; private set; } = new List<CaseFieldValueDate>();
        public ICollection<CaseFieldValueBool> DynamicBoolValues { get; private set; } = new List<CaseFieldValueBool>();
        public ICollection<CaseFieldValueOption> DynamicOptionValues { get; private set; } = new List<CaseFieldValueOption>();

        // Private parameterless constructor for ORMs
        private Case() : base()
        {
            CaseNumber = string.Empty;
            ExternalCaseNumber = null;
            Title = string.Empty;
            Owners = "[]";
            Location = string.Empty;
        }

        public Case(
            long id,
            string caseNumber,
            string title,
            long classificationId,
            string owners = "[]",
            string location = "",
            string? postalCode = null,
            string? externalCaseNumber = null)
            : base(id)
        {
            ValidateCaseNumber(caseNumber);
            var normalizedExternalCaseNumber = NormalizeOptionalCaseNumber(externalCaseNumber);
            ValidateTitle(title);

            CaseNumber = caseNumber.Trim();
            ExternalCaseNumber = normalizedExternalCaseNumber;
            Title = title;
            ClassificationId = classificationId;
            Owners = owners;
            Location = location;
            PostalCode = NormalizePostalCode(postalCode);
            AvatarImagePath = null;
        }

        public void UpdateTitle(string title)
        {
            ValidateTitle(title);
            Title = title;
            MarkAsModified();
        }

        public void UpdateExternalCaseNumber(string? externalCaseNumber)
        {
            ExternalCaseNumber = NormalizeOptionalCaseNumber(externalCaseNumber);
            MarkAsModified();
        }

        public void MoveToClassification(long classificationId)
        {
            if (classificationId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(classificationId));
            }

            ClassificationId = classificationId;
            MarkAsModified();
        }

        public void SetCreatedBy(long? userId, string? userName)
        {
            CreatedByUserId = userId > 0 ? userId : null;
            CreatedByUserName = string.IsNullOrWhiteSpace(userName) ? null : userName.Trim();
            MarkAsModified();
        }

        public void UpdateOwners(string owners)
        {
            Owners = owners;
            MarkAsModified();
        }

        public void UpdateLocation(string location)
        {
            Location = location;
            MarkAsModified();
        }

        public void UpdatePostalCode(string? postalCode)
        {
            PostalCode = NormalizePostalCode(postalCode);
            MarkAsModified();
        }

        public void UpdateAvatarImagePath(string? avatarImagePath)
        {
            if (string.IsNullOrWhiteSpace(avatarImagePath))
            {
                AvatarImagePath = null;
                MarkAsModified();
                return;
            }

            var normalized = avatarImagePath.Trim();
            if (normalized.Length > 500)
            {
                throw new ArgumentException("Avatar image path cannot exceed 500 characters.", nameof(avatarImagePath));
            }

            AvatarImagePath = normalized;
            MarkAsModified();
        }

        public void CloseCase()
        {
            if (!IsClosed)
            {
                ClosedAt = DateTime.UtcNow;
                MarkAsModified();
            }
        }

        public void ReopenCase()
        {
            if (IsClosed)
            {
                ClosedAt = null;
                MarkAsModified();
            }
        }

        public void AddFile(ArchiveFile file)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            if (IsClosed)
                throw new InvalidOperationException("Cannot add files to a closed case.");

            if (file.CaseId.HasValue && file.CaseId != Id)
                throw new InvalidOperationException("File already belongs to another case.");

            if (!Files.Any(f => f.Id == file.Id))
            {
                Files.Add(file);
                file.AssignToCase(Id);
            }
        }

        public void RemoveFile(ArchiveFile file)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            if (Files.Remove(file))
            {
                file.RemoveFromCase();
            }
        }

        public int GetFileCount() => Files.Count;

        public void SoftDelete()
        {
            DeletedAt = DateTime.UtcNow;
            MarkAsModified();
        }

        public void Restore()
        {
            if (IsDeleted)
            {
                DeletedAt = null;
                MarkAsModified();
            }
        }

        private static void ValidateCaseNumber(string caseNumber)
        {
            if (string.IsNullOrWhiteSpace(caseNumber))
                throw new ArgumentException("Case number cannot be null or empty.", nameof(caseNumber));

            if (caseNumber.Length > 100)
                throw new ArgumentException("Case number cannot exceed 100 characters.", nameof(caseNumber));
        }

        private static string? NormalizeOptionalCaseNumber(string? caseNumber)
        {
            if (string.IsNullOrWhiteSpace(caseNumber))
            {
                return null;
            }

            var normalized = caseNumber.Trim();
            ValidateCaseNumber(normalized);
            return normalized;
        }

        private static void ValidateTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Case title cannot be null or empty.", nameof(title));

            if (title.Length > 500)
                throw new ArgumentException("Case title cannot exceed 500 characters.", nameof(title));
        }

        private static string? NormalizePostalCode(string? postalCode)
        {
            if (string.IsNullOrWhiteSpace(postalCode))
            {
                return null;
            }

            var normalized = postalCode.Trim();
            if (!IranianPostalCodePattern.IsMatch(normalized))
            {
                throw new ArgumentException("Postal code must be exactly 10 digits.", nameof(postalCode));
            }

            return normalized;
        }
    }
}
