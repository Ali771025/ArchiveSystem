using ArchiveSystem.Application.Features.Owners.Interfaces;
using ArchiveSystem.Application.Features.Owners.Models;
using MediatR;

namespace ArchiveSystem.Application.Features.Owners.Commands.UpdateOwner;

public sealed record UpdateOwnerCommand(long OwnerId, CreateOwnerDto Request) : IRequest<OwnerDto>;

public sealed class UpdateOwnerCommandHandler : IRequestHandler<UpdateOwnerCommand, OwnerDto>
{
    private readonly IOwnerRepository _repository;

    public UpdateOwnerCommandHandler(IOwnerRepository repository)
    {
        _repository = repository;
    }

    public Task<OwnerDto> Handle(UpdateOwnerCommand request, CancellationToken cancellationToken)
    {
        return _repository.UpdateAsync(request.OwnerId, request.Request, cancellationToken);
    }
}
