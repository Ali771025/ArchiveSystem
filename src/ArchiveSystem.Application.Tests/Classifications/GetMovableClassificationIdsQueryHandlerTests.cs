using ArchiveSystem.Application.Features.Classifications.Queries.GetMovableClassificationIds;
using ArchiveSystem.Application.Features.Classifications.Security;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Domain.Enums;
using ArchiveSystem.Domain.Repositories;
using Moq;
using Xunit;

namespace ArchiveSystem.Application.Tests.Classifications;

public sealed class GetMovableClassificationIdsQueryHandlerTests
{
    [Fact]
    public async Task Handle_UsesMovePermissionAndReturnsOnlyMovableLeaves()
    {
        var classifications = new[]
        {
            new Classification(1, "ریشه"),
            new Classification(2, "مقصد مجاز", 1),
            new Classification(3, "مقصد بدون مجوز انتقال", 1)
        };
        var repository = new Mock<IClassificationRepository>();
        repository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(classifications);
        var authorizer = new Mock<IClassificationAccessAuthorizer>();
        authorizer
            .Setup(x => x.GetAccessibleSetAsync(
                classifications,
                ClassificationPermissionMask.Move,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<long> { 1, 2 });
        var handler = new GetMovableClassificationIdsQueryHandler(
            repository.Object,
            authorizer.Object);

        var result = await handler.Handle(
            new GetMovableClassificationIdsQuery(),
            CancellationToken.None);

        Assert.Equal(new[] { 2L }, result);
        authorizer.Verify(x => x.GetAccessibleSetAsync(
            classifications,
            ClassificationPermissionMask.Move,
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
