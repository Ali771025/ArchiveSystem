namespace ArchiveSystem.Infrastructure.Integrations.MeliPayamak
{
    public sealed class MeliPayamakOptions
    {
        public const string SectionName = "Integrations:MeliPayamak";

        public string BaseUrl { get; set; } = "https://rest.payamak-panel.com/";
        public string PatternEndpoint { get; set; } = "api/SendSMS/BaseServiceNumber";
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string From { get; set; } = string.Empty;
        public string LoginOtpPatternCode { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; } = 10;
        public int RetryCount { get; set; } = 3;
        public int CircuitBreakDurationSeconds { get; set; } = 30;
        public int CircuitBreakFailureThreshold { get; set; } = 5;
    }
}
