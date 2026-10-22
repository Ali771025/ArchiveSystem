using ArchiveSystem.Worker.ScannerIngest.Options;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ArchiveSystem.Worker.ScannerIngest
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddScannerIngestServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<ScannerIngestOptions>(configuration.GetSection(ScannerIngestOptions.SectionName));

            services.AddMassTransit(x =>
            {
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

                    cfg.ConfigureEndpoints(context);
                });
            });

            return services;
        }
    }
}
