using System.Text.Json;
using System.Text.Json.Serialization;
using ArchiveSystem.Application.AccessControl;
using ArchiveSystem.Application.Features.Reports.Interfaces;
using ArchiveSystem.Application.Features.Reports.Models;
using ArchiveSystem.Application.Options;
using ArchiveSystem.Domain.Enums;
using ArchiveSystem.Domain.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchiveSystem.Infrastructure.Services.Exports
{
    public class ExportJobExecutionService : IExportJobExecutionService
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly IExportJobRepository _exportJobRepository;
        private readonly IAccessControlService _accessControlService;
        private readonly IActivityExcelExporter _activityExcelExporter;
        private readonly IFilesZipExporter _filesZipExporter;
        private readonly IStorageService _storageService;
        private readonly ObjectStorageOptions _objectStorageOptions;
        private readonly ILogger<ExportJobExecutionService> _logger;

        public ExportJobExecutionService(
            IExportJobRepository exportJobRepository,
            IAccessControlService accessControlService,
            IActivityExcelExporter activityExcelExporter,
            IFilesZipExporter filesZipExporter,
            IStorageService storageService,
            IOptions<ObjectStorageOptions> objectStorageOptions,
            ILogger<ExportJobExecutionService> logger)
        {
            _exportJobRepository = exportJobRepository;
            _accessControlService = accessControlService;
            _activityExcelExporter = activityExcelExporter;
            _filesZipExporter = filesZipExporter;
            _storageService = storageService;
            _objectStorageOptions = objectStorageOptions.Value;
            _logger = logger;
        }

        public async Task ExecuteAsync(Guid jobId, CancellationToken cancellationToken = default)
        {
            var job = await _exportJobRepository.GetByIdForUpdateAsync(jobId, cancellationToken);
            if (job == null || job.Status != ExportJobStatus.Running)
            {
                return;
            }

            if (job.ExpiresAt <= DateTime.UtcNow)
            {
                job.MarkExpired();
                await _exportJobRepository.SaveChangesAsync(cancellationToken);
                return;
            }

            var context = await BuildAccessContextAsync(job.CreatedByUserId, cancellationToken);
            if (!context.IsAdmin && context.AllowedClassificationIds.Count == 0)
            {
                job.MarkFailed("User has no readable classifications for export.");
                await _exportJobRepository.SaveChangesAsync(cancellationToken);
                return;
            }

            var tempPath = Path.Combine(Path.GetTempPath(), $"{job.Id:N}.{GetExtension(job.Type)}");
            try
            {
                job.SetProgress(10);
                await _exportJobRepository.SaveChangesAsync(cancellationToken);

                switch (job.Type)
                {
                    case ExportJobType.ActivityExcel:
                    {
                        var request = Deserialize<ActivityExcelExportRequest>(job.RequestJson) ?? new ActivityExcelExportRequest();
                        await _activityExcelExporter.GenerateAsync(tempPath, request.Filter, context, cancellationToken);
                        break;
                    }
                    case ExportJobType.FilesZip:
                    {
                        var request = Deserialize<FilesZipExportRequest>(job.RequestJson) ?? new FilesZipExportRequest();
                        _ = await _filesZipExporter.GenerateAsync(tempPath, request, context, cancellationToken);
                        break;
                    }
                    default:
                        throw new InvalidOperationException($"Unsupported export job type '{job.Type}'.");
                }

                job.SetProgress(90);
                await _exportJobRepository.SaveChangesAsync(cancellationToken);

                var objectKey = $"exports/{job.CreatedByUserId}/{job.Id}/result.{GetExtension(job.Type)}";
                await using (var stream = new FileStream(
                    tempPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    1024 * 64,
                    FileOptions.SequentialScan))
                {
                    await _storageService.UploadStreamAsync(_objectStorageOptions.BucketName, objectKey, stream, cancellationToken);
                }

                job.MarkCompleted(objectKey, DateTime.UtcNow.AddHours(24));
                await _exportJobRepository.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Export job {JobId} failed.", jobId);
                job.MarkFailed(Truncate(ex.Message, 2000));
                await _exportJobRepository.SaveChangesAsync(cancellationToken);
            }
            finally
            {
                if (System.IO.File.Exists(tempPath))
                {
                    System.IO.File.Delete(tempPath);
                }
            }
        }

        private async Task<ActivityReportContext> BuildAccessContextAsync(long userId, CancellationToken cancellationToken)
        {
            var isAdmin = await _accessControlService.HasGlobalPermissionAsync(userId, "admin.access", cancellationToken);
            if (isAdmin)
            {
                return new ActivityReportContext { IsAdmin = true };
            }

            var allowedClassificationIds = await _accessControlService.GetAccessibleClassificationIdsAsync(
                userId,
                ClassificationPermissionMask.Read,
                cancellationToken);
            var allowedTagIds = await _accessControlService.GetAllowedTagIdsAsync(
                userId,
                TagPermissionMask.View,
                cancellationToken);

            return new ActivityReportContext
            {
                IsAdmin = false,
                AllowedClassificationIds = allowedClassificationIds,
                AllowedTagIds = allowedTagIds
            };
        }

        private static T? Deserialize<T>(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }

        private static string GetExtension(ExportJobType type) => type switch
        {
            ExportJobType.ActivityExcel => "xlsx",
            ExportJobType.FilesZip => "zip",
            _ => "dat"
        };

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            {
                return value;
            }

            return value[..maxLength];
        }
    }
}
