using ArchiveSystem.Application.Common.Models;
using ArchiveSystem.Application.Features.Owners.Interfaces;
using ArchiveSystem.Application.Features.Owners.Models;
using MediatR;

namespace ArchiveSystem.Application.Features.Owners.Queries.GetOwners;

public sealed record GetOwnersQuery(int Page, int PageSize) : IRequest<PaginatedResult<OwnerDto>>;

public sealed class GetOwnersQueryHandler : IRequestHandler<GetOwnersQuery, PaginatedResult<OwnerDto>>
{
    private readonly IOwnerRepository _repository;

    public GetOwnersQueryHandler(IOwnerRepository repository)
    {
        _repository = repository;
    }

    public Task<PaginatedResult<OwnerDto>> Handle(GetOwnersQuery request, CancellationToken cancellationToken)
    {
        return _repository.GetPagedAsync(request.Page, request.PageSize, cancellationToken);
    }
}
