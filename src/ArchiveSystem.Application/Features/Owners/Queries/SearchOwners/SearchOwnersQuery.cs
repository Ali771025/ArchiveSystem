using ArchiveSystem.Application.Features.Owners.Interfaces;
using ArchiveSystem.Application.Features.Owners.Models;
using MediatR;

namespace ArchiveSystem.Application.Features.Owners.Queries.SearchOwners;

public sealed record SearchOwnersQuery(string? Query, int Take) : IRequest<IReadOnlyCollection<OwnerSearchItemDto>>;

public sealed class SearchOwnersQueryHandler : IRequestHandler<SearchOwnersQuery, IReadOnlyCollection<OwnerSearchItemDto>>
{
    private readonly IOwnerRepository _repository;

    public SearchOwnersQueryHandler(IOwnerRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyCollection<OwnerSearchItemDto>> Handle(
        SearchOwnersQuery request,
        CancellationToken cancellationToken)
    {
        return _repository.SearchAsync(request.Query, request.Take, cancellationToken);
    }
}
