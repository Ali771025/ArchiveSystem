using ArchiveSystem.Application.Features.Taxpayers.Commands.CreateTaxpayer;
using ArchiveSystem.Application.Features.Taxpayers.Commands.UpdateTaxpayer;
using ArchiveSystem.Domain.Enums;
using Xunit;

namespace ArchiveSystem.Application.Tests.Taxpayers;

public sealed class TaxpayerNationalCompanyIdValidatorTests
{
    [Theory]
    [InlineData("10101234567", true)]
    [InlineData("1010123456", false)]
    [InlineData("101012345678", false)]
    public void CreateValidator_RequiresExactlyElevenDigits(string nationalCompanyId, bool expectedValid)
    {
        var command = new CreateTaxpayerCommand(
            TaxpayerType.LegalEntity,
            FirstName: null,
            LastName: null,
            NationalId: null,
            CompanyName: "شرکت آزمون",
            NationalCompanyId: nationalCompanyId,
            IdentityNumber: null,
            Mobile: null,
            LandlinePhone: null,
            PostalCode: null,
            Address: null);

        var result = new CreateTaxpayerCommandValidator().Validate(command);

        Assert.Equal(expectedValid, result.IsValid);
    }

    [Theory]
    [InlineData("10101234567", true)]
    [InlineData("1010123456", false)]
    [InlineData("101012345678", false)]
    public void UpdateValidator_RequiresExactlyElevenDigits(string nationalCompanyId, bool expectedValid)
    {
        var command = new UpdateTaxpayerCommand(
            Guid.NewGuid(),
            TaxpayerType.LegalEntity,
            FirstName: null,
            LastName: null,
            NationalId: null,
            CompanyName: "شرکت آزمون",
            NationalCompanyId: nationalCompanyId,
            IdentityNumber: null,
            Mobile: null,
            LandlinePhone: null,
            PostalCode: null,
            Address: null);

        var result = new UpdateTaxpayerCommandValidator().Validate(command);

        Assert.Equal(expectedValid, result.IsValid);
    }
}
