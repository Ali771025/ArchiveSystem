using ArchiveSystem.Application.Features.Reports.Interfaces;
using ArchiveSystem.Application.Features.Reports.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace ArchiveSystem.Infrastructure.Services.Exports
{
    public class ActivityExcelExporter : IActivityExcelExporter
    {
        private readonly IMovementLogRepository _movementLogRepository;

        public ActivityExcelExporter(IMovementLogRepository movementLogRepository)
        {
            _movementLogRepository = movementLogRepository;
        }

        public async Task GenerateAsync(
            string outputPath,
            ActivityReportFilter filter,
            ActivityReportContext context,
            CancellationToken cancellationToken = default)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? Path.GetTempPath());

            using var document = SpreadsheetDocument.Create(outputPath, SpreadsheetDocumentType.Workbook);
            var workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();

            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            using (var writer = OpenXmlWriter.Create(worksheetPart))
            {
                writer.WriteStartElement(new Worksheet());
                writer.WriteStartElement(new SheetData());

                WriteRow(writer, new[]
                {
                    "timestamp",
                    "actorUserId",
                    "actorName",
                    "action",
                    "entityType",
                    "entityId",
                    "classificationId",
                    "caseId",
                    "fileId",
                    "fileVersionId",
                    "tagId",
                    "ip",
                    "userAgent"
                });

                await foreach (var item in _movementLogRepository.StreamActivityAsync(filter, context, batchSize: 1000, cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    WriteRow(writer, new[]
                    {
                        item.Timestamp.ToString("O"),
                        item.ActorUserId.ToString(),
                        item.ActorName ?? string.Empty,
                        item.ActionType,
                        item.EntityType,
                        item.EntityId,
                        item.ClassificationId?.ToString() ?? string.Empty,
                        item.CaseId?.ToString() ?? string.Empty,
                        item.FileId?.ToString() ?? string.Empty,
                        item.FileVersionId?.ToString() ?? string.Empty,
                        item.TagId?.ToString() ?? string.Empty,
                        item.Ip ?? string.Empty,
                        item.UserAgent ?? string.Empty
                    });
                }

                writer.WriteEndElement();
                writer.WriteEndElement();
            }

            var sheets = workbookPart.Workbook.AppendChild(new Sheets());
            sheets.Append(new Sheet
            {
                Id = workbookPart.GetIdOfPart(worksheetPart),
                SheetId = 1,
                Name = "Activity"
            });

            workbookPart.Workbook.Save();
        }

        private static void WriteRow(OpenXmlWriter writer, IReadOnlyList<string> values)
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
    }
}
