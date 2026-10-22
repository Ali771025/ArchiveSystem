using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace ArchiveSystem.ScannerBridge.Services;

public enum ScannerDriver
{
    Twain,
    Wia
}

public static class ScannerDeviceKey
{
    public static string Encode(ScannerDriver driver, string deviceName)
    {
        var encodedName = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(deviceName));
        return $"{driver.ToString().ToLowerInvariant()}:{encodedName}";
    }

    public static (ScannerDriver Driver, string DeviceName) Decode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ScannerBridgeException("scanner_required", "ابتدا یک اسکنر انتخاب کنید.");
        }

        var separatorIndex = value.IndexOf(':');
        if (separatorIndex <= 0 || separatorIndex == value.Length - 1)
        {
            throw InvalidScannerId();
        }

        if (!Enum.TryParse<ScannerDriver>(value[..separatorIndex], true, out var driver))
        {
            throw new ScannerBridgeException("unsupported_driver", "درایور انتخاب‌شده پشتیبانی نمی‌شود.");
        }

        try
        {
            var deviceName = Encoding.UTF8.GetString(
                WebEncoders.Base64UrlDecode(value[(separatorIndex + 1)..]));
            if (string.IsNullOrWhiteSpace(deviceName))
            {
                throw new FormatException();
            }

            return (driver, deviceName);
        }
        catch (FormatException)
        {
            throw InvalidScannerId();
        }
    }

    private static ScannerBridgeException InvalidScannerId() =>
        new("invalid_scanner_id", "شناسه اسکنر معتبر نیست.");
}
