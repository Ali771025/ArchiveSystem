using ArchiveSystem.Application.Authorization;
using ArchiveSystem.Application.Common.Interfaces.Repositories;
using ArchiveSystem.Application.Features.Classifications.Commands.CreateClassification;
using ArchiveSystem.Application.Features.Classifications.Security;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Domain.Enums;
using ArchiveSystem.Domain.Repositories;
using Moq;
using Xunit;

namespace ArchiveSystem.Application.Tests.Classifications;

public sealed class CreateClassificationCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithGlobalCreateAndParentWriteAccess_CreatesChildClassification()
    {
        const long parentId = 57;
        var parent = new Classification(parentId, "Parent");
        var repository = new Mock<IClassificationRepository>(MockBehavior.Strict);
        var aclRepository = new Mock<IClassificationAclRepository>(MockBehavior.Strict);
        var authorizer = new Mock<IClassificationAccessAuthorizer>(MockBehavior.Strict);

        authorizer
            .Setup(x => x.EnsureHasPermissionAsync(Permissions.Categories.Create, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        authorizer
            .Setup(x => x.EnsureCanWriteAsync(parentId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        authorizer.SetupGet(x => x.CurrentUserId).Returns(91);
        authorizer.Setup(x => x.InvalidateAllCaches());

        repository
            .Setup(x => x.ExistsActiveSiblingWithTitleAsync(parentId, "Child", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repository
            .Setup(x => x.AddAsync(It.IsAny<Classification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository
            .Setup(x => x.GetByIdAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parent);
        repository
            .Setup(x => x.UpdateAsync(It.IsAny<Classification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        aclRepository
            .Setup(x => x.GrantUserAccessAsync(
                91,
                It.IsAny<long>(),
                ClassificationPermissionMask.Read |
                ClassificationPermissionMask.Write |
                ClassificationPermissionMask.Move,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new CreateClassificationCommandHandler(
            repository.Object,
            aclRepository.Object,
            authorizer.Object);

        var result = await handler.Handle(
            new CreateClassificationCommand
            {
                ParentId = parentId,
                Title = " Child "
            },
            CancellationToken.None);

        Assert.Equal("Child", result.Title);
        Assert.Equal(parentId, result.ParentId);
        authorizer.Verify(x => x.EnsureCanWriteAsync(parentId, It.IsAny<CancellationToken>()), Times.Once);
        authorizer.Verify(x => x.EnsureCanManageAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
        aclRepository.VerifyAll();
    }

    [Fact]
    public async Task Handle_WithGlobalCreatePermission_CreatesRootAndGrantsCreatorEditableDefaultAccess()
    {
        const long userId = 73;
        var repository = new Mock<IClassificationRepository>(MockBehavior.Strict);
        var aclRepository = new Mock<IClassificationAclRepository>(MockBehavior.Strict);
        var authorizer = new Mock<IClassificationAccessAuthorizer>(MockBehavior.Strict);

        authorizer
            .Setup(x => x.EnsureHasPermissionAsync(
                Permissions.Categories.Create,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        authorizer.SetupGet(x => x.CurrentUserId).Returns(userId);
        authorizer.Setup(x => x.InvalidateAllCaches());

        repository
            .Setup(x => x.ExistsActiveSiblingWithTitleAsync(
                null,
                "Root",
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repository
            .Setup(x => x.AddAsync(It.IsAny<Classification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository
            .Setup(x => x.UpdateAsync(It.IsAny<Classification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        aclRepository
            .Setup(x => x.GrantUserAccessAsync(
                userId,
                It.IsAny<long>(),
                ClassificationPermissionMask.Read |
                ClassificationPermissionMask.Write |
                ClassificationPermissionMask.Move,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new CreateClassificationCommandHandler(
            repository.Object,
            aclRepository.Object,
            authorizer.Object);

        var result = await handler.Handle(
            new CreateClassificationCommand { Title = " Root " },
            CancellationToken.None);

        Assert.Equal("Root", result.Title);
        Assert.Null(result.ParentId);
        authorizer.Verify(
            x => x.HasGlobalManageAsync(It.IsAny<CancellationToken>()),
            Times.Never);
        authorizer.Verify(
            x => x.EnsureCanWriteAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
        aclRepository.VerifyAll();
    }
}
