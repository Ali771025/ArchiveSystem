using System.Collections.Generic;

namespace ArchiveSystem.Application.Features.Authentication.DTOs
{
    public sealed record RegisterRequestDto(string Username, string Password, string ConfirmPassword);

    public sealed record LoginRequestDto(string Username, string Password);

    public sealed record ForgotPasswordRequestDto(string UsernameOrEmail);

    public sealed record AuthenticatedUserDto(
        long Id,
        string Username,
        string Email,
        IReadOnlyCollection<string> Roles);

    public sealed record AuthenticationResultDto(
        bool Succeeded,
        string Message,
        string? AccessToken = null,
        AuthenticatedUserDto? User = null,
        IReadOnlyCollection<string>? Errors = null);

    public sealed record ForgotPasswordResultDto(
        bool Succeeded,
        string Message,
        IReadOnlyCollection<string>? Errors = null);
}
