// src/ArchiveSystem.Domain/IFileRepository.cs
using ArchiveSystem.Domain.Enums;
using FileEntity = ArchiveSystem.Domain.Entities.ArchiveFile;

namespace ArchiveSystem.Domain.Repositories
{
    public interface IFileRepository
    {
        Task<FileEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<FileEntity>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<FileEntity>> GetByCaseIdAsync(long caseId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<FileEntity>> GetByStatusAsync(FileStatus status, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<FileEntity>> GetByExtensionAsync(string extension, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<FileEntity>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
        Task<long> CountAsync(CancellationToken cancellationToken = default);
        Task<long> CountByCaseIdAsync(long caseId, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(long id, CancellationToken cancellationToken = default);
        Task AddAsync(FileEntity file, CancellationToken cancellationToken = default);
        Task UpdateAsync(FileEntity file, CancellationToken cancellationToken = default);
        Task DeleteAsync(FileEntity file, CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
