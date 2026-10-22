using System.Reflection;
using ArchiveSystem.Application.Authorization;
using ArchiveSystem.Presentation.Api.Authorization;
using ArchiveSystem.Presentation.Api.Controllers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace ArchiveSystem.Infrastructure.Tests.Security
{
    public class ControllerAuthorizationSecurityTests
    {
        [Fact]
        public void FilesController_DownloadEndpointsRequireAuthenticatedReadPermission()
        {
            Assert.NotNull(typeof(FilesController).GetCustomAttribute<AuthorizeAttribute>());

            AssertRequiresPermission(
                typeof(FilesController).GetMethod(nameof(FilesController.DownloadLatest))!,
                Permissions.Files.Read);
            AssertRequiresPermission(
                typeof(FilesController).GetMethod(nameof(FilesController.DownloadVersion))!,
                Permissions.Files.Read);
            AssertRequiresPermission(
                typeof(AdvancedFilesController).GetMethod(nameof(AdvancedFilesController.SearchUsers))!,
                Permissions.Files.Read);
        }

        [Fact]
        public void FilesController_WriteEndpointsRequireOperationSpecificPermissions()
        {
            AssertRequiresPermission(
                typeof(FilesController).GetMethod(nameof(FilesController.SoftDeleteFile))!,
                Permissions.Files.Delete);
            AssertRequiresPermission(
                typeof(FilesController).GetMethod(nameof(FilesController.SoftDeleteVersion))!,
                Permissions.Files.Delete);
            AssertRequiresPermission(
                typeof(FilesController).GetMethod(nameof(FilesController.SetFileTags))!,
                Permissions.Files.Manage);
            AssertRequiresPermission(
                typeof(FilesController).GetMethod(nameof(FilesController.SetFileTags))!,
                Permissions.Tags.Manage);
            AssertRequiresPermission(
                typeof(AdminController).GetMethod(nameof(AdminController.CreateTag))!,
                Permissions.Tags.Manage);
        }

        [Fact]
        public void SearchController_RequiresJwtAuthentication()
        {
            var authorize = typeof(SearchController).GetCustomAttribute<AuthorizeAttribute>();

            Assert.NotNull(authorize);
            Assert.Equal(JwtBearerDefaults.AuthenticationScheme, authorize!.AuthenticationSchemes);
        }

        [Fact]
        public void CasesController_ReadAndWriteEndpointsUseAuthorizationPolicies()
        {
            Assert.Equal("CaseReadPolicy", GetAuthorizePolicy(nameof(CasesController.GetById)));
            Assert.Equal("CaseWritePolicy", GetAuthorizePolicy(nameof(CasesController.Create)));
            Assert.Equal("CaseWritePolicy", GetAuthorizePolicy(nameof(CasesController.Update)));
            Assert.Equal("CaseWritePolicy", GetAuthorizePolicy(nameof(CasesController.Delete)));
            Assert.Equal("CaseWritePolicy", GetAuthorizePolicy(nameof(CasesController.Move)));
        }

        [Fact]
        public void ClassificationsController_ReadEndpointsUseAuthenticatedAclQueries()
        {
            Assert.NotNull(typeof(ClassificationsController).GetCustomAttribute<AuthorizeAttribute>());
            Assert.Null(typeof(ClassificationsController).GetMethod(nameof(ClassificationsController.GetTree))!
                .GetCustomAttribute<RequirePermissionAttribute>());
            Assert.Null(typeof(ClassificationsController).GetMethod(nameof(ClassificationsController.Search))!
                .GetCustomAttribute<RequirePermissionAttribute>());
            Assert.Null(typeof(ClassificationsController).GetMethod(nameof(ClassificationsController.GetWritableIds))!
                .GetCustomAttribute<RequirePermissionAttribute>());
            Assert.Null(typeof(ClassificationsController).GetMethod(nameof(ClassificationsController.GetDynamicTemplate))!
                .GetCustomAttribute<RequirePermissionAttribute>());
        }

        [Fact]
        public void ManagementControllers_RequireAssignableModulePermissions()
        {
            AssertControllerRequiresPermission(typeof(UsersController), Permissions.Users.Manage);
            AssertControllerRequiresPermission(typeof(TaxpayersController), Permissions.Taxpayers.Manage);
            AssertControllerRequiresPermission(typeof(CaseLoansController), Permissions.CaseLoans.Manage);
            AssertControllerRequiresPermission(typeof(BehsazIntegrationController), Permissions.Behsaz.Search);
        }

        [Fact]
        public void OwnerMutations_RequireTaxpayerManagementPermission()
        {
            AssertRequiresPermission(
                typeof(OwnersController).GetMethod(nameof(OwnersController.CreateOwner))!,
                Permissions.Taxpayers.Manage);
            AssertRequiresPermission(
                typeof(OwnersController).GetMethod(nameof(OwnersController.UpdateOwner))!,
                Permissions.Taxpayers.Manage);
        }

        [Fact]
        public void DossierOwnerCandidateSearch_RequiresDossierPermissionOnly()
        {
            AssertControllerRequiresPermission(typeof(DossiersController), Permissions.Dossiers.Manage);
            Assert.Null(
                typeof(DossiersController)
                    .GetMethod(nameof(DossiersController.GetOwnerCandidates))!
                    .GetCustomAttribute<RequirePermissionAttribute>());
        }

        [Fact]
        public void DynamicTemplateWrite_RequiresDynamicFieldsPermission()
        {
            AssertRequiresPermission(
                typeof(ClassificationsController).GetMethod(nameof(ClassificationsController.UpsertDynamicTemplate))!,
                Permissions.DynamicFields.Manage);
        }

        private static void AssertRequiresPermission(MethodInfo method, string permission)
        {
            var attributes = method
                .GetCustomAttributes<RequirePermissionAttribute>()
                .ToArray();

            Assert.Contains(
                attributes,
                attribute => string.Equals(
                    attribute.Permission,
                    permission,
                    StringComparison.OrdinalIgnoreCase));
        }

        private static void AssertControllerRequiresPermission(Type controllerType, string permission)
        {
            var jwtAuthorize = controllerType
                .GetCustomAttributes<AuthorizeAttribute>()
                .Single(attribute => attribute.AuthenticationSchemes == JwtBearerDefaults.AuthenticationScheme);
            var permissionAttribute = controllerType.GetCustomAttribute<RequirePermissionAttribute>();

            Assert.NotNull(jwtAuthorize);
            Assert.True(string.IsNullOrWhiteSpace(jwtAuthorize.Roles));
            Assert.NotNull(permissionAttribute);
            Assert.Equal(permission, permissionAttribute!.Permission);
        }

        private static string? GetAuthorizePolicy(string methodName)
        {
            return typeof(CasesController)
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Single(method => method.Name == methodName)
                .GetCustomAttribute<AuthorizeAttribute>()
                ?.Policy;
        }
    }
}
