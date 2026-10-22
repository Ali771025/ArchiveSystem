// src/ArchiveSystem.Infrastructure/Services/AuditReportService.cs
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.IO.Compression;
using System.Reflection;

namespace ArchiveSystem.Infrastructure.Services
{
    public interface IAuditReportService
    {
        Task<byte[]> GenerateExcelReportAsync(DateTime startDate, DateTime endDate, string? action = null, long? userId = null);
        Task<byte[]> GenerateZipReportAsync(DateTime startDate, DateTime endDate);
    }

    public class AuditReportService : IAuditReportService
    {
        private readonly ArchiveDbContext _context;

        public AuditReportService(ArchiveDbContext context)
        {
            _context = context;
            ConfigureEpplusLicense();
        }

        public async Task<byte[]> GenerateExcelReportAsync(DateTime startDate, DateTime endDate, string? action = null, long? userId = null)
        {
            var query = _context.AuditLogs.AsQueryable();

            query = query.Where(log => log.CreatedAt >= startDate && log.CreatedAt <= endDate);

            if (!string.IsNullOrEmpty(action))
            {
                query = query.Where(log => log.Action == action);
            }

            if (userId.HasValue)
            {
                query = query.Where(log => log.UserId == userId.Value);
            }

            var auditLogs = await query
                .OrderByDescending(log => log.CreatedAt)
                .ToListAsync();

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Audit Logs");

            // Headers
            worksheet.Cells[1, 1].Value = "ID";
            worksheet.Cells[1, 2].Value = "User ID";
            worksheet.Cells[1, 3].Value = "Username";
            worksheet.Cells[1, 4].Value = "Action";
            worksheet.Cells[1, 5].Value = "Table Name";
            worksheet.Cells[1, 6].Value = "Record ID";
            worksheet.Cells[1, 7].Value = "Old Value";
            worksheet.Cells[1, 8].Value = "New Value";
            worksheet.Cells[1, 9].Value = "IP Address";
            worksheet.Cells[1, 10].Value = "User Agent";
            worksheet.Cells[1, 11].Value = "Severity";
            worksheet.Cells[1, 12].Value = "Created At";

            // Data
            for (int i = 0; i < auditLogs.Count; i++)
            {
                var log = auditLogs[i];
                var row = i + 2;
                
                worksheet.Cells[row, 1].Value = log.Id;
                worksheet.Cells[row, 2].Value = log.UserId;
                worksheet.Cells[row, 3].Value = log.UserName;
                worksheet.Cells[row, 4].Value = log.Action;
                worksheet.Cells[row, 5].Value = log.TableName;
                worksheet.Cells[row, 6].Value = log.RecordId;
                worksheet.Cells[row, 7].Value = log.OldValue;
                worksheet.Cells[row, 8].Value = log.NewValue;
                worksheet.Cells[row, 9].Value = log.IpAddress;
                worksheet.Cells[row, 10].Value = log.UserAgent;
                worksheet.Cells[row, 11].Value = log.Severity;
                worksheet.Cells[row, 12].Value = log.CreatedAt;
            }

            // Format headers
            using (var range = worksheet.Cells[1, 1, 1, 12])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            // Auto-fit columns
            worksheet.Cells.AutoFitColumns();

            return package.GetAsByteArray();
        }

        public async Task<byte[]> GenerateZipReportAsync(DateTime startDate, DateTime endDate)
        {
            var auditLogs = await _context.AuditLogs
                .Where(log => log.CreatedAt >= startDate && log.CreatedAt <= endDate)
                .OrderByDescending(log => log.CreatedAt)
                .ToListAsync();

            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                // Add audit logs as CSV
                var auditEntry = archive.CreateEntry("audit_logs.csv");
                using (var entryStream = auditEntry.Open())
                using (var writer = new StreamWriter(entryStream))
                {
                    writer.WriteLine("ID,UserId,Username,Action,TableName,RecordId,OldValue,NewValue,IpAddress,UserAgent,Severity,CreatedAt");
                    
                    foreach (var log in auditLogs)
                    {
                        writer.WriteLine($"\"{log.Id}\",\"{log.UserId}\",\"{log.UserName}\",\"{log.Action}\",\"{log.TableName}\",\"{log.RecordId}\",\"{log.OldValue}\",\"{log.NewValue}\",\"{log.IpAddress}\",\"{log.UserAgent}\",\"{log.Severity}\",\"{log.CreatedAt}\"");
                    }
                }

                // Add summary report
                var summaryEntry = archive.CreateEntry("summary.txt");
                using (var entryStream = summaryEntry.Open())
                using (var writer = new StreamWriter(entryStream))
                {
                    writer.WriteLine($"Audit Report Summary");
                    writer.WriteLine($"Period: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
                    writer.WriteLine($"Total Records: {auditLogs.Count}");
                    writer.WriteLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}");
                    
                    // Action breakdown
                    var actionBreakdown = auditLogs.GroupBy(log => log.Action)
                        .Select(g => new { Action = g.Key, Count = g.Count() })
                        .OrderByDescending(x => x.Count);
                    
                    writer.WriteLine("\nAction Breakdown:");
                    foreach (var breakdown in actionBreakdown)
                    {
                        writer.WriteLine($"{breakdown.Action}: {breakdown.Count}");
                    }
                }
            }

            return memoryStream.ToArray();
        }

        private static void ConfigureEpplusLicense()
        {
            var licenseProperty = typeof(ExcelPackage).GetProperty("License", BindingFlags.Public | BindingFlags.Static);
            var licenseValue = licenseProperty?.GetValue(null);
            if (licenseValue == null)
            {
                return;
            }

            var setNonCommercialPersonal = licenseValue.GetType().GetMethod("SetNonCommercialPersonal", new[] { typeof(string) });
            setNonCommercialPersonal?.Invoke(licenseValue, new object[] { "ArchiveSystem" });
        }
    }
}
