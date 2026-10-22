using System.Reflection;
using ArchiveSystem.ScannerBridge.Models;
using ArchiveSystem.ScannerBridge.Options;
using ArchiveSystem.ScannerBridge.Services;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});
var configuredOptions = builder.Configuration
    .GetSection(ScannerBridgeOptions.SectionName)
    .Get<ScannerBridgeOptions>() ?? new ScannerBridgeOptions();

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenLocalhost(Math.Clamp(configuredOptions.Port, 1024, 65535));
    serverOptions.Limits.MaxRequestBodySize = 64 * 1024;
});

builder.Services
    .AddOptions<ScannerBridgeOptions>()
    .Bind(builder.Configuration.GetSection(ScannerBridgeOptions.SectionName))
    .Validate(options => options.AllowedOrigins.Length > 0, "ScannerBridge:AllowedOrigins must not be empty")
    .ValidateOnStart();
builder.Services.AddSingleton<IScannerService, Naps2ScannerService>();
builder.Services.AddCors(corsOptions =>
{
    corsOptions.AddPolicy("ArchiveFrontend", policy =>
        policy.WithOrigins(configuredOptions.AllowedOrigins)
            .WithMethods("GET", "POST", "OPTIONS")
            .WithHeaders("Accept", "Content-Type"));
});

var app = builder.Build();
var allowedOrigins = configuredOptions.AllowedOrigins
    .Select(origin => origin.TrimEnd('/'))
    .ToHashSet(StringComparer.OrdinalIgnoreCase);

app.Use(async (context, next) =>
{
    context.Response.Headers.CacheControl = "no-store";
    context.Response.Headers.XContentTypeOptions = "nosniff";

    var origin = context.Request.Headers.Origin.ToString().TrimEnd('/');
    if (!string.IsNullOrWhiteSpace(origin) && !allowedOrigins.Contains(origin))
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(
            new BridgeErrorResponse("origin_not_allowed", "این نشانی اجازه دسترسی به Scanner Bridge را ندارد.", context.TraceIdentifier));
        return;
    }

    await next();
});
app.UseCors("ArchiveFrontend");

app.MapGet("/api/health", (IScannerService scannerService) =>
{
    var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";
    var naps2ConsolePath = (scannerService as Naps2ScannerService)?.ResolveConsolePath();
    return Results.Ok(new BridgeHealthResponse(
        "ok",
        version,
        OperatingSystem.IsWindows(),
        configuredOptions.EnableTwain,
        configuredOptions.EnableWia,
        naps2ConsolePath is not null,
        naps2ConsolePath));
});

app.MapGet("/api/scanners", async (IScannerService scannerService, HttpContext context) =>
{
    try
    {
        var devices = await scannerService.GetDevicesAsync(context.RequestAborted);
        return Results.Ok(new { devices });
    }
    catch (ScannerBridgeException exception)
    {
        return BridgeError(exception, context);
    }
});

app.MapPost("/api/scan", async (ScanRequest request, IScannerService scannerService, HttpContext context) =>
{
    try
    {
        var result = await scannerService.ScanAsync(request, context.RequestAborted);
        return Results.Ok(result);
    }
    catch (ScannerBridgeException exception)
    {
        return BridgeError(exception, context);
    }
});

app.Run();

static IResult BridgeError(ScannerBridgeException exception, HttpContext context) =>
    Results.Json(
        new BridgeErrorResponse(exception.Code, exception.Message, context.TraceIdentifier),
        statusCode: exception.StatusCode);
