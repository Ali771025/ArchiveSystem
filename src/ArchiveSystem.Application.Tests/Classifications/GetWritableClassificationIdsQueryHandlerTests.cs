using ArchiveSystem.Application.Features.Classifications.Queries.GetWritableClassificationIds;
using ArchiveSystem.Application.Features.Classifications.Security;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Domain.Enums;
using ArchiveSystem.Domain.Repositories;
using Moq;
using Xunit;

namespace ArchiveSystem.Application.Tests.Classifications;

public sealed class GetWritableClassificationIdsQueryHandlerTests
{
    [Fact]
    public async Task Handle_UsesWritePermissionAndReturnsOnlyWritableLeaves()
    {
        var classifications = new[]
        {
            new Classification(1, "ریشه"),
            new Classification(2, "شاخه قابل نوشتن", 1),
            new Classification(3, "شاخه فقط خواندنی", 1)
        };
        var repository = new Mock<IClassificationRepository>();
        repository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(classifications);

        var authorizer = new Mock<IClassificationAccessAuthorizer>();
        authorizer
            .Setup(x => x.GetAccessibleSetAsync(
                classifications,
                ClassificationPermissionMask.Write,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<long> { 1, 2 });

        var handler = new GetWritableClassificationIdsQueryHandler(
            repository.Object,
            authorizer.Object);

        var result = await handler.Handle(
            new GetWritableClassificationIdsQuery(),
            CancellationToken.None);

        Assert.Equal(new[] { 2L }, result);
        authorizer.Verify(x => x.GetAccessibleSetAsync(
            classifications,
            ClassificationPermissionMask.Write,
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
