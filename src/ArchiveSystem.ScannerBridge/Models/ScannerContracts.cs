namespace ArchiveSystem.ScannerBridge.Models;

public sealed record ScannerDeviceResponse(
    string Id,
    string Name,
    string Driver,
    bool IsDefault);

public sealed record ScanRequest(
    string ScannerId,
    int Dpi,
    string ColorMode,
    string PageSize,
    string PaperSource = "auto",
    bool AutoDeskew = true,
    string Format = "jpeg");

public sealed record ScannerPageResponse(
    string Base64,
    string ContentType,
    string FileName);

public sealed record ScanResponse(
    IReadOnlyList<ScannerPageResponse> Pages,
    bool Partial = false,
    string? Warning = null);

public sealed record BridgeErrorResponse(
    string Code,
    string Message,
    string? TraceId = null);

public sealed record BridgeHealthResponse(
    string Status,
    string Version,
    bool PlatformSupported,
    bool TwainEnabled,
    bool WiaEnabled,
    bool Naps2ConsoleAvailable,
    string? Naps2ConsolePath);
