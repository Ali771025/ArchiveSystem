// src/ArchiveSystem.Worker.Indexer/DependencyInjection.cs
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ArchiveSystem.Worker.Indexer.Consumers;

namespace ArchiveSystem.Worker.Indexer
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddIndexerWorkerServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<OcrCompletedEventConsumer>();

            // Configure MassTransit with RabbitMQ
            services.AddMassTransit(x =>
            {
                x.AddConsumer<OcrCompletedEventConsumer>();
                x.AddConsumer<ThumbnailGeneratedEventConsumer>();

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

                    // OCR Completed Event Consumer
                    cfg.ReceiveEndpoint("ocr-completed-event", e =>
                    {
                        e.UseMessageRetry(r => r.Incremental(3, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5)));
                        e.Consumer<OcrCompletedEventConsumer>(context);
                    });

                    // Thumbnail Generated Event Consumer
                    cfg.ReceiveEndpoint("thumbnail-generated-event", e =>
                    {
                        e.UseMessageRetry(r => r.Incremental(3, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5)));
                        e.Consumer<ThumbnailGeneratedEventConsumer>(context);
                    });
                });
            });

            return services;
        }
    }
}
