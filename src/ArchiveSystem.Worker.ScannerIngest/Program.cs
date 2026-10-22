using ArchiveSystem.Infrastructure;
using ArchiveSystem.Worker.ScannerIngest;
using ArchiveSystem.Worker.ScannerIngest.Workers;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddInfrastructure(hostContext.Configuration, includeMessaging: false);
        services.AddScannerIngestServices(hostContext.Configuration);
        services.AddHostedService<ScannerIngestWorker>();
    })
    .UseSystemd()
    .UseWindowsService()
    .Build();

await host.RunAsync();
