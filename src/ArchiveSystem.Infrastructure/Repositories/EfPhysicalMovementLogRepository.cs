using ArchiveSystem.Application.Common.Models.MovementLogs;
using ArchiveSystem.Application.Features.MovementLogs.Interfaces;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ArchiveSystem.Infrastructure.Repositories;

public sealed class EfPhysicalMovementLogRepository : IPhysicalMovementLogRepository
{
    private readonly ArchiveDbContext _dbContext;

    public EfPhysicalMovementLogRepository(ArchiveDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MovementLogResponseDto> CreateAsync(
        CreateMovementLogDto dto,
        CancellationToken cancellationToken = default)
    {
        var movementLog = new PhysicalMovementLog
        {
            Id = Guid.NewGuid(),
            DossierNumber = dto.DossierNumber.Trim(),
            ReceiverFirstName = dto.ReceiverFirstName.Trim(),
            ReceiverLastName = dto.ReceiverLastName.Trim(),
            NationalCode = dto.NationalCode.Trim(),
            PhoneNumber = dto.PhoneNumber.Trim(),
            CheckoutDate = dto.CheckoutDate.ToUniversalTime(),
            ExpectedReturnDate = dto.ExpectedReturnDate?.ToUniversalTime(),
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.PhysicalMovementLogs.Add(movementLog);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToResponse(movementLog);
    }

    public async Task<MovementLogResponseDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var movementLog = await _dbContext.PhysicalMovementLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return movementLog == null ? null : MapToResponse(movementLog);
    }

    private static MovementLogResponseDto MapToResponse(PhysicalMovementLog movementLog)
    {
        return new MovementLogResponseDto
        {
            Id = movementLog.Id,
            DossierNumber = movementLog.DossierNumber,
            ReceiverFirstName = movementLog.ReceiverFirstName,
            ReceiverLastName = movementLog.ReceiverLastName,
            NationalCode = movementLog.NationalCode,
            PhoneNumber = movementLog.PhoneNumber,
            CheckoutDate = movementLog.CheckoutDate.ToUniversalTime().ToString("O"),
            ExpectedReturnDate = movementLog.ExpectedReturnDate?.ToUniversalTime().ToString("O"),
            Description = movementLog.Description,
            CreatedAtUtc = movementLog.CreatedAtUtc.ToUniversalTime().ToString("O")
        };
    }
}
