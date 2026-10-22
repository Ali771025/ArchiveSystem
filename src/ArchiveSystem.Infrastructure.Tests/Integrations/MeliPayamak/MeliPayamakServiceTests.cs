using System.Net;
using System.Text;
using ArchiveSystem.Infrastructure.Integrations.MeliPayamak;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ArchiveSystem.Infrastructure.Tests.Integrations.MeliPayamak;

public sealed class MeliPayamakServiceTests
{
    [Theory]
    [InlineData(-110)]
    [InlineData(-109)]
    [InlineData(-108)]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(14)]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(17)]
    [InlineData(18)]
    [InlineData(35)]
    public async Task SendPatternAsync_KnownProviderError_ReturnsFailure(long providerCode)
    {
        var service = CreateService(providerCode);

        var result = await service.SendPatternAsync(CreateRequest());

        Assert.False(result.Succeeded);
        Assert.Equal(providerCode.ToString(), result.ProviderTrackingId);
        Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));
    }

    [Fact]
    public async Task SendPatternAsync_TrackingId_ReturnsSuccess()
    {
        const long trackingId = 987654321;
        var service = CreateService(trackingId);

        var result = await service.SendPatternAsync(CreateRequest());

        Assert.True(result.Succeeded);
        Assert.Equal(trackingId.ToString(), result.ProviderTrackingId);
        Assert.Null(result.ErrorMessage);
    }

    private static MeliPayamakService CreateService(long providerValue)
    {
        var responseJson = $$"""
            {"RetStatus":1,"StrRetStatus":null,"Value":{{providerValue}}}
            """;
        var handler = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        });
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://rest.payamak-panel.com/")
        };
        var options = Options.Create(new MeliPayamakOptions
        {
            Username = "test-user",
            Password = "test-password",
            From = "500000",
            PatternEndpoint = "api/SendSMS/BaseServiceNumber"
        });

        return new MeliPayamakService(
            httpClient,
            options,
            NullLogger<MeliPayamakService>.Instance);
    }

    private static MeliPayamakPatternRequest CreateRequest() =>
        new("09120000000", "123", new[] { "54321" });

    private sealed class StubHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(response);
    }
}
