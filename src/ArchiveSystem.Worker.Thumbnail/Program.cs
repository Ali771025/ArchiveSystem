// src/ArchiveSystem.Worker.Thumbnail/Program.cs
using ArchiveSystem.Infrastructure;
using ArchiveSystem.Worker.Thumbnail;
using ArchiveSystem.Worker.Thumbnail.Workers;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        var configuration = hostContext.Configuration;

        // Add infrastructure services
        services.AddInfrastructure(configuration, includeMessaging: false);

        // Add thumbnail worker services
        services.AddThumbnailWorkerServices(configuration);

        // Add the background worker
        services.AddHostedService<ThumbnailWorker>();
    })
    .UseSystemd() // Enable systemd integration for Linux daemons
    .UseWindowsService() // Enable Windows service integration
    .Build();

await host.RunAsync();
