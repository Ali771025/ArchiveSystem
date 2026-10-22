// src/ArchiveSystem.Application/Features/Files/Commands/UploadFile/UploadFileCommandHandler.cs
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Domain.Enums;
using ArchiveSystem.Domain.Repositories;
using ArchiveSystem.Domain.Services;
using ArchiveSystem.Domain.Events;
using ArchiveSystem.Application.Common.Models;
using ArchiveSystem.Application.Interfaces;
using FileEntity = ArchiveSystem.Domain.Entities.ArchiveFile;

namespace ArchiveSystem.Application.Features.Files.Commands.UploadFile
{
    public class UploadFileCommandHandler : IRequestHandler<UploadFileCommand, UploadFileResponse>
    {
        private readonly IFileRepository _fileRepository;
        private readonly IMessageBus _messageBus;
        private readonly IAuditService _auditService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<UploadFileCommandHandler> _logger;

        public UploadFileCommandHandler(
            IFileRepository fileRepository,
            IMessageBus messageBus,
            IAuditService auditService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<UploadFileCommandHandler> logger)
        {
            _fileRepository = fileRepository;
            _messageBus = messageBus;
            _auditService = auditService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<UploadFileResponse> Handle(UploadFileCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Generate a unique filename for storage
                var storagePath = GenerateStoragePath(request.FileName, request.TusId);
                
                // Create the file entity with pending status
                var file = new FileEntity(
                    id: GenerateFileId(),
                    name: request.FileName,
                    size: Domain.ValueObjects.FileSize.Create(request.Size),
                    extension: Domain.ValueObjects.FileExtension.Create(Path.GetExtension(request.FileName)),
                    contentType: request.ContentType,
                    storagePath: storagePath,
                    caseId: request.CaseId
                );

                file.UpdateStatus(FileStatus.Pending);

                // Save metadata to database
                await _fileRepository.AddAsync(file, cancellationToken);
                await _fileRepository.SaveChangesAsync(cancellationToken);

                // Log the file creation
                var userId = GetUserId();
                var userName = GetUserName();
                var ipAddress = GetIpAddress();
                var userAgent = GetUserAgent();

                await _auditService.LogOperationAsync(
                    userId, 
                    userName, 
                    "CREATE", 
                    "Files", 
                    file.Id.ToString(), 
                    null, 
                    System.Text.Json.JsonSerializer.Serialize(new { 
                        Id = file.Id, 
                        Name = file.Name, 
                        Size = file.Size.Bytes, 
                        Status = file.Status 
                    }), 
                    ipAddress, 
                    userAgent
                );

                // Publish event to trigger further processing (actual upload to MinIO, virus scanning, etc.)
                var fileCreatedEvent = new FileUploadedEvent(
                    fileId: file.Id,
                    fileName: file.Name,
                    fileSize: file.Size.Bytes,
                    storagePath: file.StoragePath
                );

                await _messageBus.PublishAsync(fileCreatedEvent, cancellationToken);

                _logger.LogInformation("File upload initiated for file ID: {FileId}", file.Id);

                return new UploadFileResponse
                {
                    FileId = file.Id,
                    Message = "File upload initiated successfully. Processing in background."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing UploadFileCommand for file: {FileName}", request.FileName);
                throw;
            }
        }

        private static string GenerateStoragePath(string fileName, string tusId)
        {
            var extension = Path.GetExtension(fileName);
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var randomSuffix = Guid.NewGuid().ToString("N")[..8]; // First 8 chars of GUID
            
            return $"uploads/{DateTime.UtcNow.Year}/{DateTime.UtcNow.Month:D2}/{timestamp}_{fileNameWithoutExt}_{randomSuffix}{extension}";
        }

        private static long GenerateFileId()
        {
            // In a real implementation, this would likely come from a distributed ID generator
            // For now, we'll use DateTime ticks as a simple ID generator
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        private long GetUserId()
        {
            var userIdClaim = _httpContextAccessor?.HttpContext?.User?.FindFirst("sub")?.Value;
            return long.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        private string GetUserName()
        {
            return _httpContextAccessor?.HttpContext?.User?.Identity?.Name ?? "System";
        }

        private string GetIpAddress()
        {
            return _httpContextAccessor?.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        private string GetUserAgent()
        {
            return _httpContextAccessor?.HttpContext?.Request?.Headers["User-Agent"].ToString() ?? "Unknown";
        }
    }
}
