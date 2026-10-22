namespace ArchiveSystem.Application.Features.UserManagement.Common
{
    public sealed record UserListItemDto(
        long Id,
        string Username,
        string Email,
        string? PhoneNumber,
        string FirstName,
        string LastName,
        string? NationalId,
        string? PersonnelCode,
        bool IsActive,
        bool TwoFactorEnabled,
        bool AutoBackupEnabled,
        TimeSpan ActiveDuration);

    public sealed record UserDetailsDto(
        long Id,
        string Username,
        string Email,
        string? PhoneNumber,
        string FirstName,
        string LastName,
        string? NationalId,
        string? PersonnelCode,
        bool IsActive,
        bool TwoFactorEnabled,
        bool AutoBackupEnabled,
        TimeSpan ActiveDuration,
        string? CreatedBy,
        DateTime CreatedAt,
        string? LastModifiedBy,
        DateTime? LastModified);
}
