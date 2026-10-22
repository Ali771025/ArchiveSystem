using ArchiveSystem.Application.Features.UserManagement.Common;

namespace ArchiveSystem.Application.Features.UserManagement.Interfaces
{
    public interface IUserManagementService
    {
        Task<PagedResponse<UserListItemDto>> GetUsersWithPaginationAsync(
            int pageNumber,
            int pageSize,
            string? sortBy,
            bool sortDescending,
            string? username,
            string? name,
            string? firstName,
            string? lastName,
            string? nationalId,
            string? columnFilterField,
            string? columnFilterOperator,
            string? columnFilterValue,
            CancellationToken cancellationToken = default);

        Task<UserDetailsDto?> GetUserByIdAsync(long userId, CancellationToken cancellationToken = default);

        Task<UserDetailsDto> CreateUserAsync(CreateUserManagementRequest request, CancellationToken cancellationToken = default);

        Task<UserDetailsDto?> UpdateUserAsync(UpdateUserManagementRequest request, CancellationToken cancellationToken = default);

        Task<bool> ResetPasswordAsync(
            long userId,
            string newPassword,
            string? modifiedBy,
            CancellationToken cancellationToken = default);

        Task<bool> SetUserStatusAsync(
            long userId,
            bool isActive,
            string? modifiedBy,
            CancellationToken cancellationToken = default);

        Task<bool> AssignRolesAsync(
            long userId,
            IReadOnlyCollection<string> roles,
            string? modifiedBy,
            CancellationToken cancellationToken = default);
    }

    public sealed record CreateUserManagementRequest(
        string Username,
        string Email,
        string? PhoneNumber,
        string FirstName,
        string LastName,
        string? NationalId,
        string? PersonnelCode,
        TimeSpan ActiveDuration,
        bool AutoBackupEnabled,
        bool TwoFactorEnabled,
        string Password,
        IReadOnlyCollection<string> Roles,
        string? CreatedBy);

    public sealed record UpdateUserManagementRequest(
        long UserId,
        string Username,
        string Email,
        string? PhoneNumber,
        string FirstName,
        string LastName,
        string? NationalId,
        string? PersonnelCode,
        TimeSpan ActiveDuration,
        bool AutoBackupEnabled,
        bool TwoFactorEnabled,
        string? LastModifiedBy);
}
