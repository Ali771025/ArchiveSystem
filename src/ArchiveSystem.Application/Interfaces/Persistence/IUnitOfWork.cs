namespace ArchiveSystem.Application.Interfaces.Persistence
{
    public interface IUnitOfWork
    {
        Task<IAppDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken = default);
        Task<TResult> ExecuteInTransactionAsync<TResult>(
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken = default);
    }

    public interface IAppDbTransaction : IAsyncDisposable
    {
        Task CommitAsync(CancellationToken cancellationToken = default);
        Task RollbackAsync(CancellationToken cancellationToken = default);
    }
}
