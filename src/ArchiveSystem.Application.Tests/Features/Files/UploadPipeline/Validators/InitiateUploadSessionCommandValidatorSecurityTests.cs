using ArchiveSystem.Application.Features.Files.UploadPipeline.Commands;
using ArchiveSystem.Application.Options;
using ArchiveSystem.Domain.Repositories;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace ArchiveSystem.Application.Tests.Features.Files.UploadPipeline.Validators
{
    public class InitiateUploadSessionCommandValidatorSecurityTests
    {
        private static readonly string ValidSha256 = new('a', 64);

        [Theory]
        [InlineData("malware.exe")]
        [InlineData("script.bat")]
        [InlineData("shell.php")]
        [InlineData("test.pdf.exe")]
        [InlineData("invoice.PDF.EXE")]
        public async Task Validate_BlockedExecutableOrScriptExtensions_ReturnsValidationError(string fileName)
        {
            var validator = CreateValidator();

            var result = await validator.ValidateAsync(CreateCommand(fileName));

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(InitiateUploadSessionCommand.OriginalFileName) &&
                error.ErrorMessage == "File extension is not allowed.");
        }

        [Theory]
        [InlineData("../secret.pdf")]
        [InlineData("folder/secret.pdf")]
        [InlineData("folder\\secret.pdf")]
        public async Task Validate_PathLikeFileNames_ReturnsValidationError(string fileName)
        {
            var validator = CreateValidator();

            var result = await validator.ValidateAsync(CreateCommand(fileName));

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(InitiateUploadSessionCommand.OriginalFileName) &&
                error.ErrorMessage == "File name is not valid.");
        }

        [Fact]
        public async Task Validate_FileLargerThanConfiguredLimit_ReturnsValidationError()
        {
            const long oneHundredMegabytes = 100L * 1024 * 1024;
            var validator = CreateValidator(maxFileSizeBytes: oneHundredMegabytes);

            var result = await validator.ValidateAsync(CreateCommand("document.pdf", expectedSizeBytes: oneHundredMegabytes + 1));

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(InitiateUploadSessionCommand.ExpectedSizeBytes));
        }

        [Fact]
        public async Task Validate_AllowedDocumentExtension_ReturnsSuccess()
        {
            var validator = CreateValidator();

            var result = await validator.ValidateAsync(CreateCommand("document.pdf"));

            Assert.True(result.IsValid);
        }

        private static InitiateUploadSessionCommandValidator CreateValidator(long maxFileSizeBytes = 10 * 1024 * 1024)
        {
            var options = Microsoft.Extensions.Options.Options.Create(new FileUploadOptions
            {
                MaxFileSizeBytes = maxFileSizeBytes
            });

            var classificationRepository = new Mock<IClassificationRepository>();
            classificationRepository
                .Setup(repository => repository.HasChildrenAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            return new InitiateUploadSessionCommandValidator(options, classificationRepository.Object);
        }

        private static InitiateUploadSessionCommand CreateCommand(string fileName, long expectedSizeBytes = 1024)
        {
            return new InitiateUploadSessionCommand
            {
                OriginalFileName = fileName,
                ContentType = "application/pdf",
                ExpectedSizeBytes = expectedSizeBytes,
                ExpectedSha256 = ValidSha256,
                ClassificationId = 10
            };
        }
    }
}
