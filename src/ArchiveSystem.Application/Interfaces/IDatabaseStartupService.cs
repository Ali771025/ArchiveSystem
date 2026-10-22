namespace ArchiveSystem.Application.Interfaces;

public interface IDatabaseStartupService
{
    Task ApplyMigrationsAsync(CancellationToken cancellationToken = default);

    Task EnsureSchemaCompatibilityAsync(CancellationToken cancellationToken = default);
}
