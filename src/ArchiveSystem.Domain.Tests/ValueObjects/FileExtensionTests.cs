// src/ArchiveSystem.Domain.Tests/ValueObjects/FileExtensionTests.cs
using Xunit;
using ArchiveSystem.Domain.ValueObjects;

namespace ArchiveSystem.Domain.Tests.ValueObjects
{
    public class FileExtensionTests
    {
        [Fact]
        public void Create_ValidExtension_ReturnsCorrectValue()
        {
            // Act
            var extension = FileExtension.Create(".pdf");

            // Assert
            Assert.Equal(".pdf", extension.Value);
        }

        [Fact]
        public void Create_ExtensionWithoutDot_AddsDot()
        {
            // Act
            var extension = FileExtension.Create("pdf");

            // Assert
            Assert.Equal(".pdf", extension.Value);
        }

        [Fact]
        public void Create_NullExtension_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => FileExtension.Create(null!));
        }

        [Fact]
        public void Create_EmptyExtension_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => FileExtension.Create(""));
        }

        [Fact]
        public void Create_InvalidExtension_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => FileExtension.Create(".invalid_extension_with_too_many_chars"));
        }

        [Fact]
        public void Equality_Comparison_WorksCorrectly()
        {
            // Arrange
            var ext1 = FileExtension.Create(".pdf");
            var ext2 = FileExtension.Create(".PDF"); // Different case
            var ext3 = FileExtension.Create(".txt");

            // Assert
            Assert.Equal(ext1, ext2); // Should be equal despite case
            Assert.NotEqual(ext1, ext3);
            Assert.True(ext1 == ext2);
            Assert.True(ext1 != ext3);
        }
    }
}
