using System.Text.RegularExpressions;
using ArchiveSystem.Domain.Enums;
using ArchiveSystem.Domain.Interfaces;

namespace ArchiveSystem.Domain.Entities
{
    public class Taxpayer : BaseEntity<Guid>, ISoftDeletable
    {
        private static readonly Regex TenDigitsPattern = new("^[0-9]{10}$", RegexOptions.Compiled);

        public TaxpayerType TaxpayerType { get; private set; }
        public string? FirstName { get; private set; }
        public string? LastName { get; private set; }
        public string? NationalId { get; private set; }
        public string? CompanyName { get; private set; }
        public string? NationalCompanyId { get; private set; }
        public string? IdentityNumber { get; private set; }
        public string? Mobile { get; private set; }
        public string? LandlinePhone { get; private set; }
        public string? PostalCode { get; private set; }
        public string? Address { get; private set; }
        public DateTime? DeletedAt { get; private set; }
        public bool IsDeleted => DeletedAt.HasValue;

        private Taxpayer() : base()
        {
            Id = Guid.Empty;
            FirstName = string.Empty;
            LastName = string.Empty;
            NationalId = string.Empty;
        }

        public Taxpayer(
            Guid id,
            TaxpayerType taxpayerType,
            string? firstName,
            string? lastName,
            string? nationalId,
            string? companyName = null,
            string? nationalCompanyId = null,
            string? identityNumber = null,
            string? mobile = null,
            string? landlinePhone = null,
            string? postalCode = null,
            string? address = null) : base(id == Guid.Empty ? Guid.NewGuid() : id)
        {
            ApplyState(
                taxpayerType,
                firstName,
                lastName,
                nationalId,
                companyName,
                nationalCompanyId,
                identityNumber,
                mobile,
                landlinePhone,
                postalCode,
                address);
        }

        public void Update(
            TaxpayerType taxpayerType,
            string? firstName,
            string? lastName,
            string? nationalId,
            string? companyName = null,
            string? nationalCompanyId = null,
            string? identityNumber = null,
            string? mobile = null,
            string? landlinePhone = null,
            string? postalCode = null,
            string? address = null)
        {
            ApplyState(
                taxpayerType,
                firstName,
                lastName,
                nationalId,
                companyName,
                nationalCompanyId,
                identityNumber,
                mobile,
                landlinePhone,
                postalCode,
                address);

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

        private void ApplyState(
            TaxpayerType taxpayerType,
            string? firstName,
            string? lastName,
            string? nationalId,
            string? companyName,
            string? nationalCompanyId,
            string? identityNumber,
            string? mobile,
            string? landlinePhone,
            string? postalCode,
            string? address)
        {
            if (!Enum.IsDefined(typeof(TaxpayerType), taxpayerType))
            {
                throw new ArgumentOutOfRangeException(nameof(taxpayerType), "Taxpayer type is invalid.");
            }

            TaxpayerType = taxpayerType;
            if (taxpayerType == TaxpayerType.NaturalPerson)
            {
                FirstName = RequireText(firstName, nameof(firstName), 100);
                LastName = RequireText(lastName, nameof(lastName), 100);
                NationalId = RequireNationalIdentifier(nationalId, nameof(nationalId));
                IdentityNumber = NormalizeOptional(identityNumber, 50);
                CompanyName = null;
                NationalCompanyId = null;
            }
            else
            {
                CompanyName = RequireText(companyName, nameof(companyName), 100);
                NationalCompanyId = RequireNationalIdentifier(nationalCompanyId, nameof(nationalCompanyId));
                FirstName = null;
                LastName = null;
                NationalId = null;
                IdentityNumber = null;
            }

            Mobile = NormalizeOptional(mobile, 20);
            LandlinePhone = NormalizeOptional(landlinePhone, 20);
            PostalCode = NormalizeOptional(postalCode, 10);
            Address = NormalizeOptional(address, 1000);
        }

        private static string RequireText(string? value, string paramName, int maxLength)
        {
            var normalized = NormalizeOptional(value, maxLength);
            if (normalized is null)
            {
                throw new ArgumentException($"{paramName} cannot be empty.", paramName);
            }

            return normalized;
        }

        private static string RequireNationalIdentifier(string? nationalId, string paramName)
        {
            var normalized = RequireText(nationalId, paramName, 10);
            if (!TenDigitsPattern.IsMatch(normalized))
            {
                throw new ArgumentException("National identifier must be exactly 10 digits.", paramName);
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
                throw new ArgumentException($"Value must be {maxLength} characters or less.");
            }

            return normalized;
        }
    }
}
