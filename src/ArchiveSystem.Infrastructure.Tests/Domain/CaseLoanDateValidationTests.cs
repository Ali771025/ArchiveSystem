using ArchiveSystem.Domain.Entities;
using Xunit;

namespace ArchiveSystem.Infrastructure.Tests.Domain
{
    public sealed class CaseLoanDateValidationTests
    {
        [Fact]
        public void Constructor_AllowsExpectedReturnDateEqualToCheckoutDate()
        {
            var checkoutDate = new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc);

            var loan = new CaseLoan(
                Guid.NewGuid(),
                "Ali",
                "Ahmadi",
                "CASE-100",
                "1234567890",
                "09120000000",
                checkoutDate,
                checkoutDate,
                actualReturnDate: null,
                description: null,
                status: null,
                receiverUnit: null,
                registeredBy: null);

            Assert.Equal(checkoutDate, loan.CheckoutDate);
            Assert.Equal(checkoutDate, loan.ExpectedReturnDate);
        }

        [Fact]
        public void Constructor_RejectsExpectedReturnDateBeforeCheckoutDate()
        {
            var checkoutDate = new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc);

            var exception = Assert.Throws<ArgumentException>(() => new CaseLoan(
                Guid.NewGuid(),
                "Ali",
                "Ahmadi",
                "CASE-100",
                "1234567890",
                "09120000000",
                checkoutDate,
                checkoutDate.AddDays(-1),
                actualReturnDate: null,
                description: null,
                status: null,
                receiverUnit: null,
                registeredBy: null));

            Assert.Equal("expectedReturnDate", exception.ParamName);
        }

        [Fact]
        public void Constructor_RequiresActualReturnDateForReturnedStatus()
        {
            var checkoutDate = new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc);

            var exception = Assert.Throws<ArgumentException>(() => new CaseLoan(
                Guid.NewGuid(),
                "Ali",
                "Ahmadi",
                "CASE-100",
                "1234567890",
                "09120000000",
                checkoutDate,
                checkoutDate.AddDays(7),
                actualReturnDate: null,
                description: null,
                status: "Returned",
                receiverUnit: null,
                registeredBy: null,
                isReturned: true));

            Assert.Equal("actualReturnDate", exception.ParamName);
        }

        [Fact]
        public void Update_RejectsChangesAfterLoanIsClosed()
        {
            var checkoutDate = new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc);
            var loan = new CaseLoan(
                Guid.NewGuid(),
                "Ali",
                "Ahmadi",
                "CASE-100",
                "1234567890",
                "09120000000",
                checkoutDate,
                checkoutDate.AddDays(7),
                actualReturnDate: checkoutDate.AddDays(3),
                description: null,
                status: "Returned",
                receiverUnit: null,
                registeredBy: null,
                isReturned: true);

            Assert.True(loan.IsClosed);
            Assert.Throws<InvalidOperationException>(() => loan.Update(
                "Reza",
                "Ahmadi",
                "CASE-100",
                "1234567890",
                "09120000000",
                checkoutDate,
                checkoutDate.AddDays(7),
                actualReturnDate: checkoutDate.AddDays(3),
                description: null,
                status: "Returned",
                receiverUnit: null,
                registeredBy: null,
                isReturned: true));
        }
    }
}
