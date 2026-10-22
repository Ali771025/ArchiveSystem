using System.Text.Json;
using System.Text.Json.Serialization;
using ArchiveSystem.Application.Common.Exceptions;
using ArchiveSystem.Application.Features.Reports.Interfaces;
using ArchiveSystem.Application.Features.Reports.Models;
using ArchiveSystem.Application.Options;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Domain.Enums;
using ArchiveSystem.Domain.Services;
using Microsoft.Extensions.Options;

namespace ArchiveSystem.Infrastructure.Services.Exports
{
    public class ExportService : IExportService
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly IExportJobRepository _exportJobRepository;
        private readonly IStorageService _storageService;
        private readonly ObjectStorageOptions _objectStorageOptions;

        public ExportService(
            IExportJobRepository exportJobRepository,
            IStorageService storageService,
            IOptions<ObjectStorageOptions> objectStorageOptions)
        {
            _exportJobRepository = exportJobRepository;
            _storageService = storageService;
            _objectStorageOptions = objectStorageOptions.Value;
        }

        public async Task<ExportJobDto> CreateActivityExcelJobAsync(long userId, ActivityExcelExportRequest request, CancellationToken cancellationToken = default)
        {
            var payload = SerializeRequest(request);
            var job = new ExportJob(
                id: Guid.NewGuid(),
                createdByUserId: userId,
                type: ExportJobType.ActivityExcel,
                requestJson: payload,
                expiresAt: DateTime.UtcNow.AddHours(24));

            await _exportJobRepository.AddAsync(job, cancellationToken);
            await _exportJobRepository.SaveChangesAsync(cancellationToken);
            return Map(job);
        }

        public async Task<ExportJobDto> CreateFilesZipJobAsync(long userId, FilesZipExportRequest request, CancellationToken cancellationToken = default)
        {
            var payload = SerializeRequest(request);
            var job = new ExportJob(
                id: Guid.NewGuid(),
                createdByUserId: userId,
                type: ExportJobType.FilesZip,
                requestJson: payload,
                expiresAt: DateTime.UtcNow.AddHours(24));

            await _exportJobRepository.AddAsync(job, cancellationToken);
            await _exportJobRepository.SaveChangesAsync(cancellationToken);
            return Map(job);
        }

        public async Task<ExportJobDto?> GetJobStatusAsync(Guid jobId, long requesterUserId, bool allowManageAll, CancellationToken cancellationToken = default)
        {
            var job = await _exportJobRepository.GetByIdAsync(jobId, cancellationToken);
            if (job == null)
            {
                return null;
            }

            EnsureCanAccessJob(job, requesterUserId, allowManageAll);
            await MarkExpiredIfNeededAsync(job, cancellationToken);

            var refreshed = await _exportJobRepository.GetByIdAsync(jobId, cancellationToken);
            return refreshed == null ? null : Map(refreshed);
        }

        public async Task<ExportDownloadResult?> GetJobDownloadAsync(Guid jobId, long requesterUserId, bool allowManageAll, CancellationToken cancellationToken = default)
        {
            var job = await _exportJobRepository.GetByIdAsync(jobId, cancellationToken);
            if (job == null)
            {
                return null;
            }

            EnsureCanAccessJob(job, requesterUserId, allowManageAll);
            await MarkExpiredIfNeededAsync(job, cancellationToken);

            var refreshed = await _exportJobRepository.GetByIdAsync(jobId, cancellationToken);
            if (refreshed == null || refreshed.Status != ExportJobStatus.Completed || string.IsNullOrWhiteSpace(refreshed.ResultObjectKey))
            {
                return null;
            }

            var stream = await _storageService.GetObjectStreamAsync(_objectStorageOptions.BucketName, refreshed.ResultObjectKey, cancellationToken);
            return new ExportDownloadResult
            {
                Content = stream,
                ContentType = GetContentType(refreshed.Type),
                FileName = $"{refreshed.Type}-{refreshed.Id:N}.{GetExtension(refreshed.Type)}"
            };
        }

        private async Task MarkExpiredIfNeededAsync(ExportJob job, CancellationToken cancellationToken)
        {
            if (job.Status == ExportJobStatus.Completed && job.ExpiresAt <= DateTime.UtcNow)
            {
                var tracked = await _exportJobRepository.GetByIdForUpdateAsync(job.Id, cancellationToken);
                if (tracked != null && tracked.Status == ExportJobStatus.Completed && tracked.ExpiresAt <= DateTime.UtcNow)
                {
                    tracked.MarkExpired();
                    await _exportJobRepository.SaveChangesAsync(cancellationToken);
                }
            }
        }

        private static void EnsureCanAccessJob(ExportJob job, long requesterUserId, bool allowManageAll)
        {
            if (allowManageAll || requesterUserId == job.CreatedByUserId)
            {
                return;
            }

            throw new ArchiveSystem.Application.Common.Exceptions.UnauthorizedAccessException("You do not have permission to access this export job.");
        }

        private static ExportJobDto Map(ExportJob job)
        {
            return new ExportJobDto
            {
                Id = job.Id,
                CreatedByUserId = job.CreatedByUserId,
                Type = job.Type,
                Status = job.Status,
                ProgressPercent = job.ProgressPercent,
                ErrorMessage = job.ErrorMessage,
                CreatedAt = job.CreatedAt,
                ExpiresAt = job.ExpiresAt,
                StartedAt = job.StartedAt,
                CompletedAt = job.CompletedAt
            };
        }

        private static string SerializeRequest<T>(T request)
        {
            var payload = JsonSerializer.Serialize(request, JsonOptions);
            return payload.Length <= 2000 ? payload : payload[..2000];
        }

        private static string GetExtension(ExportJobType type) => type switch
        {
            ExportJobType.ActivityExcel => "xlsx",
            ExportJobType.FilesZip => "zip",
            _ => "dat"
        };

        private static string GetContentType(ExportJobType type) => type switch
        {
            ExportJobType.ActivityExcel => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ExportJobType.FilesZip => "application/zip",
            _ => "application/octet-stream"
        };
    }
}
