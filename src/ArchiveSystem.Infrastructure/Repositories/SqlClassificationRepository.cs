// src/ArchiveSystem.Infrastructure/Repositories/SqlClassificationRepository.cs
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Domain.Repositories;
using System.Data;

namespace ArchiveSystem.Infrastructure.Repositories
{
    public class SqlClassificationRepository : IClassificationRepository
    {
        private readonly ArchiveDbContext _context;
        private bool? _hasDossiersTable;

        public SqlClassificationRepository(ArchiveDbContext context)
        {
            _context = context;
        }

        public async Task<Classification?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            return await _context.Classifications
                .AsNoTracking()
                .Include(c => c.Parent)
                .FirstOrDefaultAsync(c => c.Id == id && c.DeletedAt == null, cancellationToken);
        }

        public async Task<Classification?> GetBySystemKeyAsync(string systemKey, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(systemKey))
            {
                return null;
            }

            var normalized = systemKey.Trim();
            return await _context.Classifications
                .AsNoTracking()
                .Include(c => c.Parent)
                .FirstOrDefaultAsync(c => c.SystemKey == normalized && c.DeletedAt == null, cancellationToken);
        }

        public async Task<IReadOnlyList<Classification>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Classifications
                .AsNoTracking()
                .Include(c => c.Parent)
                .Where(c => c.DeletedAt == null)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Classification>> GetRootClassificationsAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Classifications
                .AsNoTracking()
                .Include(c => c.Parent)
                .Where(c => c.ParentId == null && c.DeletedAt == null)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Classification>> GetByParentIdAsync(long? parentId, CancellationToken cancellationToken = default)
        {
            return await _context.Classifications
                .AsNoTracking()
                .Include(c => c.Parent)
                .Where(c => c.ParentId == parentId && c.DeletedAt == null)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Classification>> GetByPathPrefixAsync(string pathPrefix, CancellationToken cancellationToken = default)
        {
            return await _context.Classifications
                .AsNoTracking()
                .Include(c => c.Parent)
                .Where(c => c.Path.StartsWith(pathPrefix) && c.DeletedAt == null)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Classification>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            return await _context.Classifications
                .AsNoTracking()
                .Include(c => c.Parent)
                .Where(c => c.DeletedAt == null)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<long> CountAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Classifications.LongCountAsync(c => c.DeletedAt == null, cancellationToken);
        }

        public async Task<bool> ExistsAsync(long id, CancellationToken cancellationToken = default)
        {
            return await _context.Classifications.AnyAsync(c => c.Id == id && c.DeletedAt == null, cancellationToken);
        }

        public async Task<bool> HasChildrenAsync(long id, CancellationToken cancellationToken = default)
        {
            return await _context.Classifications
                .AsNoTracking()
                .AnyAsync(c => c.ParentId == id && c.DeletedAt == null, cancellationToken);
        }

        public async Task<bool> ExistsActiveSiblingWithTitleAsync(
            long? parentId,
            string title,
            long? excludeClassificationId = null,
            CancellationToken cancellationToken = default)
        {
            var normalizedTitle = title.Trim();
            var query = _context.Classifications
                .AsNoTracking()
                .Where(c =>
                    c.DeletedAt == null &&
                    c.IsActive &&
                    c.ParentId == parentId &&
                    c.Title == normalizedTitle);

            if (excludeClassificationId.HasValue)
            {
                query = query.Where(c => c.Id != excludeClassificationId.Value);
            }

            return await query.AnyAsync(cancellationToken);
        }

        public async Task<bool> HasActiveCasesAsync(long id, CancellationToken cancellationToken = default)
        {
            return await _context.Cases
                .AsNoTracking()
                .AnyAsync(c => c.ClassificationId == id && c.DeletedAt == null, cancellationToken);
        }

        public async Task<bool> HasActiveDossiersAsync(long id, CancellationToken cancellationToken = default)
        {
            if (!await HasDossiersTableAsync(cancellationToken))
            {
                return false;
            }

            try
            {
                return await _context.Dossiers
                    .AsNoTracking()
                    .AnyAsync(d => d.ClassificationId == id && d.DeletedAt == null, cancellationToken);
            }
            catch (SqlException exception) when (IsMissingTable(exception, "Dossiers"))
            {
                _hasDossiersTable = false;
                return false;
            }
        }

        public async Task<IReadOnlyDictionary<long, int>> GetDossierCountsByClassificationAsync(CancellationToken cancellationToken = default)
        {
            // Always include Case-based counts so classification counters remain accurate
            // even when the Dossiers module is empty or disabled.
            var caseCounts = await _context.Cases
                .AsNoTracking()
                .Where(c => c.DeletedAt == null)
                .GroupBy(c => c.ClassificationId)
                .Select(g => new
                {
                    ClassificationId = g.Key,
                    Count = g.Count()
                })
                .ToListAsync(cancellationToken);

            var countsByClassification = caseCounts.ToDictionary(x => x.ClassificationId, x => x.Count);

            if (!await HasDossiersTableAsync(cancellationToken))
            {
                return countsByClassification;
            }

            try
            {
                var dossierCounts = await _context.Dossiers
                    .AsNoTracking()
                    .Where(d => d.DeletedAt == null)
                    .GroupBy(d => d.ClassificationId)
                    .Select(g => new
                    {
                        ClassificationId = g.Key,
                        Count = g.Count()
                    })
                    .ToListAsync(cancellationToken);

                foreach (var entry in dossierCounts)
                {
                    if (countsByClassification.TryGetValue(entry.ClassificationId, out var existingCount))
                    {
                        countsByClassification[entry.ClassificationId] = existingCount + entry.Count;
                    }
                    else
                    {
                        countsByClassification[entry.ClassificationId] = entry.Count;
                    }
                }

                return countsByClassification;
            }
            catch (SqlException exception) when (IsMissingTable(exception, "Dossiers"))
            {
                _hasDossiersTable = false;
                return countsByClassification;
            }
        }

        public async Task<IReadOnlyList<Classification>> SearchByTitleAsync(string query, int take = 50, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Array.Empty<Classification>();
            }

            var trimmed = query.Trim();
            var limit = Math.Clamp(take, 1, 200);

            return await _context.Classifications
                .AsNoTracking()
                .Where(c => c.DeletedAt == null && c.IsActive && EF.Functions.Like(c.Title, $"%{trimmed}%"))
                .OrderBy(c => c.Level)
                .ThenBy(c => c.Title)
                .Take(limit)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(Classification classification, CancellationToken cancellationToken = default)
        {
            await _context.Classifications.AddAsync(classification, cancellationToken);
        }

        public async Task UpdateAsync(Classification classification, CancellationToken cancellationToken = default)
        {
            _context.Classifications.Update(classification);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(Classification classification, CancellationToken cancellationToken = default)
        {
            classification.SoftDelete();
            _context.Classifications.Update(classification);
            await Task.CompletedTask;
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        private static bool IsMissingTable(SqlException exception, string tableName)
        {
            return exception.Number == 208
                && exception.Message.Contains(tableName, StringComparison.OrdinalIgnoreCase);
        }

        private async Task<bool> HasDossiersTableAsync(CancellationToken cancellationToken)
        {
            if (_hasDossiersTable.HasValue)
            {
                return _hasDossiersTable.Value;
            }

            var connection = _context.Database.GetDbConnection();
            var shouldClose = connection.State != ConnectionState.Open;
            if (shouldClose)
            {
                await connection.OpenAsync(cancellationToken);
            }

            try
            {
                await using var command = connection.CreateCommand();
                command.CommandText = @"
SELECT TOP (1) 1
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = 'dbo'
  AND TABLE_NAME = @tableName";

                var tableNameParameter = command.CreateParameter();
                tableNameParameter.ParameterName = "@tableName";
                tableNameParameter.Value = "Dossiers";
                command.Parameters.Add(tableNameParameter);

                var result = await command.ExecuteScalarAsync(cancellationToken);
                _hasDossiersTable = result is not null and not DBNull;
                return _hasDossiersTable.Value;
            }
            finally
            {
                if (shouldClose && connection.State == ConnectionState.Open)
                {
                    await connection.CloseAsync();
                }
            }
        }
    }
}
