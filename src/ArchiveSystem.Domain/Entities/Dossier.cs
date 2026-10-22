using ArchiveSystem.Domain.Enums;
using ArchiveSystem.Domain.Interfaces;
using System.Text.RegularExpressions;

namespace ArchiveSystem.Domain.Entities
{
    public class Dossier : BaseEntity<long>, IAggregateRoot, ISoftDeletable
    {
        private static readonly Regex IranianPostalCodePattern = new("^[0-9]{10}$", RegexOptions.Compiled);

        public string DossierNumber { get; private set; }
        public string ExternalCaseNumber { get; private set; }
        public string Address { get; private set; }
        public string? DefaultImagePath { get; private set; }
        public string? RenovationCode { get; private set; }
        public string? PostalCode { get; private set; }
        public string? RegistrationPlate { get; private set; }
        public DossierUsageType UsageType { get; private set; }
        public decimal? Latitude { get; private set; }
        public decimal? Longitude { get; private set; }
        public long ClassificationId { get; private set; }

        public DateTime? DeletedAt { get; private set; }
        public bool IsDeleted => DeletedAt.HasValue;

        public Classification Classification { get; private set; } = null!;
        public ICollection<DossierOwner> Owners { get; private set; } = new List<DossierOwner>();
        public ICollection<DossierDynamicFieldValue> DynamicFieldValues { get; private set; } = new List<DossierDynamicFieldValue>();

        private Dossier() : base()
        {
            DossierNumber = string.Empty;
            ExternalCaseNumber = string.Empty;
            Address = string.Empty;
        }

        public Dossier(
            long id,
            long classificationId,
            string dossierNumber,
            string externalCaseNumber,
            string address,
            DossierUsageType usageType,
            decimal? latitude = null,
            decimal? longitude = null,
            string? defaultImagePath = null,
            string? renovationCode = null,
            string? postalCode = null,
            string? registrationPlate = null) : base(id)
        {
            if (classificationId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(classificationId), "ClassificationId must be greater than zero.");
            }

            ClassificationId = classificationId;
            DossierNumber = RequireText(dossierNumber, nameof(dossierNumber), 100);
            ExternalCaseNumber = RequireText(externalCaseNumber, nameof(externalCaseNumber), 100);
            Address = RequireText(address, nameof(address), 500);
            UsageType = usageType;
            Latitude = latitude;
            Longitude = longitude;
            DefaultImagePath = NormalizeOptional(defaultImagePath, 500);
            RenovationCode = NormalizeOptional(renovationCode, 100);
            PostalCode = NormalizePostalCode(postalCode);
            RegistrationPlate = NormalizeOptional(registrationPlate, 50);
        }

        public void UpdateStaticFields(
            string dossierNumber,
            string externalCaseNumber,
            string address,
            DossierUsageType usageType,
            decimal? latitude,
            decimal? longitude,
            string? defaultImagePath,
            string? renovationCode,
            string? postalCode,
            string? registrationPlate)
        {
            DossierNumber = RequireText(dossierNumber, nameof(dossierNumber), 100);
            ExternalCaseNumber = RequireText(externalCaseNumber, nameof(externalCaseNumber), 100);
            Address = RequireText(address, nameof(address), 500);
            UsageType = usageType;
            Latitude = latitude;
            Longitude = longitude;
            DefaultImagePath = NormalizeOptional(defaultImagePath, 500);
            RenovationCode = NormalizeOptional(renovationCode, 100);
            PostalCode = NormalizePostalCode(postalCode);
            RegistrationPlate = NormalizeOptional(registrationPlate, 50);
            MarkAsModified();
        }

        public void ChangeClassification(long classificationId)
        {
            if (classificationId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(classificationId), "ClassificationId must be greater than zero.");
            }

            ClassificationId = classificationId;
            MarkAsModified();
        }

        public void AddOwner(long ownerId)
        {
            if (ownerId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(ownerId), "OwnerId must be greater than zero.");
            }

            var existing = Owners.FirstOrDefault(x => x.OwnerId == ownerId);
            if (existing != null)
            {
                if (!existing.IsDeleted)
                {
                    throw new InvalidOperationException($"Owner with id '{ownerId}' already exists in this dossier.");
                }

                existing.Restore();
                MarkAsModified();
                return;
            }

            Owners.Add(new DossierOwner(0, Id, ownerId));
            MarkAsModified();
        }

        public void RemoveOwner(long ownerId)
        {
            var owner = Owners.FirstOrDefault(x => x.OwnerId == ownerId && x.IsDeleted == false);
            if (owner == null)
            {
                return;
            }

            owner.SoftDelete();
            MarkAsModified();
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

        public void Restore()
        {
            if (!IsDeleted)
            {
                return;
            }

            DeletedAt = null;
            MarkAsModified();
        }

        private static string RequireText(string? value, string paramName, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"{paramName} cannot be empty.", paramName);
            }

            var normalized = value.Trim();
            if (normalized.Length > maxLength)
            {
                throw new ArgumentException($"{paramName} must be {maxLength} characters or less.", paramName);
            }

            return normalized;
        }

        private static string? NormalizeOptional(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var normalized = value.Trim();
            if (normalized.Length > maxLength)
            {
                throw new ArgumentException($"Value must be {maxLength} characters or less.", nameof(value));
            }

            return normalized;
        }

        private static string? NormalizePostalCode(string? postalCode)
        {
            var normalized = NormalizeOptional(postalCode, 10);
            if (normalized == null)
            {
                return null;
            }

            if (!IranianPostalCodePattern.IsMatch(normalized))
            {
                throw new ArgumentException("Postal code must be exactly 10 digits.", nameof(postalCode));
            }

            return normalized;
        }
    }
}
