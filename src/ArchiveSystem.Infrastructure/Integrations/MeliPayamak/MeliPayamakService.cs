using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchiveSystem.Infrastructure.Integrations.MeliPayamak
{
    public sealed class MeliPayamakService : IMeliPayamakService
    {
        private static readonly IReadOnlyDictionary<long, string> ProviderErrors = new Dictionary<long, string>
        {
            [-110] = "برای اتصال به ملی پیامک باید از ApiKey به جای رمز عبور استفاده شود.",
            [-109] = "IP سرور در فهرست IPهای مجاز ملی پیامک تنظیم نشده است.",
            [-108] = "IP سرور به دلیل تلاش ناموفق برای استفاده از API مسدود شده است.",
            [0] = "نام کاربری یا رمز عبور ملی پیامک اشتباه است.",
            [2] = "اعتبار پنل ملی پیامک کافی نیست.",
            [3] = "محدودیت ارسال روزانه ملی پیامک تکمیل شده است.",
            [4] = "محدودیت حجم ارسال ملی پیامک تکمیل شده است.",
            [5] = "شماره فرستنده پیامک معتبر نیست.",
            [6] = "سامانه ملی پیامک در حال به‌روزرسانی است.",
            [7] = "متن پیامک حاوی کلمه فیلترشده است.",
            [9] = "ارسال وب‌سرویسی از خط عمومی امکان‌پذیر نیست.",
            [10] = "حساب کاربری ملی پیامک فعال نیست.",
            [11] = "پیامک ارسال نشد.",
            [12] = "مدارک حساب ملی پیامک کامل نیست.",
            [14] = "متن پیامک حاوی لینک است.",
            [15] = "ارسال به بیش از یک شماره بدون درج «لغو11» امکان‌پذیر نیست.",
            [16] = "شماره گیرنده‌ای برای ارسال پیامک یافت نشد.",
            [17] = "متن پیامک خالی است.",
            [18] = "شماره گیرنده پیامک نامعتبر است.",
            [35] = "شماره گیرنده در فهرست سیاه مخابرات قرار دارد."
        };

        private readonly HttpClient _httpClient;
        private readonly MeliPayamakOptions _options;
        private readonly ILogger<MeliPayamakService> _logger;

        public MeliPayamakService(
            HttpClient httpClient,
            IOptions<MeliPayamakOptions> options,
            ILogger<MeliPayamakService> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<MeliPayamakSendResult> SendPatternAsync(MeliPayamakPatternRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.MobileNumber))
            {
                throw new ArgumentException("Mobile number is required.", nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.PatternCode))
            {
                throw new ArgumentException("Pattern code is required.", nameof(request));
            }

            if (string.IsNullOrWhiteSpace(_options.Username)
                || string.IsNullOrWhiteSpace(_options.Password)
                || string.IsNullOrWhiteSpace(_options.From))
            {
                return new MeliPayamakSendResult(false, null, "MeliPayamak settings are incomplete.");
            }

            var startedAt = Stopwatch.StartNew();
            var payload = new MeliPayamakPatternPayload(
                Username: _options.Username,
                Password: _options.Password,
                To: request.MobileNumber,
                From: _options.From,
                BodyId: request.PatternCode,
                Text: string.Join(';', request.Tokens));

            using var response = await _httpClient.PostAsJsonAsync(_options.PatternEndpoint, payload, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "MeliPayamak pattern send failed with HTTP {StatusCode}. Mobile={Mobile} BodyId={BodyId} DurationMs={DurationMs}",
                    (int)response.StatusCode,
                    MaskMobile(request.MobileNumber),
                    request.PatternCode,
                    startedAt.ElapsedMilliseconds);

                return new MeliPayamakSendResult(false, null, $"Provider HTTP failure: {(int)response.StatusCode}");
            }

            var providerResponse = System.Text.Json.JsonSerializer.Deserialize<MeliPayamakPatternResponse>(content);
            if (providerResponse is null)
            {
                return new MeliPayamakSendResult(false, null, "Provider response could not be parsed.");
            }

            var providerValue = ReadProviderValue(providerResponse.Value);
            if (TryGetProviderError(providerValue, out var providerError))
            {
                _logger.LogWarning(
                    "MeliPayamak provider returned an error. Code={ProviderCode} Error={ProviderError} Mobile={Mobile} BodyId={BodyId}",
                    providerValue,
                    providerError,
                    MaskMobile(request.MobileNumber),
                    request.PatternCode);

                return new MeliPayamakSendResult(false, providerValue.ToString(), providerError);
            }

            _logger.LogInformation(
                "MeliPayamak pattern send succeeded. Mobile={Mobile} BodyId={BodyId} TrackingId={TrackingId} DurationMs={DurationMs}",
                MaskMobile(request.MobileNumber),
                request.PatternCode,
                providerValue,
                startedAt.ElapsedMilliseconds);

            return new MeliPayamakSendResult(true, providerValue.ToString(), null);
        }

        private static string MaskMobile(string mobile)
        {
            if (string.IsNullOrWhiteSpace(mobile) || mobile.Length < 7)
            {
                return "***";
            }

            return $"{mobile[..4]}****{mobile[^3..]}";
        }

        private static long ReadProviderValue(JsonElement value)
        {
            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var numericValue))
            {
                return numericValue;
            }

            if (value.ValueKind == JsonValueKind.String &&
                long.TryParse(value.GetString(), out var stringValue))
            {
                return stringValue;
            }

            return -1;
        }

        private static bool TryGetProviderError(long providerValue, out string errorMessage)
        {
            if (ProviderErrors.TryGetValue(providerValue, out errorMessage!))
            {
                return true;
            }

            if (providerValue < 0 && ProviderErrors.TryGetValue(Math.Abs(providerValue), out errorMessage!))
            {
                return true;
            }

            if (providerValue < 0)
            {
                errorMessage = $"ملی پیامک ارسال را با کد خطای {providerValue} رد کرد.";
                return true;
            }

            errorMessage = string.Empty;
            return false;
        }

        private sealed record MeliPayamakPatternPayload(
            [property: JsonPropertyName("username")] string Username,
            [property: JsonPropertyName("password")] string Password,
            [property: JsonPropertyName("to")] string To,
            [property: JsonPropertyName("from")] string From,
            [property: JsonPropertyName("bodyId")] string BodyId,
            [property: JsonPropertyName("text")] string Text);

        private sealed record MeliPayamakPatternResponse(
            [property: JsonPropertyName("RetStatus")] int RetStatus,
            [property: JsonPropertyName("StrRetStatus")] string? Message,
            [property: JsonPropertyName("Value")] JsonElement Value);
    }
}
