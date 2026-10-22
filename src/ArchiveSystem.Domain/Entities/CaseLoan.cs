using System.Text.RegularExpressions;

namespace ArchiveSystem.Domain.Entities
{
    public sealed class CaseLoan : BaseEntity<Guid>
    {
        private static readonly Regex TenDigitsPattern = new("^[0-9]{10}$", RegexOptions.Compiled);

        public string BorrowerFirstName { get; private set; } = string.Empty;
        public string BorrowerLastName { get; private set; } = string.Empty;
        public string CaseNumber { get; private set; } = string.Empty;
        public string NationalId { get; private set; } = string.Empty;
        public string ContactNumber { get; private set; } = string.Empty;
        public DateTime CheckoutDate { get; private set; }
        public DateTime? ExpectedReturnDate { get; private set; }
        public DateTime? ActualReturnDate { get; private set; }
        public string Status { get; private set; } = "Loaned";
        public string? ReceiverUnit { get; private set; }
        public string? RegisteredBy { get; private set; }
        public string? Description { get; private set; }
        public bool IsReturned { get; private set; }
        public bool IsClosed =>
            Status == "Returned" &&
            IsReturned &&
            ActualReturnDate.HasValue;

        private CaseLoan() : base()
        {
            Id = Guid.Empty;
            BorrowerFirstName = string.Empty;
            BorrowerLastName = string.Empty;
            CaseNumber = string.Empty;
            NationalId = string.Empty;
            ContactNumber = string.Empty;
        }

        public CaseLoan(
            Guid id,
            string borrowerFirstName,
            string borrowerLastName,
            string caseNumber,
            string nationalId,
            string contactNumber,
            DateTime checkoutDate,
            DateTime? expectedReturnDate,
            DateTime? actualReturnDate,
            string? description,
            string? status,
            string? receiverUnit,
            string? registeredBy,
            bool isReturned = false) : base(id == Guid.Empty ? Guid.NewGuid() : id)
        {
            ApplyState(
                borrowerFirstName,
                borrowerLastName,
                caseNumber,
                nationalId,
                contactNumber,
                checkoutDate,
                expectedReturnDate,
                actualReturnDate,
                description,
                status,
                receiverUnit,
                registeredBy,
                isReturned);
        }

        public void Update(
            string borrowerFirstName,
            string borrowerLastName,
            string caseNumber,
            string nationalId,
            string contactNumber,
            DateTime checkoutDate,
            DateTime? expectedReturnDate,
            DateTime? actualReturnDate,
            string? description,
            string? status,
            string? receiverUnit,
            string? registeredBy,
            bool isReturned)
        {
            if (IsClosed)
            {
                throw new InvalidOperationException("A returned case loan cannot be modified.");
            }

            ApplyState(
                borrowerFirstName,
                borrowerLastName,
                caseNumber,
                nationalId,
                contactNumber,
                checkoutDate,
                expectedReturnDate,
                actualReturnDate,
                description,
                status,
                receiverUnit,
                registeredBy,
                isReturned);

            MarkAsModified();
        }

        private void ApplyState(
            string borrowerFirstName,
            string borrowerLastName,
            string caseNumber,
            string nationalId,
            string contactNumber,
            DateTime checkoutDate,
            DateTime? expectedReturnDate,
            DateTime? actualReturnDate,
            string? description,
            string? status,
            string? receiverUnit,
            string? registeredBy,
            bool isReturned)
        {
            var normalizedStatus = NormalizeStatus(status, isReturned);

            if (expectedReturnDate.HasValue && expectedReturnDate.Value < checkoutDate)
            {
                throw new ArgumentException("ExpectedReturnDate must be on or after CheckoutDate.", nameof(expectedReturnDate));
            }

            if (actualReturnDate.HasValue && actualReturnDate.Value < checkoutDate)
            {
                throw new ArgumentException("ActualReturnDate must be on or after CheckoutDate.", nameof(actualReturnDate));
            }

            if (normalizedStatus == "Returned" && !actualReturnDate.HasValue)
            {
                throw new ArgumentException(
                    "ActualReturnDate is required when Status is Returned.",
                    nameof(actualReturnDate));
            }

            BorrowerFirstName = RequireText(borrowerFirstName, nameof(borrowerFirstName), 100);
            BorrowerLastName = RequireText(borrowerLastName, nameof(borrowerLastName), 100);
            CaseNumber = RequireText(caseNumber, nameof(caseNumber), 50);
            NationalId = RequireNationalId(nationalId);
            ContactNumber = RequireText(contactNumber, nameof(contactNumber), 20);
            CheckoutDate = checkoutDate;
            ExpectedReturnDate = expectedReturnDate;
            ActualReturnDate = actualReturnDate;
            Status = normalizedStatus;
            ReceiverUnit = NormalizeOptional(receiverUnit, 150);
            RegisteredBy = NormalizeOptional(registeredBy, 150);
            Description = NormalizeOptional(description, 1000);
            IsReturned = Status == "Returned";
        }

        private static string NormalizeStatus(string? status, bool isReturned)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return isReturned ? "Returned" : "Loaned";
            }

            var normalized = status.Trim();
            if (normalized is "Loaned" or "Returned" or "Overdue" or "Cancelled")
            {
                return normalized;
            }

            throw new ArgumentException("Invalid case loan status.", nameof(status));
        }

        private static string RequireNationalId(string? nationalId)
        {
            var normalized = RequireText(nationalId, nameof(nationalId), 10);
            if (!TenDigitsPattern.IsMatch(normalized))
            {
                throw new ArgumentException("NationalId must be exactly 10 digits.", nameof(nationalId));
            }

            return normalized;
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
