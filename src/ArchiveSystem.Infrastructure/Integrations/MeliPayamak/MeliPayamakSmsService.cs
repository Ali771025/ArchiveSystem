using System.Net.Http.Json;
using System.Text.Json.Serialization;
using ArchiveSystem.Application.Features.Authentication.Otp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchiveSystem.Infrastructure.Integrations.MeliPayamak
{
    public sealed class MeliPayamakSmsService : ISmsSender
    {
        private const string FastSendEndpoint = "https://rest.payamak-panel.com/api/SendSMS/BaseServiceNumber";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly MeliPayamakSettings _settings;
        private readonly ILogger<MeliPayamakSmsService> _logger;

        public MeliPayamakSmsService(
            IOptions<MeliPayamakSettings> settings,
            IHttpClientFactory httpClientFactory,
            ILogger<MeliPayamakSmsService> logger)
        {
            _settings = settings.Value;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task SendSmsAsync(string mobileNumber, string otpCode, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_settings.ApiKey)
                || string.IsNullOrWhiteSpace(_settings.SenderNumber)
                || string.IsNullOrWhiteSpace(_settings.LoginPatternCode))
            {
                throw new InvalidOperationException("MeliPayamak settings are not configured. Please provide ApiKey, SenderNumber and LoginPatternCode.");
            }

            var client = _httpClientFactory.CreateClient("MeliPayamakFastSendClient");
            var payload = new FastSendRequest(
                ApiKey: _settings.ApiKey,
                To: mobileNumber,
                From: _settings.SenderNumber,
                BodyId: _settings.LoginPatternCode,
                Text: otpCode);

            using var response = await client.PostAsJsonAsync(FastSendEndpoint, payload, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "MeliPayamak fast-send failed. StatusCode={StatusCode} Mobile={Mobile}",
                    (int)response.StatusCode,
                    MaskMobile(mobileNumber));

                throw new InvalidOperationException($"MeliPayamak fast-send failed with HTTP {(int)response.StatusCode}. Response={body}");
            }
        }

        private static string MaskMobile(string mobile)
        {
            if (string.IsNullOrWhiteSpace(mobile) || mobile.Length < 7)
            {
                return "***";
            }

            return $"{mobile[..4]}****{mobile[^3..]}";
        }

        private sealed record FastSendRequest(
            [property: JsonPropertyName("apiKey")] string ApiKey,
            [property: JsonPropertyName("to")] string To,
            [property: JsonPropertyName("from")] string From,
            [property: JsonPropertyName("bodyId")] string BodyId,
            [property: JsonPropertyName("text")] string Text);
    }
}
