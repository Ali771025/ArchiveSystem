using ArchiveSystem.Application.Common.Models;
using ArchiveSystem.Application.Features.Owners.Models;

namespace ArchiveSystem.Application.Features.Owners.Interfaces;

public interface IOwnerRepository
{
    Task<OwnerDto> CreateAsync(CreateOwnerDto request, CancellationToken cancellationToken = default);
    Task<OwnerDto?> GetByIdAsync(long ownerId, CancellationToken cancellationToken = default);
    Task<OwnerDto> UpdateAsync(long ownerId, CreateOwnerDto request, CancellationToken cancellationToken = default);
    Task<PaginatedResult<OwnerDto>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<OwnerSearchItemDto>> SearchAsync(string? query, int take, CancellationToken cancellationToken = default);
}
