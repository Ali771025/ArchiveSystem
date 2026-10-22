using ArchiveSystem.Application.Features.UserManagement.Common;
using ArchiveSystem.Application.Features.UserManagement.Interfaces;
using MediatR;

namespace ArchiveSystem.Application.Features.UserManagement.Queries.GetUsersWithPagination
{
    public sealed class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDetailsDto?>
    {
        private readonly IUserManagementService _userManagementService;

        public GetUserByIdQueryHandler(IUserManagementService userManagementService)
        {
            _userManagementService = userManagementService;
        }

        public Task<UserDetailsDto?> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
        {
            return _userManagementService.GetUserByIdAsync(request.UserId, cancellationToken);
        }
    }
}
