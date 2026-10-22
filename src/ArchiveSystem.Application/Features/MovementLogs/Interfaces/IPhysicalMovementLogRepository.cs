using ArchiveSystem.Application.Common.Models.MovementLogs;

namespace ArchiveSystem.Application.Features.MovementLogs.Interfaces;

public interface IPhysicalMovementLogRepository
{
    Task<MovementLogResponseDto> CreateAsync(
        CreateMovementLogDto dto,
        CancellationToken cancellationToken = default);

    Task<MovementLogResponseDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
