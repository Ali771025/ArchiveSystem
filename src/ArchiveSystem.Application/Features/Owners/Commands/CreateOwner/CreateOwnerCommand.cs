using ArchiveSystem.Application.Features.Owners.Interfaces;
using ArchiveSystem.Application.Features.Owners.Models;
using MediatR;

namespace ArchiveSystem.Application.Features.Owners.Commands.CreateOwner;

public sealed record CreateOwnerCommand(CreateOwnerDto Request) : IRequest<OwnerDto>;

public sealed class CreateOwnerCommandHandler : IRequestHandler<CreateOwnerCommand, OwnerDto>
{
    private readonly IOwnerRepository _repository;

    public CreateOwnerCommandHandler(IOwnerRepository repository)
    {
        _repository = repository;
    }

    public Task<OwnerDto> Handle(CreateOwnerCommand request, CancellationToken cancellationToken)
    {
        return _repository.CreateAsync(request.Request, cancellationToken);
    }
}
