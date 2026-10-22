// src/ArchiveSystem.Presentation.Api/Controllers/AuthController.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MediatR;
using ArchiveSystem.Application.Features.Authentication.Commands.RequestLoginOtp;
using ArchiveSystem.Application.Features.Authentication.Commands.RequestPasswordResetOtp;
using ArchiveSystem.Application.Features.Authentication.Commands.ResetPasswordWithOtp;
using ArchiveSystem.Application.Features.Authentication.Commands.VerifyOtpAndLogin;
using ArchiveSystem.Application.Authorization;
using ArchiveSystem.Application.Common.Interfaces.Security;
using ArchiveSystem.Infrastructure.Identity;
using ArchiveSystem.Infrastructure.Logging;
using ArchiveSystem.Presentation.Api.Models;

namespace ArchiveSystem.Presentation.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<AuthController> _logger;
        private readonly ISecurityEventLogger _securityEventLogger;
        private readonly IMediator _mediator;
        private readonly IAntiforgery _antiforgery;
        private readonly IIdentityManagementService _identityManagementService;
        private readonly IDistributedCache _sessionCache;
        private const string SecurityStampClaimType = "security_stamp";

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IOptions<JwtSettings> jwtSettings,
            ILogger<AuthController> logger,
            ISecurityEventLogger securityEventLogger,
            IMediator mediator,
            IAntiforgery antiforgery,
            IIdentityManagementService identityManagementService,
            IDistributedCache sessionCache)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtSettings = jwtSettings.Value;
            _logger = logger;
            _securityEventLogger = securityEventLogger;
            _mediator = mediator;
            _antiforgery = antiforgery;
            _identityManagementService = identityManagementService;
            _sessionCache = sessionCache;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
        {
            try
            {
                var normalizedUsername = request.Username.Trim();
                var normalizedEmail = request.Email.Trim();

                var existingUser = await _userManager.FindByNameAsync(normalizedUsername);
                if (existingUser != null)
                {
                    return BadRequest(new { Message = "نام کاربری وارد شده قبلا ثبت شده است." });
                }

                var user = new ApplicationUser
                {
                    UserName = normalizedUsername,
                    Email = normalizedEmail,
                    FirstName = request.FirstName.Trim(),
                    LastName = request.LastName.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    LockoutEnabled = true
                };

                var result = await _userManager.CreateAsync(user, request.Password);
                
                if (!result.Succeeded)
                {
                    return BadRequest(new { Message = "ثبت نام انجام نشد.", Errors = result.Errors.Select(e => e.Description) });
                }

                // Add user to default role
                await _userManager.AddToRoleAsync(user, "Viewer");

                await _userManager.UpdateSecurityStampAsync(user);
                var token = await GenerateJwtTokenAsync(user);
                var permissions = await _identityManagementService.GetUserPermissionClaimsAsync(user.Id);
                return Ok(new AuthResponse
                {
                    Token = token,
                    User = new UserInfoDto
                    {
                        Id = user.Id,
                        Username = user.UserName!,
                        Email = user.Email!,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Roles = new List<string> { "Viewer" },
                        Permissions = permissions.ToList()
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for user {Username}", request.Username);
                return StatusCode(500, new { Message = "خطایی هنگام ثبت نام رخ داد." });
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var loginIdentifiers = ResolveLoginIdentifiers(request);
                if (loginIdentifiers.Count == 0 || string.IsNullOrWhiteSpace(request.Password))
                {
                    return BadRequest(new { Message = "نام کاربری و گذرواژه را وارد کنید." });
                }

                ApplicationUser? user = null;
                var attemptedIdentifier = loginIdentifiers[0].Value;

                foreach (var candidate in loginIdentifiers)
                {
                    user = candidate.Kind switch
                    {
                        LoginIdentifierKind.Username => await _userManager.FindByNameAsync(candidate.Value),
                        LoginIdentifierKind.Email => await _userManager.FindByEmailAsync(candidate.Value),
                        _ => await FindByUsernameOrEmailAsync(candidate.Value)
                    };

                    if (user is not null)
                    {
                        attemptedIdentifier = candidate.Value;
                        break;
                    }
                }

                if (user == null)
                {
                    await _securityEventLogger.LogAsync(
                        eventType: "LoginFailed",
                        description: $"Failed login attempt for unknown user '{attemptedIdentifier}'.",
                        actorUserId: null,
                        targetUserId: null,
                        metadata: new { Username = attemptedIdentifier, Candidates = loginIdentifiers.Select(x => x.Value).ToArray() });
                    return Unauthorized(new { Message = "نام کاربری وارد شده یافت نشد." });
                }

                if (!user.IsActive)
                {
                    await _securityEventLogger.LogAsync(
                        eventType: "LoginFailed",
                        description: $"Failed login attempt for deactivated user '{user.UserName}'.",
                        actorUserId: user.Id.ToString(),
                        targetUserId: user.Id.ToString(),
                        metadata: new { Username = user.UserName, Reason = "Deactivated" });
                    return Unauthorized(new { Message = "حساب کاربری شما غیرفعال است." });
                }

                if (!await _userManager.GetLockoutEnabledAsync(user))
                {
                    await _userManager.SetLockoutEnabledAsync(user, true);
                }

                if (await _userManager.IsLockedOutAsync(user))
                {
                    await _securityEventLogger.LogAsync(
                        eventType: "LoginBlocked",
                        description: $"Locked account login attempt for user '{user.UserName}'.",
                        actorUserId: user.Id.ToString(),
                        targetUserId: user.Id.ToString(),
                        metadata: new { Username = user.UserName, user.LockoutEnd });
                    return StatusCode(StatusCodes.Status423Locked, new { Message = "حساب کاربری به دلیل تلاش‌های ناموفق مکرر موقتاً قفل شده است. لطفاً بعداً دوباره تلاش کنید." });
                }

                var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
                
                if (result.IsLockedOut)
                {
                    await _securityEventLogger.LogAsync(
                        eventType: "LoginBlocked",
                        description: $"User '{user.UserName}' was locked after repeated failed login attempts.",
                        actorUserId: user.Id.ToString(),
                        targetUserId: user.Id.ToString(),
                        metadata: new { Username = user.UserName, user.LockoutEnd });
                    return StatusCode(StatusCodes.Status423Locked, new { Message = "حساب کاربری به دلیل تلاش‌های ناموفق مکرر موقتاً قفل شد. لطفاً بعداً دوباره تلاش کنید." });
                }

                if (!result.Succeeded)
                {
                    await _securityEventLogger.LogAsync(
                        eventType: "LoginFailed",
                        description: $"Failed login attempt for user '{user.UserName}'.",
                        actorUserId: user.Id.ToString(),
                        targetUserId: user.Id.ToString(),
                        metadata: new { Username = user.UserName, result.IsLockedOut });
                    return Unauthorized(new { Message = "گذرواژه را درست وارد کنید." });
                }

                if (user.TwoFactorEnabled)
                {
                    var normalizedMobile = NormalizeIranianMobile(user.PhoneNumber);
                    if (!IsValidIranianMobile(normalizedMobile))
                    {
                        return BadRequest(new
                        {
                            Message = "برای ورود دومرحله‌ای، شماره همراه معتبر برای این کاربر ثبت نشده است. لطفاً با مدیر سامانه تماس بگیرید."
                        });
                    }

                    var correlationId = HttpContext.Items[ArchiveSystem.Presentation.Api.Middleware.CorrelationIdMiddleware.ItemKey]?.ToString()
                        ?? HttpContext.TraceIdentifier;
                    var otpResult = await _mediator.Send(
                        new RequestLoginOtpCommand(
                            normalizedMobile,
                            HttpContext.Connection.RemoteIpAddress?.ToString(),
                            Request.Headers.UserAgent.ToString(),
                            correlationId),
                        cancellationToken);

                    if (!otpResult.Accepted)
                    {
                        if (otpResult.RetryAfterSeconds.HasValue)
                        {
                            Response.Headers.RetryAfter = otpResult.RetryAfterSeconds.Value.ToString();
                        }

                        var statusCode = otpResult.RateLimited
                            ? StatusCodes.Status429TooManyRequests
                            : StatusCodes.Status502BadGateway;
                        return StatusCode(statusCode, new
                        {
                            Message = otpResult.Message,
                            otpResult.RetryAfterSeconds,
                            otpResult.NextAllowedAtUtc
                        });
                    }

                    await _securityEventLogger.LogAsync(
                        eventType: "TwoFactorChallenge",
                        description: $"Two-factor challenge issued for user '{user.UserName}'.",
                        actorUserId: user.Id.ToString(),
                        targetUserId: user.Id.ToString(),
                        metadata: new { Username = user.UserName });

                    return Ok(new AuthResponse
                    {
                        RequiresTwoFactor = true,
                        MobileNumber = normalizedMobile,
                        OtpExpiresAtUtc = otpResult.NextAllowedAtUtc ?? DateTimeOffset.UtcNow.AddMinutes(2)
                    });
                }

                await _userManager.UpdateSecurityStampAsync(user);
                var token = await GenerateJwtTokenAsync(user);
                var roles = await _userManager.GetRolesAsync(user);
                var permissions = await _identityManagementService.GetUserPermissionClaimsAsync(user.Id);
                await _securityEventLogger.LogAsync(
                    eventType: "LoginSuccess",
                    description: $"User '{user.UserName}' authenticated successfully.",
                    actorUserId: user.Id.ToString(),
                    targetUserId: user.Id.ToString(),
                    metadata: new { Username = user.UserName, Roles = roles.ToArray() });

                return Ok(new AuthResponse
                {
                    Token = token,
                    User = new UserInfoDto
                    {
                        Id = user.Id,
                        Username = user.UserName!,
                        Email = user.Email!,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Roles = roles.ToList(),
                        Permissions = permissions.ToList()
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user {Username}", request.Username);
                return StatusCode(500, new { Message = "خطایی هنگام ورود رخ داد." });
            }
        }

        [HttpPost("refresh")]
        public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshRequest request)
        {
            try
            {
                var principal = GetPrincipalFromExpiredToken(request.Token);
                if (principal == null)
                {
                    return Unauthorized(new { Message = "نشست شما معتبر نیست. لطفا دوباره وارد شوید." });
                }

                var username = principal.Identity?.Name;
                if (string.IsNullOrWhiteSpace(username))
                {
                    return Unauthorized(new { Message = "نشست شما معتبر نیست. لطفا دوباره وارد شوید." });
                }

                var user = await _userManager.FindByNameAsync(username);
                if (user == null)
                {
                    return Unauthorized(new { Message = "نشست شما معتبر نیست. لطفا دوباره وارد شوید." });
                }

                var tokenSecurityStamp = principal.FindFirstValue(SecurityStampClaimType);
                var currentSecurityStamp = await _userManager.GetSecurityStampAsync(user);
                if (string.IsNullOrWhiteSpace(tokenSecurityStamp) ||
                    !string.Equals(tokenSecurityStamp, currentSecurityStamp, StringComparison.Ordinal))
                {
                    return Unauthorized(new { Message = "نشست شما منقضی شده است. لطفا دوباره وارد شوید." });
                }

                var newToken = await GenerateJwtTokenAsync(user);
                var roles = await _userManager.GetRolesAsync(user);
                var permissions = await _identityManagementService.GetUserPermissionClaimsAsync(user.Id);

                return Ok(new AuthResponse
                {
                    Token = newToken,
                    User = new UserInfoDto
                    {
                        Id = user.Id,
                        Username = user.UserName!,
                        Email = user.Email!,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Roles = roles.ToList(),
                        Permissions = permissions.ToList()
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                return StatusCode(500, new { Message = "خطایی هنگام تمدید نشست رخ داد." });
            }
        }

        [AllowAnonymous]
        [HttpGet("/api/v1/auth/csrf-token")]
        public ActionResult<CsrfTokenResponse> GetCsrfToken()
        {
            var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
            return Ok(new CsrfTokenResponse(tokens.RequestToken ?? string.Empty));
        }

        [AllowAnonymous]
        [EnableRateLimiting("otp-request")]
        [HttpPost("/api/v1/auth/request-otp")]
        public async Task<ActionResult<ApiResult<RequestLoginOtpResult>>> RequestOtp(
            [FromBody] RequestOtpApiRequest request,
            CancellationToken cancellationToken)
        {
            await _antiforgery.ValidateRequestAsync(HttpContext);

            var correlationId = HttpContext.Items[ArchiveSystem.Presentation.Api.Middleware.CorrelationIdMiddleware.ItemKey]?.ToString()
                ?? HttpContext.TraceIdentifier;
            var commandResult = await _mediator.Send(
                new RequestLoginOtpCommand(
                    request.MobileNumber,
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    Request.Headers.UserAgent.ToString(),
                    correlationId),
                cancellationToken);

            if (!commandResult.Accepted)
            {
                if (commandResult.RetryAfterSeconds.HasValue)
                {
                    Response.Headers.RetryAfter = commandResult.RetryAfterSeconds.Value.ToString();
                }

                var statusCode = commandResult.RateLimited
                    ? StatusCodes.Status429TooManyRequests
                    : StatusCodes.Status502BadGateway;
                var errorCode = commandResult.RateLimited ? "OTP_RATE_LIMITED" : "SMS_PROVIDER_ERROR";
                return StatusCode(statusCode, new ApiResult<RequestLoginOtpResult>
                {
                    Success = false,
                    ErrorCode = errorCode,
                    Message = commandResult.Message,
                    Data = commandResult
                });
            }

            return Ok(ApiResult<RequestLoginOtpResult>.Ok(commandResult, commandResult.Message));
        }

        [AllowAnonymous]
        [EnableRateLimiting("otp-verify")]
        [HttpPost("/api/v1/auth/verify-otp-and-login")]
        public async Task<ActionResult<ApiResult<VerifyOtpAndLoginResult>>> VerifyOtpAndLogin(
            [FromBody] VerifyOtpAndLoginApiRequest request,
            CancellationToken cancellationToken)
        {
            await _antiforgery.ValidateRequestAsync(HttpContext);

            var result = await _mediator.Send(
                new VerifyOtpAndLoginCommand(request.MobileNumber, request.OtpCode),
                cancellationToken);

            if (!result.Succeeded)
            {
                return Unauthorized(ApiResult<VerifyOtpAndLoginResult>.Error("INVALID_OTP", result.Message));
            }

            return Ok(ApiResult<VerifyOtpAndLoginResult>.Ok(result, result.Message));
        }

        [AllowAnonymous]
        [EnableRateLimiting("otp-request")]
        [HttpPost("/api/v1/auth/request-password-reset-otp")]
        public async Task<ActionResult<ApiResult<RequestPasswordResetOtpResult>>> RequestPasswordResetOtp(
            [FromBody] RequestPasswordResetOtpApiRequest request,
            CancellationToken cancellationToken)
        {
            await _antiforgery.ValidateRequestAsync(HttpContext);

            var correlationId = HttpContext.Items[ArchiveSystem.Presentation.Api.Middleware.CorrelationIdMiddleware.ItemKey]?.ToString()
                ?? HttpContext.TraceIdentifier;
            var commandResult = await _mediator.Send(
                new RequestPasswordResetOtpCommand(
                    request.MobileNumber,
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    Request.Headers.UserAgent.ToString(),
                    correlationId),
                cancellationToken);

            if (!commandResult.Accepted)
            {
                var statusCode = commandResult.RateLimited
                    ? StatusCodes.Status429TooManyRequests
                    : StatusCodes.Status502BadGateway;
                var errorCode = commandResult.RateLimited ? "OTP_RATE_LIMITED" : "SMS_PROVIDER_ERROR";
                return StatusCode(statusCode, ApiResult<RequestPasswordResetOtpResult>.Error(errorCode, commandResult.Message));
            }

            return Ok(ApiResult<RequestPasswordResetOtpResult>.Ok(commandResult, commandResult.Message));
        }

        [AllowAnonymous]
        [EnableRateLimiting("otp-verify")]
        [HttpPost("/api/v1/auth/reset-password-with-otp")]
        public async Task<ActionResult<ApiResult<ResetPasswordWithOtpResult>>> ResetPasswordWithOtp(
            [FromBody] ResetPasswordWithOtpApiRequest request,
            CancellationToken cancellationToken)
        {
            await _antiforgery.ValidateRequestAsync(HttpContext);

            var result = await _mediator.Send(
                new ResetPasswordWithOtpCommand(request.MobileNumber, request.OtpCode, request.NewPassword),
                cancellationToken);

            if (!result.Succeeded)
            {
                return Unauthorized(ApiResult<ResetPasswordWithOtpResult>.Error("INVALID_PASSWORD_RESET_OTP", result.Message));
            }

            return Ok(ApiResult<ResetPasswordWithOtpResult>.Ok(result, result.Message));
        }

        private async Task<string> GenerateJwtTokenAsync(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var permissions = await _identityManagementService.GetUserPermissionClaimsAsync(user.Id);
            var securityStamp = await _userManager.GetSecurityStampAsync(user);
            await StoreActiveSessionAsync(user.Id, securityStamp);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName!),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(ClaimTypes.GivenName, user.FirstName),
                new Claim(ClaimTypes.Surname, user.LastName),
                new Claim(SecurityStampClaimType, securityStamp),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            foreach (var permission in permissions)
            {
                claims.Add(new Claim(Permissions.ClaimType, permission));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private async Task StoreActiveSessionAsync(long userId, string securityStamp)
        {
            var idleTimeoutMinutes = _jwtSettings.IdleTimeoutMinutes <= 0 ? 20 : _jwtSettings.IdleTimeoutMinutes;
            await _sessionCache.SetStringAsync(
                BuildSessionCacheKey(userId, securityStamp),
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                new DistributedCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromMinutes(idleTimeoutMinutes)
                });
        }

        private static string BuildSessionCacheKey(long userId, string securityStamp)
        {
            return $"auth:session:{userId}:{securityStamp}";
        }

        private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret)),
                ClockSkew = TimeSpan.Zero
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            SecurityToken securityToken;
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);
            var jwtSecurityToken = securityToken as JwtSecurityToken;

            if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                return null;

            return principal;
        }

        private async Task<ApplicationUser?> FindByUsernameOrEmailAsync(string identifier)
        {
            var user = await _userManager.FindByNameAsync(identifier);
            return user ?? await _userManager.FindByEmailAsync(identifier);
        }

        private static string NormalizeIranianMobile(string? input)
        {
            var compact = (input ?? string.Empty)
                .Replace(" ", string.Empty, StringComparison.Ordinal)
                .Replace("-", string.Empty, StringComparison.Ordinal)
                .Trim();

            if (compact.StartsWith("+98", StringComparison.Ordinal))
            {
                return "0" + compact[3..];
            }

            if (compact.StartsWith("98", StringComparison.Ordinal) && compact.Length == 12)
            {
                return "0" + compact[2..];
            }

            return compact;
        }

        private static bool IsValidIranianMobile(string mobileNumber)
        {
            return mobileNumber.Length == 11 &&
                   mobileNumber.StartsWith("09", StringComparison.Ordinal) &&
                   mobileNumber.All(char.IsDigit);
        }

        private static List<LoginIdentifier> ResolveLoginIdentifiers(LoginRequest request)
        {
            var candidates = new List<LoginIdentifier>(capacity: 3);
            AddCandidate(candidates, request.Username, LoginIdentifierKind.Username);
            AddCandidate(candidates, request.Email, LoginIdentifierKind.Email);
            AddCandidate(candidates, request.Identifier, LoginIdentifierKind.Any);
            return candidates;
        }

        private static void AddCandidate(List<LoginIdentifier> candidates, string? rawValue, LoginIdentifierKind kind)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return;
            }

            var normalized = rawValue.Trim();
            foreach (var existing in candidates)
            {
                if (existing.Kind == kind && string.Equals(existing.Value, normalized, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            candidates.Add(new LoginIdentifier(normalized, kind));
        }

        private readonly record struct LoginIdentifier(string Value, LoginIdentifierKind Kind);

        private enum LoginIdentifierKind
        {
            Username,
            Email,
            Any
        }
    }

    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        public string Identifier { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class RefreshRequest
    {
        public string Token { get; set; } = string.Empty;
    }

    public sealed class RequestOtpApiRequest
    {
        public string MobileNumber { get; set; } = string.Empty;
    }

    public sealed class VerifyOtpAndLoginApiRequest
    {
        public string MobileNumber { get; set; } = string.Empty;
        public string OtpCode { get; set; } = string.Empty;
    }

    public sealed class RequestPasswordResetOtpApiRequest
    {
        public string MobileNumber { get; set; } = string.Empty;
    }

    public sealed class ResetPasswordWithOtpApiRequest
    {
        public string MobileNumber { get; set; } = string.Empty;
        public string OtpCode { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public sealed class CsrfTokenResponse
    {
        public CsrfTokenResponse(string token)
        {
            Token = token;
        }

        public string Token { get; }
    }

    public class AuthResponse
    {
        public string? Token { get; set; }
        public UserInfoDto? User { get; set; }
        public bool RequiresTwoFactor { get; set; }
        public string? MobileNumber { get; set; }
        public DateTimeOffset? OtpExpiresAtUtc { get; set; }
    }

    public class UserInfoDto
    {
        public long Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
        public List<string> Permissions { get; set; } = new();
    }

    public class JwtSettings
    {
        public string Secret { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int ExpirationMinutes { get; set; } = 60;
        public int IdleTimeoutMinutes { get; set; } = 20;
    }
}
