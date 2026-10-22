using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArchiveSystem.Application.Common.Interfaces.Security;
using ArchiveSystem.Application.Features.Authentication.DTOs;
using ArchiveSystem.Application.Features.Authentication.Interfaces;

namespace ArchiveSystem.Application.Features.Authentication.Services
{
    public sealed class AuthenticationService : IAuthenticationService
    {
        private static readonly string[] DefaultRoles = { "Viewer" };
        private const string InvalidUsernameMessage = "نام کاربری وارد شده یافت نشد.";
        private const string InvalidPasswordMessage = "گذرواژه را درست وارد کنید.";
        private const string ForgotPasswordMessage = "اگر حساب کاربری وجود داشته باشد، راهنمای بازیابی ارسال می شود.";

        private readonly IIdentityService _identityService;

        public AuthenticationService(IIdentityService identityService)
        {
            _identityService = identityService;
        }

        public async Task<AuthenticationResultDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default)
        {
            var username = request.Username.Trim();
            if (await _identityService.UsernameExistsAsync(username, cancellationToken))
            {
                return new AuthenticationResultDto(
                    false,
                    "نام کاربری وارد شده قبلا ثبت شده است.",
                    Errors: new[] { "USERNAME_ALREADY_EXISTS" });
            }

            var createResult = await _identityService.CreateUserAsync(
                username,
                request.Password,
                DefaultRoles,
                cancellationToken);

            if (!createResult.Succeeded)
            {
                return new AuthenticationResultDto(
                    false,
                    "ثبت نام انجام نشد.",
                    Errors: createResult.Errors);
            }

            var user = await _identityService.FindByUsernameOrEmailAsync(username, cancellationToken);
            if (user is null)
            {
                return new AuthenticationResultDto(
                    false,
                    "کاربر ایجاد شد اما امکان بارگذاری اطلاعات او وجود ندارد.",
                    Errors: new[] { "USER_LOAD_FAILED" });
            }

            var token = await _identityService.CreateAccessTokenAsync(user, cancellationToken);
            var roles = await _identityService.GetUserRolesAsync(user.Id, cancellationToken);

            return new AuthenticationResultDto(
                true,
                "ثبت نام با موفقیت انجام شد.",
                AccessToken: token,
                User: new AuthenticatedUserDto(
                    user.Id,
                    user.Username,
                    user.Email,
                    roles.OrderBy(x => x).ToArray()));
        }

        public async Task<AuthenticationResultDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
        {
            var user = await _identityService.FindByUsernameOrEmailAsync(request.Username.Trim(), cancellationToken);
            if (user is null)
            {
                return new AuthenticationResultDto(false, InvalidUsernameMessage, Errors: new[] { "USERNAME_NOT_FOUND" });
            }

            if (!user.IsActive)
            {
                return new AuthenticationResultDto(false, "حساب کاربری شما غیرفعال است.", Errors: new[] { "ACCOUNT_DEACTIVATED" });
            }

            var validPassword = await _identityService.ValidatePasswordAsync(user.Id, request.Password, cancellationToken);
            if (!validPassword)
            {
                return new AuthenticationResultDto(false, InvalidPasswordMessage, Errors: new[] { "INVALID_PASSWORD" });
            }

            var token = await _identityService.CreateAccessTokenAsync(user, cancellationToken);
            var roles = await _identityService.GetUserRolesAsync(user.Id, cancellationToken);

            return new AuthenticationResultDto(
                true,
                "ورود با موفقیت انجام شد.",
                AccessToken: token,
                User: new AuthenticatedUserDto(
                    user.Id,
                    user.Username,
                    user.Email,
                    roles.OrderBy(x => x).ToArray()));
        }

        public async Task<ForgotPasswordResultDto> ForgotPasswordAsync(ForgotPasswordRequestDto request, CancellationToken cancellationToken = default)
        {
            var usernameOrEmail = request.UsernameOrEmail.Trim();
            if (string.IsNullOrWhiteSpace(usernameOrEmail))
            {
                return new ForgotPasswordResultDto(false, "نام کاربری یا ایمیل را وارد کنید.", new[] { "USERNAME_OR_EMAIL_REQUIRED" });
            }

            _ = await _identityService.StartForgotPasswordAsync(usernameOrEmail, cancellationToken);
            return new ForgotPasswordResultDto(true, ForgotPasswordMessage);
        }
    }
}
