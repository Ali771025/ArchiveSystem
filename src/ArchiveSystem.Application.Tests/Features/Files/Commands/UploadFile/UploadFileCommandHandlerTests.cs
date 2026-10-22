// src/ArchiveSystem.Application.Tests/Features/Files/Commands/UploadFile/UploadFileCommandHandlerTests.cs
using Moq;
using Xunit;
using ArchiveSystem.Application.Interfaces;
using Microsoft.AspNetCore.Http;

using Microsoft.Extensions.Logging.Abstractions;
using ArchiveSystem.Application.Features.Files.Commands.UploadFile;
using ArchiveSystem.Domain.Repositories;
using ArchiveSystem.Domain.Services;
using ArchiveSystem.Domain.Events;

namespace ArchiveSystem.Application.Tests.Features.Files.Commands.UploadFile
{
    public class UploadFileCommandHandlerTests
    {
        private readonly Mock<IFileRepository> _mockFileRepository;
        private readonly Mock<IMessageBus> _mockMessageBus;
        private readonly Mock<IAuditService> _mockAuditService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UploadFileCommandHandler _handler;

        public UploadFileCommandHandlerTests()
        {
            _mockFileRepository = new Mock<IFileRepository>();
            _mockMessageBus = new Mock<IMessageBus>();
            _mockAuditService = new Mock<IAuditService>();
            _httpContextAccessor = new HttpContextAccessor();

            _mockAuditService
                .Setup(a => a.LogOperationAsync(
                    It.IsAny<long>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _handler = new UploadFileCommandHandler(
                _mockFileRepository.Object,
                _mockMessageBus.Object,
                _mockAuditService.Object,
                _httpContextAccessor,
                NullLogger<UploadFileCommandHandler>.Instance);
        }

        [Fact]
        public async Task Handle_ValidCommand_CreatesFileAndPublishesEvent()
        {
            // Arrange
            var command = new UploadFileCommand
            {
                FileName = "test.pdf",
                Size = 1024,
                ContentType = "application/pdf",
                TusId = "test-tus-id",
                OriginalFileName = "original_test.pdf"
            };

            _mockFileRepository.Setup(r => r.AddAsync(It.IsAny<Domain.Entities.ArchiveFile>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockFileRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.FileId > 0);
            Assert.Equal("File upload initiated successfully. Processing in background.", result.Message);

            _mockFileRepository.Verify(r => r.AddAsync(It.IsAny<Domain.Entities.ArchiveFile>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockFileRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockMessageBus.Verify(mb => mb.PublishAsync(It.IsAny<FileUploadedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ValidCommand_CreatesFileWithCorrectProperties()
        {
            // Arrange
            var command = new UploadFileCommand
            {
                FileName = "test_document.pdf",
                Size = 2048,
                ContentType = "application/pdf",
                TusId = "test-tus-id",
                OriginalFileName = "original_document.pdf"
            };

            Domain.Entities.ArchiveFile capturedFile = null!;
            _mockFileRepository.Setup(r => r.AddAsync(It.IsAny<Domain.Entities.ArchiveFile>(), It.IsAny<CancellationToken>()))
                .Callback<Domain.Entities.ArchiveFile, CancellationToken>((f, _) => capturedFile = f)
                .Returns(Task.CompletedTask);
            _mockFileRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.NotNull(capturedFile);
            Assert.Equal("test_document.pdf", capturedFile.Name);
            Assert.Equal(2048, capturedFile.Size.Bytes);
            Assert.Equal(".pdf", capturedFile.Extension.Value);
            Assert.Equal("application/pdf", capturedFile.ContentType);
            Assert.Equal(Domain.Enums.FileStatus.Pending, capturedFile.Status);
            Assert.Contains("uploads/", capturedFile.StoragePath);
            Assert.Contains("test_document", capturedFile.StoragePath);
        }

        [Fact]
        public async Task Handle_CommandWithCaseId_AssignsToCase()
        {
            // Arrange
            var command = new UploadFileCommand
            {
                FileName = "test.pdf",
                Size = 1024,
                ContentType = "application/pdf",
                TusId = "test-tus-id",
                CaseId = 123,
                OriginalFileName = "original_test.pdf"
            };

            Domain.Entities.ArchiveFile capturedFile = null!;
            _mockFileRepository.Setup(r => r.AddAsync(It.IsAny<Domain.Entities.ArchiveFile>(), It.IsAny<CancellationToken>()))
                .Callback<Domain.Entities.ArchiveFile, CancellationToken>((f, _) => capturedFile = f)
                .Returns(Task.CompletedTask);
            _mockFileRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.NotNull(capturedFile);
            Assert.Equal(123L, capturedFile.CaseId);
        }

        [Fact]
        public async Task Handle_RepositorySaveThrows_ExceptionIsPropagated()
        {
            // Arrange
            var command = new UploadFileCommand
            {
                FileName = "test.pdf",
                Size = 1024,
                ContentType = "application/pdf",
                TusId = "test-tus-id",
                OriginalFileName = "original_test.pdf"
            };

            _mockFileRepository.Setup(r => r.AddAsync(It.IsAny<Domain.Entities.ArchiveFile>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockFileRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, CancellationToken.None));

            _mockFileRepository.Verify(r => r.AddAsync(It.IsAny<Domain.Entities.ArchiveFile>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockFileRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
