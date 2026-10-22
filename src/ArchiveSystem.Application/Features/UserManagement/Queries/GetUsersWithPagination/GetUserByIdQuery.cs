using ArchiveSystem.Application.Features.UserManagement.Common;
using MediatR;

namespace ArchiveSystem.Application.Features.UserManagement.Queries.GetUsersWithPagination
{
    public sealed record GetUserByIdQuery(long UserId) : IRequest<UserDetailsDto?>;
}
