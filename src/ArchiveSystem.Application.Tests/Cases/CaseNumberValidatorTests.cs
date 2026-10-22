using ArchiveSystem.Application.Features.Cases.Commands.CreateCase;
using ArchiveSystem.Application.Features.Cases.Commands.UpdateCase;
using ArchiveSystem.Application.Features.Cases.Services;
using System.Globalization;
using Xunit;

namespace ArchiveSystem.Application.Tests.Cases;

public class CaseNumberValidatorTests
{
    [Theory]
    [InlineData("test%0d%0aX-Test:123")]
    [InlineData("A-100\r\nX-Test:123")]
    [InlineData("A 100")]
    [InlineData("9999/99/99")]
    public void CreateCaseValidator_rejects_invalid_case_number_characters(string caseNumber)
    {
        var validator = new CreateCaseCommandValidator();

        var result = validator.Validate(new CreateCaseCommand
        {
            ClassificationId = 1,
            ExternalCaseNumber = caseNumber,
            Address = "تهران"
        });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateCaseCommand.ExternalCaseNumber));
    }

    [Fact]
    public void CreateCaseValidator_accepts_allowed_case_number_format()
    {
        var validator = new CreateCaseCommandValidator();

        var result = validator.Validate(new CreateCaseCommand
        {
            ClassificationId = 1,
            ExternalCaseNumber = "A-1404/001",
            Address = "تهران"
        });

        Assert.DoesNotContain(result.Errors, error => error.PropertyName == nameof(CreateCaseCommand.ExternalCaseNumber));
    }

    [Fact]
    public void CreateCaseValidator_accepts_external_case_number_format()
    {
        var validator = new CreateCaseCommandValidator();

        var result = validator.Validate(new CreateCaseCommand
        {
            ClassificationId = 1,
            ExternalCaseNumber = "EXT-1000",
            Address = "تهران"
        });

        Assert.DoesNotContain(result.Errors, error => error.PropertyName == nameof(CreateCaseCommand.ExternalCaseNumber));
    }

    [Fact]
    public void CreateCaseValidator_accepts_missing_external_case_number()
    {
        var validator = new CreateCaseCommandValidator();

        var result = validator.Validate(new CreateCaseCommand
        {
            ClassificationId = 1,
            ExternalCaseNumber = null,
            Address = "تهران"
        });

        Assert.DoesNotContain(result.Errors, error => error.PropertyName == nameof(CreateCaseCommand.ExternalCaseNumber));
    }

    [Theory]
    [InlineData(1, "1405050300001")]
    [InlineData(99999, "1405050399999")]
    public void PersianCaseNumberFormat_appends_five_digit_daily_serial(int serial, string expected)
    {
        Assert.Equal(expected, PersianCaseNumberFormat.Format("14050503", serial));
    }

    [Fact]
    public void PersianCaseNumberFormat_uses_persian_calendar_date()
    {
        var calendar = new PersianCalendar();
        var date = calendar.ToDateTime(1405, 5, 3, 0, 0, 0, 0);

        Assert.Equal("14050503", PersianCaseNumberFormat.GetDateKey(date));
    }

    [Fact]
    public void UpdateCaseValidator_rejects_invalid_case_number_characters()
    {
        var validator = new UpdateCaseCommandValidator();

        var result = validator.Validate(new UpdateCaseCommand
        {
            CaseId = 1,
            ExternalCaseNumber = "test%0d%0aX-Test:123",
            Address = "تهران"
        });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateCaseCommand.ExternalCaseNumber));
    }
}
