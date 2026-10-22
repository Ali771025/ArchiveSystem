using System.Globalization;

namespace ArchiveSystem.Application.Features.Cases.Services;

public static class PersianCaseNumberFormat
{
    private static readonly PersianCalendar Calendar = new();

    public static string GetDateKey(DateTime localDate)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{Calendar.GetYear(localDate):0000}{Calendar.GetMonth(localDate):00}{Calendar.GetDayOfMonth(localDate):00}");
    }

    public static string Format(string dateKey, int serial)
    {
        if (dateKey.Length != 8 || !dateKey.All(char.IsAsciiDigit))
        {
            throw new ArgumentException("Date key must contain exactly 8 digits.", nameof(dateKey));
        }

        if (serial is < 1 or > 99_999)
        {
            throw new ArgumentOutOfRangeException(nameof(serial), "Daily case serial must be between 1 and 99999.");
        }

        return string.Create(CultureInfo.InvariantCulture, $"{dateKey}{serial:00000}");
    }
}
