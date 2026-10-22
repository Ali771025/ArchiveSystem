using ArchiveSystem.Application.Common.Exceptions;
using ArchiveSystem.Application.Features.Files.Commands.DeleteFile;
using ArchiveSystem.Application.Features.Files.Security;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Domain.Enums;
using ArchiveSystem.Domain.Repositories;
using ArchiveSystem.Domain.ValueObjects;
using Moq;

namespace ArchiveSystem.Infrastructure.Tests.AccessControl;

public sealed class DeleteFileAuthorizationTests
{
    [Theory]
    [InlineData(FileStatus.Pending)]
    [InlineData(FileStatus.Ready)]
    [InlineData(FileStatus.Archived)]
    public async Task Handle_SoftDeletesActiveDocument(FileStatus status)
    {
        var file = CreateFile(status);
        var repository = new Mock<IFileRepository>();
        repository
            .Setup(instance => instance.GetByIdAsync(file.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(file);
        repository
            .Setup(instance => instance.DeleteAsync(file, It.IsAny<CancellationToken>()))
            .Callback<ArchiveFile, CancellationToken>((document, _) => document.SoftDelete())
            .Returns(Task.CompletedTask);
        var authorizer = new Mock<IFileAccessAuthorizer>();
        var handler = new DeleteFileCommandHandler(repository.Object, authorizer.Object);

        await handler.Handle(new DeleteFileCommand(file.Id), CancellationToken.None);

        authorizer.Verify(
            instance => instance.EnsureCanWriteFileAsync(file.Id, It.IsAny<CancellationToken>()),
            Times.Once);
        repository.Verify(
            instance => instance.DeleteAsync(file, It.IsAny<CancellationToken>()),
            Times.Once);
        repository.Verify(
            instance => instance.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.True(file.IsDeleted);
        Assert.Equal(FileStatus.Deleted, file.Status);
    }

    [Fact]
    public async Task Handle_RejectsAlreadyDeletedDocument()
    {
        var file = CreateFile(FileStatus.Deleted);
        var repository = new Mock<IFileRepository>();
        repository
            .Setup(instance => instance.GetByIdAsync(file.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(file);
        var authorizer = new Mock<IFileAccessAuthorizer>();
        var handler = new DeleteFileCommandHandler(repository.Object, authorizer.Object);

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new DeleteFileCommand(file.Id), CancellationToken.None));

        Assert.Equal("این سند قبلاً حذف شده است.", exception.Message);
        repository.Verify(
            instance => instance.DeleteAsync(It.IsAny<ArchiveFile>(), It.IsAny<CancellationToken>()),
            Times.Never);
        repository.Verify(
            instance => instance.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static ArchiveFile CreateFile(FileStatus status)
    {
        var file = new ArchiveFile(
            42,
            "document.pdf",
            FileSize.Create(100),
            FileExtension.Create("pdf"),
            "application/pdf",
            "cases/1/document.pdf",
            1);
        file.UpdateStatus(status);
        return file;
    }
}
