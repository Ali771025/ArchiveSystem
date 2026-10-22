using ArchiveSystem.Application.Authorization;
using ArchiveSystem.Application.Common.Exceptions;
using ArchiveSystem.Application.Common.Models.DynamicFields;
using ArchiveSystem.Application.Features.Classifications.Security;
using ArchiveSystem.Application.Features.DynamicFields.Commands.UpsertTemplate;
using ArchiveSystem.Application.Interfaces.Persistence;
using ArchiveSystem.Domain.Repositories;
using Moq;
using Xunit;

namespace ArchiveSystem.Application.Tests.DynamicFields;

public sealed class UpsertTemplateCommandHandlerAuthorizationTests
{
    [Fact]
    public async Task Handle_RequiresDynamicFieldsPermissionAndClassificationWriteAccess()
    {
        const long classificationId = 57;
        var classifications = new Mock<IClassificationRepository>(MockBehavior.Strict);
        var templates = new Mock<IDynamicTemplateRepository>(MockBehavior.Strict);
        var authorizer = new Mock<IClassificationAccessAuthorizer>(MockBehavior.Strict);

        authorizer
            .Setup(x => x.EnsureHasPermissionAsync(Permissions.DynamicFields.Manage, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        authorizer
            .Setup(x => x.EnsureCanWriteAsync(classificationId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        classifications
            .Setup(x => x.GetByIdAsync(classificationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ArchiveSystem.Domain.Entities.Classification?)null);

        var handler = new UpsertTemplateCommandHandler(
            classifications.Object,
            templates.Object,
            authorizer.Object);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(
            new UpsertTemplateCommand
            {
                ClassificationId = classificationId,
                Template = new DynamicTemplateDto()
            },
            CancellationToken.None));

        authorizer.Verify(
            x => x.EnsureHasPermissionAsync(Permissions.DynamicFields.Manage, It.IsAny<CancellationToken>()),
            Times.Once);
        authorizer.Verify(
            x => x.EnsureCanWriteAsync(classificationId, It.IsAny<CancellationToken>()),
            Times.Once);
        authorizer.Verify(
            x => x.EnsureCanManageAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
