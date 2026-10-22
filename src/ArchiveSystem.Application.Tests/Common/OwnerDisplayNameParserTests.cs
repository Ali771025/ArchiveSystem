using ArchiveSystem.Application.Common;
using Xunit;

namespace ArchiveSystem.Application.Tests.Common;

public sealed class OwnerDisplayNameParserTests
{
    [Fact]
    public void ParseNames_ExtractsPersonAndCompanyNamesFromJson()
    {
        const string json = """
            [
              { "FullName": "علی رضایی" },
              { "firstName": "مریم", "lastName": "احمدی" },
              { "companyName": "شرکت نمونه" }
            ]
            """;

        var result = OwnerDisplayNameParser.ParseNames(json);

        Assert.Equal(new[] { "علی رضایی", "مریم احمدی", "شرکت نمونه" }, result);
    }

    [Fact]
    public void ParseNames_PreservesLegacyPlainText()
    {
        var result = OwnerDisplayNameParser.ParseNames("مالک قدیمی");

        Assert.Equal(new[] { "مالک قدیمی" }, result);
    }
}
