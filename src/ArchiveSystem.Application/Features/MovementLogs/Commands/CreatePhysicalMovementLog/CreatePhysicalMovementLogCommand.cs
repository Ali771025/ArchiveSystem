using ArchiveSystem.Application.Common.Models.MovementLogs;
using ArchiveSystem.Application.Features.MovementLogs.Interfaces;
using MediatR;

namespace ArchiveSystem.Application.Features.MovementLogs.Commands.CreatePhysicalMovementLog;

public sealed record CreatePhysicalMovementLogCommand(CreateMovementLogDto Dto) : IRequest<MovementLogResponseDto>;

public sealed class CreatePhysicalMovementLogCommandHandler
    : IRequestHandler<CreatePhysicalMovementLogCommand, MovementLogResponseDto>
{
    private readonly IPhysicalMovementLogRepository _repository;

    public CreatePhysicalMovementLogCommandHandler(IPhysicalMovementLogRepository repository)
    {
        _repository = repository;
    }

    public Task<MovementLogResponseDto> Handle(
        CreatePhysicalMovementLogCommand request,
        CancellationToken cancellationToken)
    {
        return _repository.CreateAsync(request.Dto, cancellationToken);
    }
}
