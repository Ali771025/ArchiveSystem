namespace ArchiveSystem.Infrastructure.Services.Exports
{
    public interface IExportJobExecutionService
    {
        Task ExecuteAsync(Guid jobId, CancellationToken cancellationToken = default);
    }
}
