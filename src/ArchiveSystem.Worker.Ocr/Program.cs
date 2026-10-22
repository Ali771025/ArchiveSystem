// src/ArchiveSystem.Worker.Ocr/Program.cs
using ArchiveSystem.Infrastructure;
using ArchiveSystem.Worker.Ocr;
using ArchiveSystem.Worker.Ocr.Workers;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        var configuration = hostContext.Configuration;

        // Add infrastructure services
        services.AddInfrastructure(configuration, includeMessaging: false);

        // Add OCR worker services
        services.AddOcrWorkerServices(configuration);

        // Add the background worker
        services.AddHostedService<OcrWorker>();
    })
    .UseSystemd() // Enable systemd integration for Linux daemons
    .UseWindowsService() // Enable Windows service integration
    .Build();

await host.RunAsync();
