using ArchiveSystem.Application.Features.Owners.Interfaces;
using ArchiveSystem.Application.Features.Owners.Models;
using MediatR;

namespace ArchiveSystem.Application.Features.Owners.Queries.GetOwnerById;

public sealed record GetOwnerByIdQuery(long OwnerId) : IRequest<OwnerDto?>;

public sealed class GetOwnerByIdQueryHandler : IRequestHandler<GetOwnerByIdQuery, OwnerDto?>
{
    private readonly IOwnerRepository _repository;

    public GetOwnerByIdQueryHandler(IOwnerRepository repository)
    {
        _repository = repository;
    }

    public Task<OwnerDto?> Handle(GetOwnerByIdQuery request, CancellationToken cancellationToken)
    {
        return _repository.GetByIdAsync(request.OwnerId, cancellationToken);
    }
}
