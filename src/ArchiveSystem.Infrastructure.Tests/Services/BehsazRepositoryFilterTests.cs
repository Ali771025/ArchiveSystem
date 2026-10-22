using System.Data;
using System.Reflection;
using ArchiveSystem.Domain.Enums;
using ArchiveSystem.Domain.Integrations;
using ArchiveSystem.Infrastructure.Services.Behsaz;
using Xunit;

namespace ArchiveSystem.Infrastructure.Tests.Services;

public sealed class BehsazRepositoryFilterTests
{
    [Fact]
    public void BuildPeoplePropertiesTable_AppliesSingleRealTaxpayerFilter()
    {
        var table = BuildPeoplePropertiesTable(new BehsazSearchCriteria
        {
            TaxpayerGroup = TaxpayerGroup.Real,
            FirstName = "علی"
        });

        var row = Assert.Single(table.Rows.Cast<DataRow>());
        Assert.Equal((int)TaxpayerGroup.Real, row["TBASPeopleGroupID"]);
        Assert.Equal("علی", row["Name"]);
        Assert.Equal(DBNull.Value, row["Family"]);
        Assert.Equal(DBNull.Value, row["NationalCode"]);
    }

    [Theory]
    [InlineData("FirstName", "علی", "Name")]
    [InlineData("LastName", "احمدی", "Family")]
    [InlineData("NationalCode", "4113695181", "NationalCode")]
    public void BuildPeoplePropertiesTable_AppliesEachRealTaxpayerFilterIndependently(
        string propertyName,
        string value,
        string expectedColumn)
    {
        var criteria = new BehsazSearchCriteria
        {
            TaxpayerGroup = TaxpayerGroup.Real
        };
        typeof(BehsazSearchCriteria).GetProperty(propertyName)!.SetValue(criteria, value);

        var row = Assert.Single(BuildPeoplePropertiesTable(criteria).Rows.Cast<DataRow>());

        Assert.Equal((int)TaxpayerGroup.Real, row["TBASPeopleGroupID"]);
        Assert.Equal(value, row[expectedColumn]);
    }

    [Fact]
    public void BuildPeoplePropertiesTable_AppliesSingleLegalNationalIdFilter()
    {
        var table = BuildPeoplePropertiesTable(new BehsazSearchCriteria
        {
            NationalCompanyId = "4113695181"
        });

        var row = Assert.Single(table.Rows.Cast<DataRow>());
        Assert.Equal((int)TaxpayerGroup.Legal, row["TBASPeopleGroupID"]);
        Assert.Equal("4113695181", row["NationalID"]);
        Assert.Equal(DBNull.Value, row["OrganizationName"]);
        Assert.Equal(DBNull.Value, row["RegisterNumber"]);
    }

    [Fact]
    public void ResultMapping_DoesNotOverwriteFullTaxpayerDetailsWithPeopleKey()
    {
        var resolveMethod = typeof(BehsazRepository).GetMethod(
            "ResolveResultProperty",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(resolveMethod);

        var detailsProperty = Assert.IsAssignableFrom<PropertyInfo>(
            resolveMethod!.Invoke(null, new object[] { typeof(BehsazSearchResultDto), "PeopleProperty" }));
        var keyProperty = Assert.IsAssignableFrom<PropertyInfo>(
            resolveMethod.Invoke(null, new object[] { typeof(BehsazSearchResultDto), "PeopleKey" }));

        Assert.Equal(nameof(BehsazSearchResultDto.TaxpayerDetails), detailsProperty.Name);
        Assert.Equal(nameof(BehsazSearchResultDto.TaxpayerKey), keyProperty.Name);
    }

    private static DataTable BuildPeoplePropertiesTable(BehsazSearchCriteria criteria)
    {
        var method = typeof(BehsazRepository).GetMethod(
            "BuildPeoplePropertiesTable",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        return Assert.IsType<DataTable>(method!.Invoke(null, new object[] { criteria }));
    }
}
