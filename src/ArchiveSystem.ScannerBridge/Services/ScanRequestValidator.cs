using ArchiveSystem.ScannerBridge.Models;

namespace ArchiveSystem.ScannerBridge.Services;

public static class ScanRequestValidator
{
    private static readonly HashSet<int> SupportedDpi = [150, 200, 300, 600];
    private static readonly HashSet<string> SupportedColorModes = new(StringComparer.OrdinalIgnoreCase)
    {
        "color", "grayscale", "black-and-white"
    };
    private static readonly HashSet<string> SupportedPageSizes = new(StringComparer.OrdinalIgnoreCase)
    {
        "a4", "auto"
    };
    private static readonly HashSet<string> SupportedPaperSources = new(StringComparer.OrdinalIgnoreCase)
    {
        "auto", "flatbed", "feeder", "duplex"
    };

    public static void Validate(ScanRequest request)
    {
        _ = ScannerDeviceKey.Decode(request.ScannerId);

        if (!SupportedDpi.Contains(request.Dpi))
        {
            throw new ScannerBridgeException("invalid_dpi", "وضوح اسکن باید یکی از مقادیر ۱۵۰، ۲۰۰، ۳۰۰ یا ۶۰۰ DPI باشد.");
        }

        if (!SupportedColorModes.Contains(request.ColorMode))
        {
            throw new ScannerBridgeException("invalid_color_mode", "حالت رنگ انتخاب‌شده معتبر نیست.");
        }

        if (!SupportedPageSizes.Contains(request.PageSize))
        {
            throw new ScannerBridgeException("invalid_page_size", "اندازه کاغذ انتخاب‌شده معتبر نیست.");
        }

        if (!SupportedPaperSources.Contains(request.PaperSource))
        {
            throw new ScannerBridgeException("invalid_paper_source", "منبع کاغذ انتخاب‌شده معتبر نیست.");
        }

        if (!string.Equals(request.Format, "jpeg", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(request.Format, "jpg", StringComparison.OrdinalIgnoreCase))
        {
            throw new ScannerBridgeException("unsupported_format", "در حال حاضر خروجی پیش‌نمایش فقط با فرمت JPEG پشتیبانی می‌شود.");
        }
    }
}
