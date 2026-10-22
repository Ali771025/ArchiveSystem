using ArchiveSystem.Application.Interfaces.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ArchiveSystem.Infrastructure.Persistence
{
    public sealed class EfUnitOfWork : IUnitOfWork
    {
        private readonly ArchiveDbContext _dbContext;

        public EfUnitOfWork(ArchiveDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IAppDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            return new EfAppDbTransaction(transaction);
        }

        public async Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken = default)
        {
            var executionStrategy = _dbContext.Database.CreateExecutionStrategy();
            await executionStrategy.ExecuteAsync(
                state: operation,
                operation: async (_, state, ct) =>
                {
                    await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);

                    try
                    {
                        await state(ct);
                        await transaction.CommitAsync(ct);
                    }
                    catch
                    {
                        await transaction.RollbackAsync(ct);
                        throw;
                    }

                    return true;
                },
                verifySucceeded: null,
                cancellationToken: cancellationToken);
        }

        public async Task<TResult> ExecuteInTransactionAsync<TResult>(
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken = default)
        {
            var executionStrategy = _dbContext.Database.CreateExecutionStrategy();
            return await executionStrategy.ExecuteAsync(
                state: operation,
                operation: async (_, state, ct) =>
                {
                    await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);

                    try
                    {
                        var result = await state(ct);
                        await transaction.CommitAsync(ct);
                        return result;
                    }
                    catch
                    {
                        await transaction.RollbackAsync(ct);
                        throw;
                    }
                },
                verifySucceeded: null,
                cancellationToken: cancellationToken);
        }

        private sealed class EfAppDbTransaction : IAppDbTransaction
        {
            private readonly IDbContextTransaction _transaction;

            public EfAppDbTransaction(IDbContextTransaction transaction)
            {
                _transaction = transaction;
            }

            public Task CommitAsync(CancellationToken cancellationToken = default)
            {
                return _transaction.CommitAsync(cancellationToken);
            }

            public Task RollbackAsync(CancellationToken cancellationToken = default)
            {
                return _transaction.RollbackAsync(cancellationToken);
            }

            public ValueTask DisposeAsync()
            {
                return _transaction.DisposeAsync();
            }
        }
    }
}
