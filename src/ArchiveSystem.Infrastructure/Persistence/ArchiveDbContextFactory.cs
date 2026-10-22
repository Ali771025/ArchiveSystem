using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;

namespace ArchiveSystem.Infrastructure.Persistence
{
    public class ArchiveDbContextFactory : IDesignTimeDbContextFactory<ArchiveDbContext>
    {
        public ArchiveDbContext CreateDbContext(string[] args)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

            var startupProjectPath = ResolveStartupProjectPath();
            var configuration = new ConfigurationBuilder()
                .SetBasePath(startupProjectPath)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var rawConnectionString =
                Environment.GetEnvironmentVariable("ARCHIVE_DB_CONNECTION")
                ?? configuration.GetConnectionString("DefaultConnection")
                ?? "Server=DESKTOP-LIQTV0I\\SQLSERVER;Database=ArchiveSystem;Trusted_Connection=true;Encrypt=false;TrustServerCertificate=true;MultipleActiveResultSets=true;";

            var builder = new SqlConnectionStringBuilder(rawConnectionString);
            builder.TrustServerCertificate = true;
            if (!builder.ContainsKey("Encrypt"))
            {
                builder["Encrypt"] = "False";
            }
            var connectionString = builder.ConnectionString;

            var optionsBuilder = new DbContextOptionsBuilder<ArchiveDbContext>();
            optionsBuilder.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(ArchiveDbContextFactory).Assembly.FullName);
            });

            return new ArchiveDbContext(optionsBuilder.Options);
        }

        private static string ResolveStartupProjectPath()
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var candidates = new[]
            {
                System.IO.Path.Combine(currentDirectory, "ArchiveSystem.Presentation.Api"),
                System.IO.Path.Combine(currentDirectory, "..", "ArchiveSystem.Presentation.Api"),
                currentDirectory
            };

            foreach (var candidate in candidates)
            {
                if (System.IO.File.Exists(System.IO.Path.Combine(candidate, "appsettings.json")))
                {
                    return System.IO.Path.GetFullPath(candidate);
                }
            }

            return currentDirectory;
        }
    }
}
