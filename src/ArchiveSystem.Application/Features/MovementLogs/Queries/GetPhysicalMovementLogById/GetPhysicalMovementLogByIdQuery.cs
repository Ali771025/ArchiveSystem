using ArchiveSystem.Application.Common.Models.MovementLogs;
using ArchiveSystem.Application.Features.MovementLogs.Interfaces;
using MediatR;

namespace ArchiveSystem.Application.Features.MovementLogs.Queries.GetPhysicalMovementLogById;

public sealed record GetPhysicalMovementLogByIdQuery(Guid Id) : IRequest<MovementLogResponseDto?>;

public sealed class GetPhysicalMovementLogByIdQueryHandler
    : IRequestHandler<GetPhysicalMovementLogByIdQuery, MovementLogResponseDto?>
{
    private readonly IPhysicalMovementLogRepository _repository;

    public GetPhysicalMovementLogByIdQueryHandler(IPhysicalMovementLogRepository repository)
    {
        _repository = repository;
    }

    public Task<MovementLogResponseDto?> Handle(
        GetPhysicalMovementLogByIdQuery request,
        CancellationToken cancellationToken)
    {
        return _repository.GetByIdAsync(request.Id, cancellationToken);
    }
}
