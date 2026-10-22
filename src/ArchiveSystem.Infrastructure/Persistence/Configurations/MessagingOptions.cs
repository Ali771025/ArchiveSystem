// src/ArchiveSystem.Infrastructure/Configurations/MessagingOptions.cs
namespace ArchiveSystem.Infrastructure.Configurations
{
    public class MessagingOptions
    {
        public const string SectionName = "Messaging";

        public string Host { get; set; } = "localhost";
        public string Username { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public int Port { get; set; } = 5672;
        public string VirtualHost { get; set; } = "/";
    }
}