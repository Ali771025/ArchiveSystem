using System.IO.Compression;
using System.Net;
using System.Text;
using Amazon.S3;
using ArchiveSystem.Application.Features.Reports.Interfaces;
using ArchiveSystem.Application.Features.Reports.Models;
using ArchiveSystem.Application.Interfaces.Storage;
using ArchiveSystem.Application.Options;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchiveSystem.Infrastructure.Services.Exports
{
    public class FilesZipExporter : IFilesZipExporter
    {
        private readonly IFileExportQueryRepository _fileExportQueryRepository;
        private readonly IObjectStorage _objectStorage;
        private readonly ObjectStorageOptions _objectStorageOptions;
        private readonly ILogger<FilesZipExporter> _logger;

        public FilesZipExporter(
            IFileExportQueryRepository fileExportQueryRepository,
            IObjectStorage objectStorage,
            IOptions<ObjectStorageOptions> objectStorageOptions,
            ILogger<FilesZipExporter> logger)
        {
            _fileExportQueryRepository = fileExportQueryRepository;
            _objectStorage = objectStorage;
            _objectStorageOptions = objectStorageOptions.Value;
            _logger = logger;
        }

        public async Task<int> GenerateAsync(
            string outputPath,
            FilesZipExportRequest request,
            ActivityReportContext context,
            CancellationToken cancellationToken = default)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? Path.GetTempPath());
            var metadataPath = Path.Combine(Path.GetDirectoryName(outputPath) ?? Path.GetTempPath(), $"{Guid.NewGuid():N}-metadata.csv");
            var excelPath = Path.Combine(Path.GetDirectoryName(outputPath) ?? Path.GetTempPath(), $"{Guid.NewGuid():N}-case-files.xlsx");
            var missingObjectsPath = Path.Combine(Path.GetDirectoryName(outputPath) ?? Path.GetTempPath(), $"{Guid.NewGuid():N}-missing-objects.csv");

            var exportedCount = 0;
            try
            {
                await using var zipFileStream = new FileStream(
                    outputPath,
                    FileMode.Create,
                    FileAccess.ReadWrite,
                    FileShare.None,
                    bufferSize: 1024 * 64,
                    FileOptions.SequentialScan);

                using var archive = new ZipArchive(zipFileStream, ZipArchiveMode.Create, leaveOpen: true);
                using (var metadataWriter = new StreamWriter(metadataPath, append: false, Encoding.UTF8))
                using (var missingObjectsWriter = new StreamWriter(missingObjectsPath, append: false, Encoding.UTF8))
                {
                    await metadataWriter.WriteLineAsync("status,fileId,versionId,versionNumber,fileName,classificationId,caseId,caseNumber,caseTitle,caseOwners,caseLocation,casePostalCode,sizeBytes,tagIds,objectKey");
                    await missingObjectsWriter.WriteLineAsync("fileId,versionId,versionNumber,fileName,caseId,caseNumber,objectKey,error");

                    var manifestRows = new List<FileExportManifestRow>();

                    await foreach (var file in _fileExportQueryRepository.StreamFilesForZipExportAsync(request, context, batchSize: 200, cancellationToken))
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var tagList = string.Join('|', file.TagIds.OrderBy(x => x));
                        try
                        {
                            var entryName = $"files/{file.FileId}_{file.VersionNumber}_{SanitizeFileName(file.FileName)}";
                            await using (var sourceStream = await _objectStorage.GetObjectStreamAsync(_objectStorageOptions.BucketName, file.ObjectKey, cancellationToken))
                            {
                                var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
                                await using var entryStream = entry.Open();
                                await sourceStream.CopyToAsync(entryStream, 1024 * 64, cancellationToken);
                            }

                            await metadataWriter.WriteLineAsync(BuildMetadataCsvLine("Exported", file, tagList));
                            manifestRows.Add(FileExportManifestRow.From(file, "Exported", string.Empty));
                            exportedCount++;
                        }
                        catch (Exception ex) when (IsMissingObjectException(ex))
                        {
                            _logger.LogWarning(
                                "Skipping missing object during ZIP export. FileId={FileId}, VersionId={VersionId}, ObjectKey={ObjectKey}, Error={Error}",
                                file.FileId,
                                file.VersionId,
                                file.ObjectKey,
                                ex.Message);

                            await metadataWriter.WriteLineAsync(BuildMetadataCsvLine("MissingObject", file, tagList));
                            await missingObjectsWriter.WriteLineAsync(
                                $"{file.FileId},{file.VersionId},{file.VersionNumber},{EscapeCsv(file.FileName)},{file.CaseId?.ToString() ?? string.Empty},{EscapeCsv(file.CaseNumber)},{EscapeCsv(file.ObjectKey)},{EscapeCsv(ex.Message)}");
                            manifestRows.Add(FileExportManifestRow.From(file, "MissingObject", ex.Message));
                        }
                    }

                    await metadataWriter.FlushAsync();
                    await missingObjectsWriter.FlushAsync();
                    WriteManifestExcel(excelPath, manifestRows);
                }

                await AddFileToArchiveAsync(archive, "metadata/exported-files.csv", metadataPath, cancellationToken);
                await AddFileToArchiveAsync(archive, "metadata/missing-objects.csv", missingObjectsPath, cancellationToken);
                await AddFileToArchiveAsync(archive, "metadata/case-files.xlsx", excelPath, cancellationToken);
            }
            finally
            {
                DeleteIfExists(metadataPath);
                DeleteIfExists(excelPath);
                DeleteIfExists(missingObjectsPath);
            }

            return exportedCount;
        }

        private static async Task AddFileToArchiveAsync(
            ZipArchive archive,
            string entryName,
            string sourcePath,
            CancellationToken cancellationToken)
        {
            var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
            await using (var entryStream = entry.Open())
            await using (var fileStream = new FileStream(
                sourcePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 1024 * 64,
                FileOptions.SequentialScan))
            {
                await fileStream.CopyToAsync(entryStream, 1024 * 64, cancellationToken);
            }
        }

        private static void DeleteIfExists(string path)
        {
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }
        }

        private static string SanitizeFileName(string fileName)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sanitized = new string(fileName.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
            return string.IsNullOrWhiteSpace(sanitized) ? "file.bin" : sanitized;
        }

        private static string EscapeCsv(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var escaped = value.Replace("\"", "\"\"");
            return $"\"{escaped}\"";
        }

        private static string BuildMetadataCsvLine(string status, FileExportItemDto file, string tagList)
        {
            return string.Join(
                ",",
                EscapeCsv(status),
                file.FileId.ToString(),
                file.VersionId.ToString(),
                file.VersionNumber.ToString(),
                EscapeCsv(file.FileName),
                file.ClassificationId?.ToString() ?? string.Empty,
                file.CaseId?.ToString() ?? string.Empty,
                EscapeCsv(file.CaseNumber),
                EscapeCsv(file.CaseTitle),
                EscapeCsv(file.CaseOwners),
                EscapeCsv(file.CaseLocation),
                EscapeCsv(file.CasePostalCode),
                file.SizeBytes.ToString(),
                EscapeCsv(tagList),
                EscapeCsv(file.ObjectKey));
        }

        private static bool IsMissingObjectException(Exception exception)
        {
            var ex = exception as AmazonS3Exception;
            while (ex == null && exception.InnerException != null)
            {
                exception = exception.InnerException;
                ex = exception as AmazonS3Exception;
            }

            if (ex == null)
            {
                return exception.Message.Contains("specified key does not exist", StringComparison.OrdinalIgnoreCase)
                    || exception.Message.Contains("NoSuchKey", StringComparison.OrdinalIgnoreCase);
            }

            if (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return true;
            }

            if (string.Equals(ex.ErrorCode, "NoSuchKey", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return ex.Message.Contains("specified key does not exist", StringComparison.OrdinalIgnoreCase);
        }

        private static void WriteManifestExcel(string outputPath, IReadOnlyCollection<FileExportManifestRow> rows)
        {
            using var document = SpreadsheetDocument.Create(outputPath, SpreadsheetDocumentType.Workbook);
            var workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();

            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            using (var writer = OpenXmlWriter.Create(worksheetPart))
            {
                writer.WriteStartElement(new Worksheet());
                writer.WriteStartElement(new SheetData());

                WriteExcelRow(writer, new[]
                {
                    "status",
                    "fileId",
                    "versionId",
                    "versionNumber",
                    "fileName",
                    "contentType",
                    "sizeBytes",
                    "classificationId",
                    "caseId",
                    "caseNumber",
                    "caseTitle",
                    "caseOwners",
                    "caseLocation",
                    "casePostalCode",
                    "tagIds",
                    "objectKey",
                    "error"
                });

                foreach (var row in rows)
                {
                    WriteExcelRow(writer, row.ToValues());
                }

                writer.WriteEndElement();
                writer.WriteEndElement();
            }

            var sheets = workbookPart.Workbook.AppendChild(new Sheets());
            sheets.Append(new Sheet
            {
                Id = workbookPart.GetIdOfPart(worksheetPart),
                SheetId = 1,
                Name = "CaseFiles"
            });

            workbookPart.Workbook.Save();
        }

        private static void WriteExcelRow(OpenXmlWriter writer, IReadOnlyList<string> values)
        {
            writer.WriteStartElement(new Row());
            foreach (var value in values)
            {
                writer.WriteStartElement(new Cell { DataType = CellValues.InlineString });
                writer.WriteElement(new InlineString(new Text(value ?? string.Empty)));
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        private sealed class FileExportManifestRow
        {
            public string Status { get; private init; } = string.Empty;
            public FileExportItemDto File { get; private init; } = new();
            public string TagList { get; private init; } = string.Empty;
            public string Error { get; private init; } = string.Empty;

            public static FileExportManifestRow From(FileExportItemDto file, string status, string error)
            {
                return new FileExportManifestRow
                {
                    Status = status,
                    File = file,
                    TagList = string.Join('|', file.TagIds.OrderBy(x => x)),
                    Error = error
                };
            }

            public IReadOnlyList<string> ToValues()
            {
                return new[]
                {
                    Status,
                    File.FileId.ToString(),
                    File.VersionId.ToString(),
                    File.VersionNumber.ToString(),
                    File.FileName,
                    File.ContentType,
                    File.SizeBytes.ToString(),
                    File.ClassificationId?.ToString() ?? string.Empty,
                    File.CaseId?.ToString() ?? string.Empty,
                    File.CaseNumber,
                    File.CaseTitle,
                    File.CaseOwners,
                    File.CaseLocation,
                    File.CasePostalCode ?? string.Empty,
                    TagList,
                    File.ObjectKey,
                    Error
                };
            }
        }
    }
}
