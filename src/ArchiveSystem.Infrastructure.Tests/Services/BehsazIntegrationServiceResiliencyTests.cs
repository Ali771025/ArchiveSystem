using ArchiveSystem.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ArchiveSystem.Infrastructure.Tests.Services
{
    public class BehsazIntegrationServiceResiliencyTests
    {
        [Fact]
        public async Task ProcessCase_WhenCircuitOpened_RejectsSubsequentCalls()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:BehsazConnection"] = "InvalidConnectionString",
                    ["Integrations:Behsaz:IsEnabled"] = "true",
                    ["Integrations:Behsaz:RetryCount"] = "0",
                    ["Integrations:Behsaz:CircuitBreaker:FailureThreshold"] = "1",
                    ["Integrations:Behsaz:CircuitBreaker:OpenDurationSeconds"] = "60"
                })
                .Build();

            var service = new BehsazIntegrationService(configuration, NullLogger<BehsazIntegrationService>.Instance);

            await Assert.ThrowsAnyAsync<Exception>(() => service.ProcessCaseAsync(1, "CASE-1", CancellationToken.None));

            var blocked = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.ProcessCaseAsync(2, "CASE-2", CancellationToken.None));

            Assert.Contains("circuit breaker is open", blocked.Message, StringComparison.OrdinalIgnoreCase);
        }
    }
}
