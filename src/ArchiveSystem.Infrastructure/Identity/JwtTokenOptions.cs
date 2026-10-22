namespace ArchiveSystem.Infrastructure.Identity
{
    public sealed class JwtTokenOptions
    {
        public const string SectionName = "Jwt";

        public string Secret { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int ExpirationMinutes { get; set; } = 60;
        public int IdleTimeoutMinutes { get; set; } = 20;
    }
}
