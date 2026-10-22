namespace ArchiveSystem.ScannerBridge.Options;

public sealed class ScannerBridgeOptions
{
    public const string SectionName = "ScannerBridge";

    public int Port { get; set; } = 17891;
    public string[] AllowedOrigins { get; set; } =
    [
        "http://localhost:5173",
        "https://localhost:5173",
        "http://127.0.0.1:5173",
        "https://127.0.0.1:5173"
    ];
    public string? Naps2ConsolePath { get; set; }
    public bool EnableTwain { get; set; } = true;
    public bool EnableWia { get; set; } = true;
    public int ScanTimeoutSeconds { get; set; } = 300;
    public int MaxPagesPerScan { get; set; } = 200;
    public long MaxResponseBytes { get; set; } = 100 * 1024 * 1024;
    public int JpegQuality { get; set; } = 85;
    public int MaxProcessOutputCharacters { get; set; } = 32_768;
}
