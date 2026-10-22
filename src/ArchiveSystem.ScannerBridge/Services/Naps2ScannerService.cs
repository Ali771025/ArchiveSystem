using System.Diagnostics;
using ArchiveSystem.ScannerBridge.Models;
using ArchiveSystem.ScannerBridge.Options;
using Microsoft.Extensions.Options;

namespace ArchiveSystem.ScannerBridge.Services;

public sealed class Naps2ScannerService : IScannerService, IDisposable
{
    private readonly ScannerBridgeOptions _options;
    private readonly ILogger<Naps2ScannerService> _logger;
    private readonly SemaphoreSlim _scannerLock = new(1, 1);

    public Naps2ScannerService(
        IOptions<ScannerBridgeOptions> options,
        ILogger<Naps2ScannerService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public string? ResolveConsolePath()
    {
        var candidates = new[]
        {
            _options.Naps2ConsolePath,
            Path.Combine(AppContext.BaseDirectory, "NAPS2.Console.exe"),
            Environment.ExpandEnvironmentVariables(@"%ProgramFiles%\NAPS2\NAPS2.Console.exe"),
            Environment.ExpandEnvironmentVariables(@"%ProgramFiles(x86)%\NAPS2\NAPS2.Console.exe")
        };

        return candidates
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .FirstOrDefault(File.Exists);
    }

    public async Task<IReadOnlyList<ScannerDeviceResponse>> GetDevicesAsync(
        CancellationToken cancellationToken)
    {
        EnsureWindows();
        var consolePath = RequireConsolePath();
        var devices = new List<CliScannerDevice>();

        if (_options.EnableTwain)
        {
            await AddDevicesAsync(consolePath, ScannerDriver.Twain, devices, cancellationToken);
        }

        if (_options.EnableWia)
        {
            await AddDevicesAsync(consolePath, ScannerDriver.Wia, devices, cancellationToken);
        }

        var uniqueDevices = devices
            .DistinctBy(device => (device.Driver, device.Name), DeviceIdentityComparer.Instance)
            .OrderBy(device => device.Driver == ScannerDriver.Twain ? 0 : 1)
            .ThenBy(device => device.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        var preferredDevice = uniqueDevices.FirstOrDefault(IsFujitsuDevice) ?? uniqueDevices.FirstOrDefault();

        return uniqueDevices
            .Select(device => new ScannerDeviceResponse(
                ScannerDeviceKey.Encode(device.Driver, device.Name),
                device.Name,
                device.Driver.ToString().ToLowerInvariant(),
                device == preferredDevice))
            .ToList();
    }

    public async Task<ScanResponse> ScanAsync(ScanRequest request, CancellationToken cancellationToken)
    {
        EnsureWindows();
        ScanRequestValidator.Validate(request);

        if (!await _scannerLock.WaitAsync(0, cancellationToken))
        {
            throw new ScannerBridgeException(
                "scanner_busy",
                "یک عملیات اسکن دیگر در حال اجرا است. پس از پایان آن دوباره تلاش کنید.",
                StatusCodes.Status409Conflict);
        }

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(_options.ScanTimeoutSeconds, 30, 1800)));
            return await ScanCoreAsync(request, timeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ScannerBridgeException(
                "scan_timeout",
                "زمان عملیات اسکن بیش از حد مجاز شد. اتصال دستگاه و وضعیت کاغذ را بررسی کنید.",
                StatusCodes.Status504GatewayTimeout);
        }
        finally
        {
            _scannerLock.Release();
        }
    }

    private async Task<ScanResponse> ScanCoreAsync(ScanRequest request, CancellationToken cancellationToken)
    {
        var consolePath = RequireConsolePath();
        var (driver, deviceName) = ScannerDeviceKey.Decode(request.ScannerId);
        EnsureDriverEnabled(driver);

        var availableDevices = await ListDeviceNamesAsync(consolePath, driver, cancellationToken);
        if (!availableDevices.Contains(deviceName, StringComparer.OrdinalIgnoreCase))
        {
            throw new ScannerBridgeException(
                "scanner_not_found",
                "اسکنر انتخاب‌شده پیدا نشد. اتصال USB و نصب بودن درایور را بررسی و دستگاه‌ها را دوباره شناسایی کنید.",
                StatusCodes.Status404NotFound);
        }

        var scanDirectory = Path.Combine(
            Path.GetTempPath(),
            "ArchiveSystem.ScannerBridge",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(scanDirectory);

        try
        {
            var outputPattern = Path.Combine(scanDirectory, "page-$(nnn).jpg");
            var arguments = BuildScanArguments(request, driver, deviceName, outputPattern);
            var processResult = await RunNaps2Async(consolePath, arguments, cancellationToken);
            var pagePaths = Directory
                .EnumerateFiles(scanDirectory, "page-*.jpg", SearchOption.TopDirectoryOnly)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (pagePaths.Count == 0)
            {
                if (processResult.ExitCode != 0)
                {
                    throw MapProcessError(processResult.StandardError);
                }

                throw new ScannerBridgeException(
                    "no_pages",
                    "اسکنر هیچ صفحه‌ای برنگرداند. کاغذ و منبع انتخاب‌شده را بررسی کنید.");
            }

            var maxPages = Math.Clamp(_options.MaxPagesPerScan, 1, 1000);
            if (pagePaths.Count > maxPages)
            {
                throw TooLarge();
            }

            var pages = new List<ScannerPageResponse>(pagePaths.Count);
            long totalBytes = 0;
            foreach (var pagePath in pagePaths)
            {
                var bytes = await File.ReadAllBytesAsync(pagePath, cancellationToken);
                totalBytes += bytes.LongLength;
                if (totalBytes > Math.Max(_options.MaxResponseBytes, 1024 * 1024))
                {
                    throw TooLarge();
                }

                var pageNumber = pages.Count + 1;
                pages.Add(new ScannerPageResponse(
                    Convert.ToBase64String(bytes),
                    "image/jpeg",
                    $"scan-{DateTime.Now:yyyyMMdd-HHmmss}-page-{pageNumber:D3}.jpg"));
            }

            if (processResult.ExitCode != 0)
            {
                var warning = MapProcessError(processResult.StandardError).Message;
                _logger.LogWarning(
                    "NAPS2 returned exit code {ExitCode} after producing {PageCount} pages: {Error}",
                    processResult.ExitCode,
                    pages.Count,
                    Truncate(processResult.StandardError));
                return new ScanResponse(pages, Partial: true, Warning: warning);
            }

            return new ScanResponse(pages);
        }
        finally
        {
            TryDeleteDirectory(scanDirectory);
        }
    }

    private async Task AddDevicesAsync(
        string consolePath,
        ScannerDriver driver,
        ICollection<CliScannerDevice> destination,
        CancellationToken cancellationToken)
    {
        try
        {
            var names = await ListDeviceNamesAsync(consolePath, driver, cancellationToken);
            foreach (var name in names)
            {
                destination.Add(new CliScannerDevice(driver, name));
            }
        }
        catch (ScannerBridgeException exception)
        {
            _logger.LogWarning("Could not enumerate {Driver} scanner devices: {Message}", driver, exception.Message);
        }
    }

    private async Task<IReadOnlyList<string>> ListDeviceNamesAsync(
        string consolePath,
        ScannerDriver driver,
        CancellationToken cancellationToken)
    {
        var result = await RunNaps2Async(
            consolePath,
            ["--listdevices", "--driver", DriverArgument(driver)],
            cancellationToken);

        if (result.ExitCode != 0)
        {
            throw MapProcessError(result.StandardError);
        }

        return result.StandardOutput
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private IReadOnlyList<string> BuildScanArguments(
        ScanRequest request,
        ScannerDriver driver,
        string deviceName,
        string outputPattern)
    {
        var arguments = new List<string>
        {
            "--output", outputPattern,
            "--force",
            "--noprofile",
            "--driver", DriverArgument(driver),
            "--device", deviceName,
            "--dpi", request.Dpi.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "--bitdepth", BitDepthArgument(request.ColorMode),
            "--jpegquality", Math.Clamp(_options.JpegQuality, 50, 100).ToString(System.Globalization.CultureInfo.InvariantCulture)
        };

        var source = SourceArgument(request.PaperSource);
        if (source is not null)
        {
            arguments.Add("--source");
            arguments.Add(source);
        }

        if (string.Equals(request.PageSize, "a4", StringComparison.OrdinalIgnoreCase))
        {
            arguments.Add("--pagesize");
            arguments.Add("a4");
        }

        if (request.AutoDeskew)
        {
            arguments.Add("--deskew");
        }

        return arguments;
    }

    private async Task<ProcessResult> RunNaps2Async(
        string consolePath,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = consolePath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(consolePath) ?? AppContext.BaseDirectory
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };
        try
        {
            if (!process.Start())
            {
                throw ConsoleUnavailable();
            }
        }
        catch (Exception exception) when (exception is not ScannerBridgeException)
        {
            _logger.LogError(exception, "Could not start NAPS2 Console at {Path}", consolePath);
            throw ConsoleUnavailable();
        }

        var standardOutputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var standardErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            throw;
        }

        var standardOutput = Truncate(await standardOutputTask);
        var standardError = Truncate(await standardErrorTask);
        return new ProcessResult(process.ExitCode, standardOutput, standardError);
    }

    private string RequireConsolePath() => ResolveConsolePath() ?? throw ConsoleUnavailable();

    private static void EnsureWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new ScannerBridgeException(
                "windows_required",
                "Scanner Bridge باید روی رایانه ویندوزی متصل به اسکنر اجرا شود.",
                StatusCodes.Status503ServiceUnavailable);
        }
    }

    private void EnsureDriverEnabled(ScannerDriver driver)
    {
        if ((driver == ScannerDriver.Twain && !_options.EnableTwain) ||
            (driver == ScannerDriver.Wia && !_options.EnableWia))
        {
            throw new ScannerBridgeException(
                "driver_disabled",
                "درایور انتخاب‌شده در تنظیمات Scanner Bridge غیرفعال است.");
        }
    }

    private static bool IsFujitsuDevice(CliScannerDevice device) =>
        device.Name.Contains("Fujitsu", StringComparison.OrdinalIgnoreCase) ||
        device.Name.Contains("RICOH", StringComparison.OrdinalIgnoreCase) ||
        device.Name.Contains("PaperStream", StringComparison.OrdinalIgnoreCase) ||
        device.Name.StartsWith("fi-", StringComparison.OrdinalIgnoreCase);

    private static string DriverArgument(ScannerDriver driver) =>
        driver == ScannerDriver.Twain ? "twain" : "wia";

    private static string BitDepthArgument(string colorMode) => colorMode.ToLowerInvariant() switch
    {
        "grayscale" => "gray",
        "black-and-white" => "bw",
        _ => "color"
    };

    private static string? SourceArgument(string paperSource) => paperSource.ToLowerInvariant() switch
    {
        "flatbed" => "glass",
        "feeder" => "feeder",
        "duplex" => "duplex",
        _ => null
    };

    private static ScannerBridgeException MapProcessError(string error)
    {
        var normalized = error.ToLowerInvariant();
        if (normalized.Contains("paper jam"))
        {
            return new("paper_jam", "کاغذ در اسکنر گیر کرده است. پس از رفع گیر دوباره تلاش کنید.", StatusCodes.Status409Conflict);
        }

        if (normalized.Contains("feeder") && (normalized.Contains("empty") || normalized.Contains("no paper")))
        {
            return new("feeder_empty", "کاغذی در تغذیه‌کننده اسکنر قرار ندارد.", StatusCodes.Status409Conflict);
        }

        if (normalized.Contains("busy"))
        {
            return new("device_busy", "اسکنر در حال استفاده توسط برنامه دیگری است.", StatusCodes.Status409Conflict);
        }

        if (normalized.Contains("offline") || normalized.Contains("communication"))
        {
            return new("device_offline", "اسکنر خاموش، آفلاین یا ارتباط USB آن قطع است.", StatusCodes.Status503ServiceUnavailable);
        }

        return new(
            "scan_failed",
            "عملیات NAPS2 ناموفق بود. وضعیت اسکنر، کاغذ و درایور PaperStream IP را بررسی کنید.",
            StatusCodes.Status500InternalServerError);
    }

    private static ScannerBridgeException TooLarge() =>
        new(
            "scan_too_large",
            "تعداد صفحات یا حجم اسکن از حد مجاز بیشتر شد. سند را در چند مرحله اسکن کنید.",
            StatusCodes.Status413PayloadTooLarge);

    private static ScannerBridgeException ConsoleUnavailable() =>
        new(
            "naps2_not_found",
            "NAPS2 Console پیدا نشد. NAPS2 را نصب یا مسیر NAPS2.Console.exe را در تنظیمات Bridge وارد کنید.",
            StatusCodes.Status503ServiceUnavailable);

    private string Truncate(string value)
    {
        var maxLength = Math.Clamp(_options.MaxProcessOutputCharacters, 1024, 1_000_000);
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // The process may have exited between the state check and Kill.
        }
    }

    private static void TryDeleteDirectory(string directory)
    {
        try
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
        catch
        {
            // Temporary cleanup must not replace the scan result/error.
        }
    }

    public void Dispose() => _scannerLock.Dispose();

    private sealed record CliScannerDevice(ScannerDriver Driver, string Name);
    private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);

    private sealed class DeviceIdentityComparer : IEqualityComparer<(ScannerDriver Driver, string Name)>
    {
        public static DeviceIdentityComparer Instance { get; } = new();

        public bool Equals(
            (ScannerDriver Driver, string Name) left,
            (ScannerDriver Driver, string Name) right) =>
            left.Driver == right.Driver &&
            string.Equals(left.Name, right.Name, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode((ScannerDriver Driver, string Name) value) =>
            HashCode.Combine(value.Driver, StringComparer.OrdinalIgnoreCase.GetHashCode(value.Name));
    }
}
