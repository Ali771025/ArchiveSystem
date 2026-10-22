// src/ArchiveSystem.Worker.Ocr/DependencyInjection.cs
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ArchiveSystem.Domain.Services;
using ArchiveSystem.Worker.Ocr.Consumers;
using ArchiveSystem.Worker.Ocr.Options;
using ArchiveSystem.Worker.Ocr.Services;
using ArchiveSystem.Infrastructure.Messaging;

namespace ArchiveSystem.Worker.Ocr
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddOcrWorkerServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<OcrWorkerOptions>(configuration.GetSection(OcrWorkerOptions.SectionName));
            services.Configure<RapidOcrEngineOptions>(configuration.GetSection(RapidOcrEngineOptions.SectionName));
            services.AddScoped<IOcrService, RapidOcrService>();
            services.AddScoped<OcrProcessingService>();

            var messagingEnabled = configuration.GetValue<bool?>("Messaging:Enabled") ?? true;
            if (messagingEnabled)
            {
                services.AddMassTransit(x =>
                {
                    x.AddConsumer<FileVersionCreatedEventConsumer>();

                    x.UsingRabbitMq((context, cfg) =>
                    {
                        var rabbitMqSection = configuration.GetSection("RabbitMQ");
                        var rabbitMqHost = rabbitMqSection["Host"] ?? "localhost";
                        var rabbitMqUsername = rabbitMqSection["Username"] ?? "guest";
                        var rabbitMqPassword = rabbitMqSection["Password"] ?? "guest";

                        cfg.Host(rabbitMqHost, h =>
                        {
                            h.Username(rabbitMqUsername);
                            h.Password(rabbitMqPassword);
                        });

                        cfg.ReceiveEndpoint("file-version-created-ocr", e =>
                        {
                            var maxConcurrentJobs = configuration.GetValue<int?>("OcrWorker:MaxConcurrentJobs") ?? 2;
                            e.ConcurrentMessageLimit = Math.Max(1, Math.Min(maxConcurrentJobs, 16));
                            e.UseMessageRetry(r => r.Incremental(3, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5)));
                            e.Consumer<FileVersionCreatedEventConsumer>(context);
                        });
                    });
                });

                // AddInfrastructure is intentionally called with includeMessaging=false in the
                // worker host to avoid registering MassTransit twice. Replace its NoOp publisher
                // here when the event-driven path is enabled.
                services.AddScoped<IMessageBus, RabbitMqMessageBus>();
            }

            return services;
        }
    }
}
