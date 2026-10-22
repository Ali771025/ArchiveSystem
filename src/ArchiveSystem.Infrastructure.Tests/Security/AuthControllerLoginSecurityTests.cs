using ArchiveSystem.Application.Common.Interfaces.Security;
using ArchiveSystem.Infrastructure.Identity;
using ArchiveSystem.Infrastructure.Logging;
using ArchiveSystem.Presentation.Api.Controllers;
using MediatR;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace ArchiveSystem.Infrastructure.Tests.Security
{
    public class AuthControllerLoginSecurityTests
    {
        [Fact]
        public async Task Login_WithUnknownUsername_ReturnsUnauthorized()
        {
            var controller = CreateController();

            var result = await controller.Login(new LoginRequest
            {
                Username = "missing-user",
                Password = "CorrectPassword1!"
            });

            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            Assert.Equal("نام کاربری وارد شده یافت نشد.", GetMessage(unauthorized.Value));
        }

        [Fact]
        public async Task Login_WithWrongPassword_ReturnsUnauthorized()
        {
            var user = CreateUser();
            var controller = CreateController(
                configureUserManager: manager =>
                {
                    manager.Setup(x => x.FindByNameAsync("valid-user")).ReturnsAsync(user);
                },
                configureSignInManager: manager =>
                {
                    manager
                        .Setup(x => x.CheckPasswordSignInAsync(user, "WrongPassword1!", true))
                        .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);
                });

            var result = await controller.Login(new LoginRequest
            {
                Username = "valid-user",
                Password = "WrongPassword1!"
            });

            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            Assert.Equal("گذرواژه را درست وارد کنید.", GetMessage(unauthorized.Value));
        }

        [Fact]
        public async Task Login_WithEmailInUsernameField_DoesNotFallbackToEmailLookup()
        {
            var user = CreateUser();
            var controller = CreateController(
                configureUserManager: manager =>
                {
                    manager.Setup(x => x.FindByEmailAsync("valid@example.test")).ReturnsAsync(user);
                });

            var result = await controller.Login(new LoginRequest
            {
                Username = "valid@example.test",
                Password = "CorrectPassword1!"
            });

            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            Assert.Equal("نام کاربری وارد شده یافت نشد.", GetMessage(unauthorized.Value));
        }

        [Fact]
        public async Task Login_WithSqlInjectionIdentifier_ReturnsUnauthorized()
        {
            var controller = CreateController();

            var result = await controller.Login(new LoginRequest
            {
                Username = "' OR 1=1 --",
                Password = "CorrectPassword1!"
            });

            Assert.IsType<UnauthorizedObjectResult>(result.Result);
        }

        [Fact]
        public async Task Login_WhenIdentityLocksUserOut_ReturnsUnauthorized()
        {
            var user = CreateUser();
            var controller = CreateController(
                configureUserManager: manager =>
                {
                    manager.Setup(x => x.FindByNameAsync("valid-user")).ReturnsAsync(user);
                },
                configureSignInManager: manager =>
                {
                    manager
                        .Setup(x => x.CheckPasswordSignInAsync(user, "WrongPassword1!", true))
                        .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);
                });

            var result = await controller.Login(new LoginRequest
            {
                Username = "valid-user",
                Password = "WrongPassword1!"
            });

            Assert.IsType<UnauthorizedObjectResult>(result.Result);
        }

        [Fact]
        public async Task Login_WithBlankFields_ReturnsBadRequest()
        {
            var controller = CreateController();

            var result = await controller.Login(new LoginRequest
            {
                Username = "   ",
                Password = "   "
            });

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        private static AuthController CreateController(
            Action<Mock<UserManager<ApplicationUser>>>? configureUserManager = null,
            Action<Mock<SignInManager<ApplicationUser>>>? configureSignInManager = null)
        {
            var userManager = CreateUserManager();
            userManager
                .Setup(x => x.FindByNameAsync(It.IsAny<string>()))
                .ReturnsAsync((ApplicationUser?)null);
            userManager
                .Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((ApplicationUser?)null);
            configureUserManager?.Invoke(userManager);

            var signInManager = CreateSignInManager(userManager.Object);
            configureSignInManager?.Invoke(signInManager);

            var controller = new AuthController(
                userManager.Object,
                signInManager.Object,
                Options.Create(new JwtSettings
                {
                    Secret = "test-secret-test-secret-test-secret-32",
                    Issuer = "ArchiveSystem.Tests",
                    Audience = "ArchiveSystem.Tests",
                    ExpirationMinutes = 60
                }),
                Mock.Of<ILogger<AuthController>>(),
                new CapturingSecurityEventLogger(),
                Mock.Of<IMediator>(),
                Mock.Of<IAntiforgery>(),
                Mock.Of<IIdentityManagementService>(),
                Mock.Of<IDistributedCache>())
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            return controller;
        }

        private static Mock<UserManager<ApplicationUser>> CreateUserManager()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            return new Mock<UserManager<ApplicationUser>>(
                store.Object,
                Options.Create(new IdentityOptions()),
                Mock.Of<IPasswordHasher<ApplicationUser>>(),
                Array.Empty<IUserValidator<ApplicationUser>>(),
                Array.Empty<IPasswordValidator<ApplicationUser>>(),
                Mock.Of<ILookupNormalizer>(),
                new IdentityErrorDescriber(),
                Mock.Of<IServiceProvider>(),
                Mock.Of<ILogger<UserManager<ApplicationUser>>>());
        }

        private static Mock<SignInManager<ApplicationUser>> CreateSignInManager(UserManager<ApplicationUser> userManager)
        {
            return new Mock<SignInManager<ApplicationUser>>(
                userManager,
                Mock.Of<IHttpContextAccessor>(),
                Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>(),
                Options.Create(new IdentityOptions()),
                Mock.Of<ILogger<SignInManager<ApplicationUser>>>(),
                Mock.Of<IAuthenticationSchemeProvider>(),
                Mock.Of<IUserConfirmation<ApplicationUser>>());
        }

        private static ApplicationUser CreateUser()
        {
            return new ApplicationUser
            {
                Id = 101,
                UserName = "valid-user",
                Email = "valid@example.test",
                FirstName = "Valid",
                LastName = "User",
                IsActive = true
            };
        }

        private static string? GetMessage(object? response)
        {
            return response?.GetType().GetProperty("Message")?.GetValue(response)?.ToString();
        }

        private sealed class CapturingSecurityEventLogger : ISecurityEventLogger
        {
            public Task LogAsync(
                string eventType,
                string description,
                string? actorUserId,
                string? targetUserId,
                object? metadata = null,
                CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }
        }
    }
}
