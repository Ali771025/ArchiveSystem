using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ArchiveSystem.Application.Features.UserManagement.DTOs;

namespace ArchiveSystem.Application.Features.UserManagement.Interfaces
{
    public interface IUserRoleManagementService
    {
        Task<IReadOnlyCollection<UserRolesDto>> GetUsersWithRolesAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<string>> GetAvailableRolesAsync(CancellationToken cancellationToken = default);
        Task<UserRolesDto> AssignRolesAsync(AssignUserRolesRequestDto request, CancellationToken cancellationToken = default);
    }
}
