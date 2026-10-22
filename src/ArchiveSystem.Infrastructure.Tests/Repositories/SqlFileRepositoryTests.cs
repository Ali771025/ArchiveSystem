// src/ArchiveSystem.Infrastructure.Tests/Repositories/SqlFileRepositoryTests.cs
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using ArchiveSystem.Application.Interfaces;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Domain.Enums;
using ArchiveSystem.Domain.ValueObjects;
using FileEntity = ArchiveSystem.Domain.Entities.ArchiveFile;
using DomainFile = ArchiveSystem.Domain.Entities.ArchiveFile;
using ArchiveSystem.Infrastructure.Persistence;
using ArchiveSystem.Infrastructure.Repositories;

namespace ArchiveSystem.Infrastructure.Tests.Repositories
{
    public class SqlFileRepositoryTests : IDisposable
    {
        private readonly ArchiveDbContext _context;
        private readonly SqlFileRepository _repository;
        private readonly IConfiguration _configuration;
        private readonly IAuditService _auditService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SqlFileRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<ArchiveDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;

            _context = new ArchiveDbContext(options);

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\MSSQLLocalDB;Database=ArchiveSystem_Test;Trusted_Connection=true;TrustServerCertificate=true;"
                })
                .Build();

            _auditService = new NoOpAuditService();
            _httpContextAccessor = new HttpContextAccessor();

            _repository = new SqlFileRepository(
                _context,
                _configuration,
                _auditService,
                _httpContextAccessor,
                NullLogger<SqlFileRepository>.Instance);
        }

        [Fact]
        public async Task GetByIdAsync_ExistingFile_ReturnsFile()
        {
            // Arrange
            var file = new DomainFile(
                1L,
                "test.pdf",
                FileSize.Create(1024),
                FileExtension.Create(".pdf"),
                "application/pdf",
                "/files/test.pdf"
            );
            await _context.Files.AddAsync(file);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByIdAsync(1L);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1L, result.Id);
            Assert.Equal("test.pdf", result.Name);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistentFile_ReturnsNull()
        {
            // Act
            var result = await _repository.GetByIdAsync(999L);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByIdAsync_DeletedFile_ReturnsNull()
        {
            // Arrange
            var file = new DomainFile(
                1L,
                "test.pdf",
                FileSize.Create(1024),
                FileExtension.Create(".pdf"),
                "application/pdf",
                "/files/test.pdf"
            );
            file.SoftDelete(); // Mark as deleted
            await _context.Files.AddAsync(file);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByIdAsync(1L);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task AddAsync_NewFile_AddsToFile()
        {
            // Arrange
            var file = new DomainFile(
                1L,
                "new_file.pdf",
                FileSize.Create(2048),
                FileExtension.Create(".pdf"),
                "application/pdf",
                "/files/new_file.pdf"
            );

            // Act
            await _repository.AddAsync(file);
            await _repository.SaveChangesAsync();

            // Assert
            var addedFile = await _context.Files.FirstOrDefaultAsync(f => f.Id == 1L);
            Assert.NotNull(addedFile);
            Assert.Equal("new_file.pdf", addedFile.Name);
            Assert.Equal(2048, addedFile.Size.Bytes);
        }

        [Fact]
        public async Task UpdateAsync_ExistingFile_UpdatesFile()
        {
            // Arrange
            var file = new DomainFile(
                1L,
                "original.pdf",
                FileSize.Create(1024),
                FileExtension.Create(".pdf"),
                "application/pdf",
                "/files/original.pdf"
            );
            await _context.Files.AddAsync(file);
            await _context.SaveChangesAsync();

            // Update file properties
            file.UpdateStatus(FileStatus.Ready);
            file.AssignToCase(123);

            // Act
            await _repository.UpdateAsync(file);
            await _repository.SaveChangesAsync();

            // Assert
            var updatedFile = await _context.Files.FirstAsync(f => f.Id == 1L);
            Assert.Equal(FileStatus.Ready, updatedFile.Status);
            Assert.Equal(123L, updatedFile.CaseId);
        }

        [Fact]
        public async Task DeleteAsync_File_SoftDeletesFile()
        {
            // Arrange
            var file = new DomainFile(
                1L,
                "to_delete.pdf",
                FileSize.Create(1024),
                FileExtension.Create(".pdf"),
                "application/pdf",
                "/files/to_delete.pdf"
            );
            await _context.Files.AddAsync(file);
            await _context.SaveChangesAsync();

            // Act
            await _repository.DeleteAsync(file);
            await _repository.SaveChangesAsync();

            // Assert
            var deletedFile = await _context.Files.FirstOrDefaultAsync(f => f.Id == 1L);
            Assert.NotNull(deletedFile);
            Assert.Equal(FileStatus.Deleted, deletedFile.Status);
            Assert.NotNull(deletedFile.DeletedAt);
        }

        [Fact]
        public async Task GetByCaseIdAsync_ExistingCase_ReturnsFiles()
        {
            // Arrange
            var file1 = new DomainFile(
                1L,
                "file1.pdf",
                FileSize.Create(1024),
                FileExtension.Create(".pdf"),
                "application/pdf",
                "/files/file1.pdf",
                caseId: 123);

            var file2 = new DomainFile(
                2L,
                "file2.pdf",
                FileSize.Create(2048),
                FileExtension.Create(".pdf"),
                "application/pdf",
                "/files/file2.pdf",
                caseId: 123);

            var file3 = new DomainFile(
                3L,
                "file3.pdf",
                FileSize.Create(512),
                FileExtension.Create(".pdf"),
                "application/pdf",
                "/files/file3.pdf",
                caseId: 456); // Different case

            await _context.Files.AddRangeAsync(file1, file2, file3);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByCaseIdAsync(123L);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(result, f => f.Id == 1L);
            Assert.Contains(result, f => f.Id == 2L);
            Assert.DoesNotContain(result, f => f.Id == 3L);
        }

        [Fact]
        public async Task GetByStatusAsync_ExistingStatus_ReturnsFiles()
        {
            // Arrange
            var file1 = new DomainFile(
                1L,
                "pending_file.pdf",
                FileSize.Create(1024),
                FileExtension.Create(".pdf"),
                "application/pdf",
                "/files/pending_file.pdf"
            );
            file1.UpdateStatus(FileStatus.Pending);

            var file2 = new DomainFile(
                2L,
                "ready_file.pdf",
                FileSize.Create(2048),
                FileExtension.Create(".pdf"),
                "application/pdf",
                "/files/ready_file.pdf"
            );
            file2.UpdateStatus(FileStatus.Ready);

            var file3 = new DomainFile(
                3L,
                "another_pending.pdf",
                FileSize.Create(512),
                FileExtension.Create(".pdf"),
                "application/pdf",
                "/files/another_pending.pdf"
            );
            file3.UpdateStatus(FileStatus.Pending);

            await _context.Files.AddRangeAsync(file1, file2, file3);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByStatusAsync(FileStatus.Pending);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(result, f => f.Id == 1L);
            Assert.Contains(result, f => f.Id == 3L);
            Assert.DoesNotContain(result, f => f.Id == 2L);
        }

        [Fact]
        public async Task CountAsync_ReturnsCorrectCount()
        {
            // Arrange
            var files = new[]
            {
                new DomainFile(1L, "file1.pdf", FileSize.Create(1024), FileExtension.Create(".pdf"), "application/pdf", "/files/file1.pdf"),
                new DomainFile(2L, "file2.pdf", FileSize.Create(2048), FileExtension.Create(".pdf"), "application/pdf", "/files/file2.pdf"),
                new DomainFile(3L, "file3.pdf", FileSize.Create(512), FileExtension.Create(".pdf"), "application/pdf", "/files/file3.pdf")
            };
            await _context.Files.AddRangeAsync(files);
            await _context.SaveChangesAsync();

            // Act
            var count = await _repository.CountAsync();

            // Assert
            Assert.Equal(3, count);
        }

        [Fact]
        public async Task ExistsAsync_ExistingFile_ReturnsTrue()
        {
            // Arrange
            var file = new DomainFile(
                1L,
                "exists.pdf",
                FileSize.Create(1024),
                FileExtension.Create(".pdf"),
                "application/pdf",
                "/files/exists.pdf"
            );
            await _context.Files.AddAsync(file);
            await _context.SaveChangesAsync();

            // Act
            var exists = await _repository.ExistsAsync(1L);

            // Assert
            Assert.True(exists);
        }

        [Fact]
        public async Task ExistsAsync_NonExistentFile_ReturnsFalse()
        {
            // Act
            var exists = await _repository.ExistsAsync(999L);

            // Assert
            Assert.False(exists);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }

        private sealed class NoOpAuditService : IAuditService
        {
            public Task LogOperationAsync(long userId, string userName, string action, string tableName, string recordId, string? oldValue, string? newValue, string ipAddress, string userAgent, string severity = "Information")
            {
                return Task.CompletedTask;
            }

            public Task LogFileOperationAsync(long userId, string userName, FileEntity file, string action, string ipAddress, string userAgent)
            {
                return Task.CompletedTask;
            }

            public Task LogCaseOperationAsync(long userId, string userName, Case caseEntity, string action, string ipAddress, string userAgent)
            {
                return Task.CompletedTask;
            }
        }
    }
}
