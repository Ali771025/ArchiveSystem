// src/ArchiveSystem.Worker.Indexer/Program.cs
using ArchiveSystem.Infrastructure;
using ArchiveSystem.Worker.Indexer;
using ArchiveSystem.Worker.Indexer.Workers;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        var configuration = hostContext.Configuration;

        // Add infrastructure services (includes OpenSearch configuration)
        services.AddInfrastructure(configuration, includeMessaging: false);

        // Add indexer worker services
        services.AddIndexerWorkerServices(configuration);

        // Add the background worker
        services.AddHostedService<IndexerWorker>();
    })
    .UseSystemd() // Enable systemd integration for Linux daemons
    .UseWindowsService() // Enable Windows service integration
    .Build();

await host.RunAsync();
