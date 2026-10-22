using ArchiveSystem.Application.AccessControl;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Domain.Enums;
using Moq;
using Xunit;

namespace ArchiveSystem.Application.Tests.AccessControl
{
    public class AccessControlPolicyEngineTests
    {
        private static readonly IReadOnlyList<ClassificationNode> Tree =
            new[]
            {
                new ClassificationNode(1, null),
                new ClassificationNode(2, 1),
                new ClassificationNode(3, 2),
                new ClassificationNode(4, null)
            };

        [Fact]
        public void Scenario01_DefaultDeny()
        {
            var effective = AccessControlPolicyEngine.ComputeEffectiveClassificationPermissions(Tree, Array.Empty<ClassificationAclEntry>());
            Assert.All(effective.Values, mask => Assert.Equal(ClassificationPermissionMask.None, mask));
        }

        [Fact]
        public void Scenario02_InheritanceAllow()
        {
            var entries = new[]
            {
                new ClassificationAclEntry(0, AccessSubjectType.User, 10, 1, ClassificationPermissionMask.Read)
            };

            var effective = AccessControlPolicyEngine.ComputeEffectiveClassificationPermissions(Tree, entries);
            Assert.Equal(ClassificationPermissionMask.Read, effective[1]);
            Assert.Equal(ClassificationPermissionMask.Read, effective[2]);
            Assert.Equal(ClassificationPermissionMask.Read, effective[3]);
            Assert.Equal(ClassificationPermissionMask.None, effective[4]);
        }

        [Fact]
        public void Scenario03_ExplicitAllowOnChild()
        {
            var entries = new[]
            {
                new ClassificationAclEntry(0, AccessSubjectType.User, 10, 2, ClassificationPermissionMask.Write)
            };

            var effective = AccessControlPolicyEngine.ComputeEffectiveClassificationPermissions(Tree, entries);
            Assert.Equal(ClassificationPermissionMask.None, effective[1]);
            Assert.Equal(ClassificationPermissionMask.Write, effective[2]);
            Assert.Equal(ClassificationPermissionMask.Write, effective[3]);
        }

        [Fact]
        public void Scenario04_UserDirectAndRoleCombine()
        {
            var entries = new[]
            {
                new ClassificationAclEntry(0, AccessSubjectType.User, 10, 1, ClassificationPermissionMask.Read),
                new ClassificationAclEntry(0, AccessSubjectType.Role, 7, 1, ClassificationPermissionMask.Write)
            };

            var effective = AccessControlPolicyEngine.ComputeEffectiveClassificationPermissions(Tree, entries);
            Assert.Equal(ClassificationPermissionMask.Read | ClassificationPermissionMask.Write, effective[1]);
            Assert.Equal(ClassificationPermissionMask.Read | ClassificationPermissionMask.Write, effective[2]);
        }

        [Fact]
        public void Scenario05_MissingTagViewBlocksFileVisibility()
        {
            var allowed = new[] { 1L, 2L };
            var fileTags = new[] { 1L, 3L };
            Assert.False(AccessControlPolicyEngine.HasAllTagPermissions(fileTags, allowed));
        }

        [Fact]
        public void Scenario06_TaglessFileDependsOnlyOnClassification()
        {
            var allowed = Array.Empty<long>();
            var fileTags = Array.Empty<long>();
            Assert.True(AccessControlPolicyEngine.HasAllTagPermissions(fileTags, allowed));
        }

        [Fact]
        public void Scenario07_WriteRequiresWriteMask()
        {
            var entries = new[]
            {
                new ClassificationAclEntry(0, AccessSubjectType.User, 10, 1, ClassificationPermissionMask.Read)
            };

            var effective = AccessControlPolicyEngine.ComputeEffectiveClassificationPermissions(Tree, entries);
            Assert.False((effective[1] & ClassificationPermissionMask.Write) == ClassificationPermissionMask.Write);
        }

        [Fact]
        public void Scenario08_ManageRequiresManageMask()
        {
            var entries = new[]
            {
                new ClassificationAclEntry(0, AccessSubjectType.User, 10, 1, ClassificationPermissionMask.Write)
            };

            var effective = AccessControlPolicyEngine.ComputeEffectiveClassificationPermissions(Tree, entries);
            Assert.False((effective[1] & ClassificationPermissionMask.Manage) == ClassificationPermissionMask.Manage);
        }

        [Fact]
        public void Scenario09_SearchFilteringExcludesUnauthorizedClassifications()
        {
            var entries = new[]
            {
                new ClassificationAclEntry(0, AccessSubjectType.User, 10, 1, ClassificationPermissionMask.Read)
            };

            var effective = AccessControlPolicyEngine.ComputeEffectiveClassificationPermissions(Tree, entries);
            var allowed = effective
                .Where(x => (x.Value & ClassificationPermissionMask.Read) == ClassificationPermissionMask.Read)
                .Select(x => x.Key)
                .ToHashSet();

            Assert.Contains(2, allowed);
            Assert.DoesNotContain(4, allowed);
        }

        [Fact]
        public void Scenario10_NotInTagLogicExcludesUnauthorizedFiles()
        {
            var allowedTags = new[] { 1L, 2L };
            var fileTags = new[] { 1L, 3L };
            var unauthorizedExists = fileTags.Any(tag => !allowedTags.Contains(tag));

            Assert.True(unauthorizedExists);
            Assert.False(AccessControlPolicyEngine.HasAllTagPermissions(fileTags, allowedTags));
        }

        [Fact]
        public void Scenario11_ExplicitZeroOverrideRevokesInheritedCreatorPermissions()
        {
            var entries = new[]
            {
                new ClassificationAclEntry(
                    1,
                    AccessSubjectType.User,
                    10,
                    1,
                    ClassificationPermissionMask.Read |
                    ClassificationPermissionMask.Write |
                    ClassificationPermissionMask.Move,
                    true,
                    ClassificationAclInheritMode.Override),
                new ClassificationAclEntry(
                    2,
                    AccessSubjectType.User,
                    10,
                    2,
                    ClassificationPermissionMask.None,
                    true,
                    ClassificationAclInheritMode.Override)
            };

            var effective = AccessControlPolicyEngine
                .ComputeEffectiveClassificationPermissions(Tree, entries);

            Assert.Equal(
                ClassificationPermissionMask.Read |
                ClassificationPermissionMask.Write |
                ClassificationPermissionMask.Move,
                effective[1]);
            Assert.Equal(ClassificationPermissionMask.None, effective[2]);
            Assert.Equal(ClassificationPermissionMask.None, effective[3]);
        }

        [Fact]
        public async Task Scenario12_AclChangeInvalidatesCache()
        {
            var roleRepo = new Mock<IRoleRepository>();
            var permissionRepo = new Mock<IPermissionRepository>();
            var userRoleRepo = new Mock<IUserRoleRepository>();
            var classificationAclRepo = new Mock<IClassificationAclRepository>();
            var tagAclRepo = new Mock<ITagAclRepository>();
            var accessControl = new Mock<IAccessControlService>();

            var service = new AccessAdministrationService(
                roleRepo.Object,
                permissionRepo.Object,
                userRoleRepo.Object,
                classificationAclRepo.Object,
                tagAclRepo.Object,
                accessControl.Object);

            await service.UpdateClassificationAclAsync(10, Array.Empty<ClassificationAclEntryDto>());

            accessControl.Verify(x => x.InvalidateAllCaches(), Times.Once);
        }

        [Fact]
        public async Task Scenario13_RoleChangeInvalidatesCache()
        {
            var roleRepo = new Mock<IRoleRepository>();
            var permissionRepo = new Mock<IPermissionRepository>();
            var userRoleRepo = new Mock<IUserRoleRepository>();
            var classificationAclRepo = new Mock<IClassificationAclRepository>();
            var tagAclRepo = new Mock<ITagAclRepository>();
            var accessControl = new Mock<IAccessControlService>();

            var service = new AccessAdministrationService(
                roleRepo.Object,
                permissionRepo.Object,
                userRoleRepo.Object,
                classificationAclRepo.Object,
                tagAclRepo.Object,
                accessControl.Object);

            roleRepo
                .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[]
                {
                    new Role(1, "Role 1"),
                    new Role(2, "Role 2")
                });
            userRoleRepo
                .Setup(x => x.SetUserRolesAsync(77, It.Is<IReadOnlyCollection<long>>(ids => ids.SequenceEqual(new[] { 1L, 2L })), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            await service.SetUserRolesAsync(77, new[] { 1L, 2L });

            accessControl.Verify(x => x.InvalidateUserCache(77), Times.Once);
        }

        [Fact]
        public async Task Scenario14_CascadeOverridesOnlyTheSelectedSubjectOnDescendants()
        {
            var roleRepo = new Mock<IRoleRepository>();
            var permissionRepo = new Mock<IPermissionRepository>();
            var userRoleRepo = new Mock<IUserRoleRepository>();
            var classificationAclRepo = new Mock<IClassificationAclRepository>();
            var tagAclRepo = new Mock<ITagAclRepository>();
            var accessControl = new Mock<IAccessControlService>();
            var service = new AccessAdministrationService(
                roleRepo.Object,
                permissionRepo.Object,
                userRoleRepo.Object,
                classificationAclRepo.Object,
                tagAclRepo.Object,
                accessControl.Object);
            var entries = new[]
            {
                new ClassificationAclEntryDto(
                    0,
                    AccessSubjectType.User,
                    10,
                    1,
                    ClassificationPermissionMask.Read | ClassificationPermissionMask.Write,
                    true,
                    ClassificationAclInheritMode.Override,
                    true),
                new ClassificationAclEntryDto(
                    0,
                    AccessSubjectType.Role,
                    20,
                    1,
                    ClassificationPermissionMask.Read,
                    true,
                    ClassificationAclInheritMode.Override,
                    false)
            };

            await service.UpdateClassificationAclAsync(1, entries);

            classificationAclRepo.Verify(repository => repository.ReplaceForClassificationAsync(
                1,
                It.Is<IReadOnlyCollection<ClassificationAclEntry>>(items => items.Count == 2),
                It.IsAny<CancellationToken>()), Times.Once);
            classificationAclRepo.Verify(repository => repository.OverrideForDescendantsAsync(
                1,
                It.Is<IReadOnlyCollection<ClassificationAclEntry>>(items =>
                    items.Count == 1 &&
                    items.Single().SubjectType == AccessSubjectType.User &&
                    items.Single().SubjectId == 10 &&
                    items.Single().PermissionMask ==
                    (ClassificationPermissionMask.Read | ClassificationPermissionMask.Write)),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
