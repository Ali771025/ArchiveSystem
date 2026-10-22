using ArchiveSystem.Domain.Enums;
using System.Text.RegularExpressions;
using OwnerTypeEnum = ArchiveSystem.Domain.Enums.OwnerType;

namespace ArchiveSystem.Domain.Entities
{
    public class Owner : BaseEntity<long>
    {
        private static readonly Regex DigitsPattern = new("^[0-9]+$", RegexOptions.Compiled);

        public OwnerTypeEnum OwnerType { get; private set; }

        public string? Mobile { get; private set; }
        public string? PostalCode { get; private set; }
        public string? Address { get; private set; }
        public string? HomePhone { get; private set; }
        public string? TaxpayerId { get; private set; }

        public string? FirstName { get; private set; }
        public string? LastName { get; private set; }
        public string? NationalCode { get; private set; }
        public string? IdentityNumber { get; private set; }

        public string? CompanyName { get; private set; }
        public string? NationalId { get; private set; }
        public string? RegistrationNumber { get; private set; }

        public ICollection<DossierOwner> DossierLinks { get; private set; } = new List<DossierOwner>();

        private Owner() : base()
        {
            OwnerType = OwnerTypeEnum.Natural;
        }

        public Owner(
            long id,
            OwnerTypeEnum ownerType,
            string? mobile = null,
            string? postalCode = null,
            string? address = null,
            string? homePhone = null,
            string? firstName = null,
            string? lastName = null,
            string? nationalCode = null,
            string? identityNumber = null,
            string? companyName = null,
            string? nationalId = null,
            string? registrationNumber = null,
            string? taxpayerId = null) : base(id)
        {
            ApplyState(
                ownerType,
                mobile,
                postalCode,
                address,
                homePhone,
                firstName,
                lastName,
                nationalCode,
                identityNumber,
                companyName,
                nationalId,
                registrationNumber,
                taxpayerId);
        }

        public void Update(
            OwnerTypeEnum ownerType,
            string? mobile = null,
            string? postalCode = null,
            string? address = null,
            string? homePhone = null,
            string? firstName = null,
            string? lastName = null,
            string? nationalCode = null,
            string? identityNumber = null,
            string? companyName = null,
            string? nationalId = null,
            string? registrationNumber = null,
            string? taxpayerId = null)
        {
            ApplyState(
                ownerType,
                mobile,
                postalCode,
                address,
                homePhone,
                firstName,
                lastName,
                nationalCode,
                identityNumber,
                companyName,
                nationalId,
                registrationNumber,
                taxpayerId);

            MarkAsModified();
        }

        public string GetDisplayName()
        {
            if (OwnerType == OwnerTypeEnum.Natural)
            {
                return $"{FirstName} {LastName}".Trim();
            }

            return CompanyName?.Trim() ?? string.Empty;
        }

        public string GetIdentifier()
        {
            return OwnerType == OwnerTypeEnum.Natural
                ? NationalCode?.Trim() ?? string.Empty
                : NationalId?.Trim() ?? string.Empty;
        }

        private void ApplyState(
            OwnerTypeEnum ownerType,
            string? mobile,
            string? postalCode,
            string? address,
            string? homePhone,
            string? firstName,
            string? lastName,
            string? nationalCode,
            string? identityNumber,
            string? companyName,
            string? nationalId,
            string? registrationNumber,
            string? taxpayerId)
        {
            OwnerType = ownerType;
            Mobile = NormalizeOptional(mobile, 20);
            PostalCode = NormalizeOptional(postalCode, 20);
            Address = NormalizeOptional(address, 1000);
            HomePhone = NormalizeOptional(homePhone, 20);
            TaxpayerId = NormalizeTaxpayerId(taxpayerId);

            if (ownerType == OwnerTypeEnum.Natural)
            {
                FirstName = RequireText(firstName, nameof(firstName), 100);
                LastName = RequireText(lastName, nameof(lastName), 100);
                NationalCode = RequireDigits(nationalCode, nameof(nationalCode), 10);
                IdentityNumber = NormalizeOptional(identityNumber, 50);

                CompanyName = null;
                NationalId = null;
                RegistrationNumber = null;
                return;
            }

            CompanyName = RequireText(companyName, nameof(companyName), 200);
            NationalId = RequireDigits(nationalId, nameof(nationalId), 10, 11);
            RegistrationNumber = NormalizeOptional(registrationNumber, 50);

            FirstName = null;
            LastName = null;
            NationalCode = null;
            IdentityNumber = null;
        }

        private static string RequireText(string? value, string paramName, int maxLength)
        {
            var normalized = NormalizeOptional(value, maxLength);
            if (normalized == null)
            {
                throw new ArgumentException($"{paramName} cannot be empty.", paramName);
            }

            return normalized;
        }

        private static string RequireDigits(string? value, string paramName, int exactLength)
        {
            var normalized = RequireText(value, paramName, exactLength);
            if (normalized.Length != exactLength || !DigitsPattern.IsMatch(normalized))
            {
                throw new ArgumentException($"{paramName} must be exactly {exactLength} digits.", paramName);
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

        private static string? NormalizeTaxpayerId(string? value)
        {
            var normalized = NormalizeOptional(value, 14);
            if (normalized == null)
            {
                return null;
            }

            if (!DigitsPattern.IsMatch(normalized) || (normalized.Length != 11 && normalized.Length != 14))
            {
                throw new ArgumentException("TaxpayerId must be 11 or 14 digits.", nameof(value));
            }

            return normalized;
        }

        private static string RequireDigits(string? value, string paramName, int minLength, int maxLength)
        {
            var normalized = RequireText(value, paramName, maxLength);
            if (normalized.Length < minLength || normalized.Length > maxLength || !DigitsPattern.IsMatch(normalized))
            {
                throw new ArgumentException($"{paramName} must be between {minLength} and {maxLength} digits.", paramName);
            }

            return normalized;
        }
    }
}
