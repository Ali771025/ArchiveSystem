using ArchiveSystem.Application.Common.Interfaces.Security;
using ArchiveSystem.Application.Features.Classifications.Security;
using Moq;
using Xunit;

namespace ArchiveSystem.Application.Tests.Classifications;

public sealed class ClassificationAccessAuthorizerTests
{
    [Fact]
    public async Task EnsureCanManageAsync_WithClassificationWriteAcl_AllowsOperation()
    {
        const long userId = 41;
        const long classificationId = 65;
        var currentUser = new Mock<ICurrentUserContext>();
        var accessControl = new Mock<IAccessControlService>();
        var identity = new Mock<IIdentityManagementService>();

        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns(userId);
        currentUser.Setup(x => x.IsInRole("Admin")).Returns(false);
        accessControl
            .Setup(x => x.HasGlobalPermissionAsync(
                userId,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        accessControl
            .Setup(x => x.CanManageClassificationAsync(
                userId,
                classificationId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        accessControl
            .Setup(x => x.CanWriteClassificationAsync(
                userId,
                classificationId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var authorizer = new ClassificationAccessAuthorizer(
            currentUser.Object,
            accessControl.Object,
            identity.Object);

        await authorizer.EnsureCanManageAsync(classificationId, CancellationToken.None);

        accessControl.Verify(
            x => x.CanWriteClassificationAsync(
                userId,
                classificationId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
