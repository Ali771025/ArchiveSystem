using ArchiveSystem.Application.Features.UserManagement.Common;
using ArchiveSystem.Application.Features.UserManagement.Interfaces;
using MediatR;

namespace ArchiveSystem.Application.Features.UserManagement.Queries.GetUsersWithPagination
{
    public sealed class GetUsersWithPaginationQueryHandler : IRequestHandler<GetUsersWithPaginationQuery, PagedResponse<UserListItemDto>>
    {
        private readonly IUserManagementService _userManagementService;

        public GetUsersWithPaginationQueryHandler(IUserManagementService userManagementService)
        {
            _userManagementService = userManagementService;
        }

        public Task<PagedResponse<UserListItemDto>> Handle(GetUsersWithPaginationQuery request, CancellationToken cancellationToken)
        {
            return _userManagementService.GetUsersWithPaginationAsync(
                request.PageNumber,
                request.PageSize,
                request.SortBy,
                request.SortDescending,
                request.Username,
                request.Name,
                request.FirstName,
                request.LastName,
                request.NationalId,
                request.ColumnFilterField,
                request.ColumnFilterOperator,
                request.ColumnFilterValue,
                cancellationToken);
        }
    }
}
