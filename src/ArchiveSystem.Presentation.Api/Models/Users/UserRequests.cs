namespace ArchiveSystem.Presentation.Api.Models.Users
{
    public sealed class CreateUserRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? NationalId { get; set; }
        public string? PersonnelCode { get; set; }
        public TimeSpan ActiveDuration { get; set; }
        public bool AutoBackupEnabled { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public string Password { get; set; } = string.Empty;
        public IReadOnlyCollection<string> Roles { get; set; } = Array.Empty<string>();
    }

    public sealed class UpdateUserRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? NationalId { get; set; }
        public string? PersonnelCode { get; set; }
        public TimeSpan ActiveDuration { get; set; }
        public bool AutoBackupEnabled { get; set; }
        public bool TwoFactorEnabled { get; set; }
    }

    public sealed class ToggleUserStatusRequest
    {
        public bool IsActive { get; set; }
    }

    public sealed class ResetUserPasswordRequest
    {
        public string NewPassword { get; set; } = string.Empty;
    }

    public sealed class AssignRolesRequest
    {
        public IReadOnlyCollection<string> Roles { get; set; } = Array.Empty<string>();
    }
}
