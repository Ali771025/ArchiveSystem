// src/ArchiveSystem.Domain.Tests/Entities/FileTests.cs
using Xunit;
using ArchiveSystem.Domain.Enums;
using ArchiveSystem.Domain.ValueObjects;
using FileEntity = ArchiveSystem.Domain.Entities.ArchiveFile;

namespace ArchiveSystem.Domain.Tests.Entities
{
    public class FileTests
    {
        [Fact]
        public void Constructor_ValidParameters_SetsPropertiesCorrectly()
        {
            // Arrange
            var id = 1L;
            var name = "test.pdf";
            var size = FileSize.Create(1024);
            var extension = FileExtension.Create(".pdf");
            var contentType = "application/pdf";
            var storagePath = "/files/test.pdf";

            // Act
            var file = new FileEntity(id, name, size, extension, contentType, storagePath);

            // Assert
            Assert.Equal(id, file.Id);
            Assert.Equal(name, file.Name);
            Assert.Equal(size, file.Size);
            Assert.Equal(extension, file.Extension);
            Assert.Equal(contentType, file.ContentType);
            Assert.Equal(storagePath, file.StoragePath);
            Assert.Equal(FileStatus.Pending, file.Status);
            Assert.NotNull(file.CreatedAt);
        }

        [Fact]
        public void Constructor_NullSize_ThrowsArgumentNullException()
        {
            // Arrange
            var name = "test.pdf";
            FileSize? size = null;
            var extension = FileExtension.Create(".pdf");
            var contentType = "application/pdf";
            var storagePath = "/files/test.pdf";

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new FileEntity(1L, name, size!, extension, contentType, storagePath));
        }

        [Fact]
        public void Constructor_NullExtension_ThrowsArgumentNullException()
        {
            // Arrange
            var name = "test.pdf";
            var size = FileSize.Create(1024);
            FileExtension? extension = null;
            var contentType = "application/pdf";
            var storagePath = "/files/test.pdf";

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new FileEntity(1L, name, size, extension!, contentType, storagePath));
        }

        [Fact]
        public void UpdateStatus_ChangesStatusAndModifiesTimestamp()
        {
            // Arrange
            var file = CreateValidFile();
            var originalModifiedAt = file.ModifiedAt;

            // Act
            file.UpdateStatus(FileStatus.Ready);

            // Assert
            Assert.Equal(FileStatus.Ready, file.Status);
            Assert.NotNull(file.ModifiedAt);
            Assert.NotEqual(originalModifiedAt, file.ModifiedAt);
        }

        [Fact]
        public void AssignToCase_SetsCaseIdAndModifiesTimestamp()
        {
            // Arrange
            var file = CreateValidFile();
            var caseId = 123L;
            var originalModifiedAt = file.ModifiedAt;

            // Act
            file.AssignToCase(caseId);

            // Assert
            Assert.Equal(caseId, file.CaseId);
            Assert.NotNull(file.ModifiedAt);
            Assert.NotEqual(originalModifiedAt, file.ModifiedAt);
        }

        [Fact]
        public void SoftDelete_SetsStatusToDeletedAndSetsDeletedAt()
        {
            // Arrange
            var file = CreateValidFile();

            // Act
            file.SoftDelete();

            // Assert
            Assert.Equal(FileStatus.Deleted, file.Status);
            Assert.NotNull(file.DeletedAt);
        }

        [Fact]
        public void Restore_FromDeletedStatus_ChangesStatusToReady()
        {
            // Arrange
            var file = CreateValidFile();
            file.SoftDelete(); // Set to deleted first

            // Act
            file.Restore();

            // Assert
            Assert.Equal(FileStatus.Ready, file.Status);
            Assert.Null(file.DeletedAt);
        }

        [Fact]
        public void IsValidExtension_ValidExtension_ReturnsTrue()
        {
            // Arrange
            var file = CreateValidFile();
            var allowedExtensions = new[] { ".pdf", ".doc", ".txt" };

            // Act
            var result = file.IsValidExtension(allowedExtensions);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValidExtension_InvalidExtension_ReturnsFalse()
        {
            // Arrange
            var file = CreateValidFile();
            var allowedExtensions = new[] { ".jpg", ".png", ".gif" };

            // Act
            var result = file.IsValidExtension(allowedExtensions);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void SetHash_ValidHash_SetsHashCorrectly()
        {
            // Arrange
            var file = CreateValidFile();
            var hash = "abc123def456";

            // Act
            file.SetHash(hash);

            // Assert
            Assert.Equal(hash, file.Hash);
        }

        [Fact]
        public void SetHash_NullOrEmptyHash_ThrowsArgumentException()
        {
            // Arrange
            var file = CreateValidFile();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => file.SetHash(null!));
            Assert.Throws<ArgumentException>(() => file.SetHash(""));
            Assert.Throws<ArgumentException>(() => file.SetHash("   "));
        }

        private FileEntity CreateValidFile()
        {
            return new FileEntity(
                1L,
                "test.pdf",
                FileSize.Create(1024),
                FileExtension.Create(".pdf"),
                "application/pdf",
                "/files/test.pdf"
            );
        }
    }
}
