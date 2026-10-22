using ArchiveSystem.Application.Common.Exceptions;
using ArchiveSystem.Application.Features.Files.Commands.RetryFileOcr;
using ArchiveSystem.Application.Features.Files.Security;
using ArchiveSystem.Application.Interfaces.Persistence;
using ArchiveSystem.Domain.Entities;
using Moq;
using Xunit;

namespace ArchiveSystem.Application.Tests.Features.Files.Commands.RetryFileOcr
{
    public sealed class RetryFileOcrCommandHandlerTests
    {
        [Fact]
        public async Task Handle_FailedVersion_QueuesRetryAfterWriteAuthorization()
        {
            var version = CreateVersion();
            version.FailOcr("temporary failure");
            var repository = new Mock<IFileVersionRepository>();
            var authorizer = new Mock<IFileAccessAuthorizer>();
            repository.Setup(x => x.GetByIdAsync(22, It.IsAny<CancellationToken>()))
                .ReturnsAsync(version);
            repository.Setup(x => x.TryQueueOcrRetryAsync(22, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            var handler = new RetryFileOcrCommandHandler(repository.Object, authorizer.Object);

            await handler.Handle(new RetryFileOcrCommand(11, 22), CancellationToken.None);

            authorizer.Verify(x => x.EnsureCanWriteFileAsync(11, It.IsAny<CancellationToken>()), Times.Once);
            repository.Verify(x => x.TryQueueOcrRetryAsync(22, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_PendingVersion_RejectsDuplicateRetry()
        {
            var repository = new Mock<IFileVersionRepository>();
            var authorizer = new Mock<IFileAccessAuthorizer>();
            repository.Setup(x => x.GetByIdAsync(22, It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateVersion());
            var handler = new RetryFileOcrCommandHandler(repository.Object, authorizer.Object);

            await Assert.ThrowsAsync<ConflictException>(() =>
                handler.Handle(new RetryFileOcrCommand(11, 22), CancellationToken.None));

            repository.Verify(
                x => x.TryQueueOcrRetryAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private static FileVersion CreateVersion() => new(
            id: 22,
            fileId: 11,
            versionNumber: 1,
            storagePath: "cases/11/test.pdf",
            fileName: "test.pdf",
            contentType: "application/pdf",
            size: 100,
            sha256: new string('a', 64));
    }
}
