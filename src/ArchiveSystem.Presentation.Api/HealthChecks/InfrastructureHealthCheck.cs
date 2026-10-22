// src/ArchiveSystem.Presentation.Api/HealthChecks/InfrastructureHealthCheck.cs
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Data.SqlClient;
using ArchiveSystem.Domain.Services;
using OpenSearch.Client;

namespace ArchiveSystem.Presentation.Api.HealthChecks
{
    public class InfrastructureHealthCheck : IHealthCheck
    {
        private readonly IConfiguration _configuration;
        private readonly IStorageService _storageService;
        private readonly IOpenSearchClient _openSearchClient;

        public InfrastructureHealthCheck(
            IConfiguration configuration,
            IStorageService storageService,
            IOpenSearchClient openSearchClient)
        {
            _configuration = configuration;
            _storageService = storageService;
            _openSearchClient = openSearchClient;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var tasks = new List<Task<bool>>
            {
                CheckDatabaseHealthAsync(),
                CheckStorageHealthAsync(),
                CheckSearchHealthAsync()
            };

            var results = await Task.WhenAll(tasks);

            if (results.All(r => r))
            {
                return HealthCheckResult.Healthy("All infrastructure services are healthy");
            }

            var unhealthyServices = new List<string>();
            if (!results[0]) unhealthyServices.Add("Database");
            if (!results[1]) unhealthyServices.Add("Storage");
            if (!results[2]) unhealthyServices.Add("Search");

            return HealthCheckResult.Unhealthy($"Unhealthy services: {string.Join(", ", unhealthyServices)}");
        }

        private async Task<bool> CheckDatabaseHealthAsync()
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    return false;
                }

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                using var command = new SqlCommand("SELECT 1", connection);
                var result = await command.ExecuteScalarAsync();
                return result != null;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> CheckStorageHealthAsync()
        {
            try
            {
                // Test storage connectivity by checking a simple operation
                var exists = await _storageService.ObjectExistsAsync("test", "health-check");
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> CheckSearchHealthAsync()
        {
            try
            {
                var response = await _openSearchClient.Cluster.HealthAsync();
                return response.IsValid;
            }
            catch
            {
                return false;
            }
        }
    }
}
