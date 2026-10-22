using ArchiveSystem.Application.AccessControl;
using ArchiveSystem.Application.Common.Exceptions;
using ArchiveSystem.Application.Features.Files.Security;
using ArchiveSystem.Application.Features.Files.UploadPipeline.Queries;
using ArchiveSystem.Application.Features.Reports.Interfaces;
using ArchiveSystem.Application.Features.Reports.Models;
using ArchiveSystem.Application.Interfaces.Persistence;
using ArchiveSystem.Application.Interfaces.Storage;
using ArchiveSystem.Application.Options;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Domain.ValueObjects;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace ArchiveSystem.Infrastructure.Tests.AccessControl
{
    public class FileDownloadAuthorizationTests
    {
        [Fact]
        public async Task Handle_UserWithoutFileAccess_ThrowsForbiddenBeforeReadingObjectStorage()
        {
            var file = new ArchiveFile(
                id: 200,
                name: "secret.pdf",
                size: FileSize.Create(1024),
                extension: FileExtension.Create(".pdf"),
                contentType: "application/pdf",
                storagePath: "bucket/object",
                caseId: 100);

            var fileRepository = new Mock<IFileRepository>();
            fileRepository
                .Setup(repository => repository.GetByIdAsync(file.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(file);

            var versionRepository = new Mock<IFileVersionRepository>();
            var objectStorage = new Mock<IObjectStorage>();
            var authorizer = new Mock<IFileAccessAuthorizer>();
            authorizer
                .Setup(service => service.EnsureCanViewFileAsync(file.Id, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ForbiddenAccessException("User is not allowed to view this file."));

            var handler = new GetDownloadQueryHandler(
                fileRepository.Object,
                versionRepository.Object,
                objectStorage.Object,
                Options.Create(new ObjectStorageOptions()),
                authorizer.Object,
                Mock.Of<IAccessControlLookupRepository>(),
                Mock.Of<IAuditWriter>(),
                new HttpContextAccessor());

            await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
                handler.Handle(new GetDownloadQuery { FileId = file.Id }, CancellationToken.None));

            objectStorage.Verify(
                storage => storage.GetObjectStreamAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_AuthorizedDownload_WritesMovementLogWithCaseId()
        {
            var file = new ArchiveFile(
                id: 200,
                name: "document.pdf",
                size: FileSize.Create(1024),
                extension: FileExtension.Create(".pdf"),
                contentType: "application/pdf",
                storagePath: "files/document.pdf",
                caseId: 100);
            var version = new FileVersion(
                id: 300,
                fileId: file.Id,
                versionNumber: 1,
                storagePath: "files/document-v1.pdf",
                fileName: "document.pdf",
                contentType: "application/pdf",
                size: 1024,
                sha256: new string('a', 64));

            var fileRepository = new Mock<IFileRepository>();
            fileRepository
                .Setup(repository => repository.GetByIdAsync(file.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(file);

            var versionRepository = new Mock<IFileVersionRepository>();
            versionRepository
                .Setup(repository => repository.GetLatestVersionAsync(file.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(version);

            var objectStorage = new Mock<IObjectStorage>();
            objectStorage
                .Setup(storage => storage.GetObjectStreamAsync(
                    It.IsAny<string>(),
                    version.StoragePath,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MemoryStream(new byte[] { 1, 2, 3 }));

            var authorizer = new Mock<IFileAccessAuthorizer>();
            authorizer.SetupGet(service => service.CurrentUserId).Returns(500);
            authorizer
                .Setup(service => service.EnsureCanViewFileAsync(file.Id, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var lookupRepository = new Mock<IAccessControlLookupRepository>();
            lookupRepository
                .Setup(repository => repository.GetFileAccessDescriptorAsync(file.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FileAccessDescriptor(file.Id, 57, Array.Empty<long>()));

            MovementLogWriteModel? writtenLog = null;
            var auditWriter = new Mock<IAuditWriter>();
            auditWriter
                .Setup(writer => writer.WriteAsync(It.IsAny<MovementLogWriteModel>(), It.IsAny<CancellationToken>()))
                .Callback<MovementLogWriteModel, CancellationToken>((model, _) => writtenLog = model)
                .Returns(Task.CompletedTask);

            var handler = new GetDownloadQueryHandler(
                fileRepository.Object,
                versionRepository.Object,
                objectStorage.Object,
                Options.Create(new ObjectStorageOptions()),
                authorizer.Object,
                lookupRepository.Object,
                auditWriter.Object,
                new HttpContextAccessor { HttpContext = new DefaultHttpContext() });

            var result = await handler.Handle(
                new GetDownloadQuery { FileId = file.Id },
                CancellationToken.None);

            Assert.NotNull(result);
            Assert.NotNull(writtenLog);
            Assert.Equal("FILE_DOWNLOADED", writtenLog.ActionType);
            Assert.Equal(100, writtenLog.CaseId);
            Assert.Equal(file.Id, writtenLog.FileId);
            Assert.Equal(version.Id, writtenLog.FileVersionId);
            Assert.Equal(57, writtenLog.ClassificationId);
        }
    }
}
