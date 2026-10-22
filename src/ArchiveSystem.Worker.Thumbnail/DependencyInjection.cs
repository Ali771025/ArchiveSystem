// src/ArchiveSystem.Worker.Thumbnail/DependencyInjection.cs
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ArchiveSystem.Worker.Thumbnail.Consumers;
using ArchiveSystem.Worker.Thumbnail.Options;
using ArchiveSystem.Worker.Thumbnail.Services;

namespace ArchiveSystem.Worker.Thumbnail
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddThumbnailWorkerServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<ThumbnailOptions>()
                .Bind(configuration.GetSection(ThumbnailOptions.SectionName))
                .ValidateDataAnnotations()
                .Validate(
                    options => IsSupportedOutputFormat(options.OutputFormat),
                    "Thumbnail.OutputFormat must be one of: jpeg, png, webp, bmp, gif, tiff.")
                .ValidateOnStart();

            services.AddScoped<IThumbnailGenerator, ThumbnailGenerator>();
            services.AddScoped<ThumbnailProcessingService>();

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

                    cfg.ReceiveEndpoint("file-version-created-thumbnail", e =>
                    {
                        e.UseMessageRetry(r => r.Incremental(3, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5)));
                        e.Consumer<FileVersionCreatedEventConsumer>(context);
                    });
                });
            });

            return services;
        }

        private static bool IsSupportedOutputFormat(string? outputFormat)
        {
            if (string.IsNullOrWhiteSpace(outputFormat))
            {
                return false;
            }

            var normalized = outputFormat.Trim().ToLowerInvariant();
            return normalized is "jpg" or "jpeg" or "png" or "webp" or "bmp" or "gif" or "tif" or "tiff";
        }
    }
}
