using System.Drawing;
using System.Reflection;
using ArchiveSystem.Application.Common.Models.DynamicFields;
using ArchiveSystem.Application.Features.Cases.Models;
using ArchiveSystem.Application.Features.Cases.Queries.ExportCaseWorkbook;
using ArchiveSystem.Domain.Enums;
using ArchiveSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace ArchiveSystem.Infrastructure.Services.Exports;

public sealed class CaseWorkbookExporter : ICaseWorkbookExporter
{
    private readonly ArchiveDbContext _dbContext;

    public CaseWorkbookExporter(ArchiveDbContext dbContext)
    {
        _dbContext = dbContext;
        ConfigureEpplusLicense();
    }

    public async Task<byte[]> GenerateAsync(
        CaseDetailsDto caseDetails,
        CaseDynamicFieldsDto dynamicFields,
        CancellationToken cancellationToken = default)
    {
        var files = await _dbContext.Files
            .AsNoTracking()
            .Where(file => file.CaseId == caseDetails.Id && file.DeletedAt == null)
            .OrderBy(file => file.Name)
            .Select(file => new
            {
                file.Id,
                file.Name,
                Extension = file.Extension.Value,
                file.ContentType,
                Size = file.Size.Bytes,
                file.Status,
                file.CreatedAt,
                file.ModifiedAt
            })
            .ToListAsync(cancellationToken);

        using var package = new ExcelPackage();
        WriteCaseSheet(package, caseDetails);
        WriteOwnersSheet(package, caseDetails.Owners);
        WriteDynamicFieldsSheet(package, dynamicFields);
        WriteFilesSheet(package, files.Select(file => new object?[]
        {
            file.Id, file.Name, file.Extension, file.ContentType, file.Size,
            file.Status.ToString(), file.CreatedAt, file.ModifiedAt
        }).ToList());
        WriteHistorySheet(package, caseDetails.History);

        return package.GetAsByteArray();
    }

    private static void WriteCaseSheet(ExcelPackage package, CaseDetailsDto details)
    {
        var sheet = CreateSheet(package, "اطلاعات پرونده");
        WriteHeader(sheet, "عنوان", "مقدار");
        var rows = new (string Label, object? Value)[]
        {
            ("شناسه پرونده", details.Id),
            ("شماره پرونده", details.CaseNumber),
            ("شماره بیرونی", details.ExternalCaseNumber),
            ("طبقه‌بندی", details.ClassificationTitle),
            ("نشانی", details.Address),
            ("کد پستی", details.PostalCode),
            ("آخرین بروزرسانی", details.UpdatedAt)
        };
        for (var index = 0; index < rows.Length; index++)
        {
            sheet.Cells[index + 2, 1].Value = rows[index].Label;
            sheet.Cells[index + 2, 2].Value = rows[index].Value;
        }
        FinalizeSheet(sheet);
    }

    private static void WriteOwnersSheet(ExcelPackage package, IReadOnlyList<CaseOwnerDto> owners)
    {
        var sheet = CreateSheet(package, "مالکین");
        WriteHeader(sheet, "ردیف", "نام و نام خانوادگی", "کد ملی", "شناسه مودی", "تلفن");
        for (var index = 0; index < owners.Count; index++)
        {
            var owner = owners[index];
            WriteRow(sheet, index + 2, index + 1, owner.FullName, owner.NationalCode, owner.TaxpayerId, owner.Phone);
        }
        FinalizeSheet(sheet);
    }

    private static void WriteDynamicFieldsSheet(ExcelPackage package, CaseDynamicFieldsDto fields)
    {
        var sheet = CreateSheet(package, "فیلدهای پویا");
        WriteHeader(sheet, "ردیف", "عنوان فیلد", "کلید", "نوع", "مقدار");
        for (var index = 0; index < fields.Values.Count; index++)
        {
            var field = fields.Values[index];
            WriteRow(sheet, index + 2, index + 1, field.Label, field.Key, field.FieldType.ToString(), FormatDynamicValue(field));
        }
        FinalizeSheet(sheet);
    }

    private static string FormatDynamicValue(CaseDynamicFieldValueDto field)
    {
        if (field.OptionIds.Count > 0)
        {
            var labels = field.OptionIds.Select(optionId =>
                field.Options.FirstOrDefault(option => option.Id == optionId)?.Label ?? optionId.ToString());
            return string.Join("، ", labels);
        }

        if (field.BoolValue.HasValue) return field.BoolValue.Value ? "بله" : "خیر";
        if (field.DateValue.HasValue) return field.DateValue.Value.ToString("yyyy/MM/dd HH:mm");
        if (field.NumberValue.HasValue) return field.NumberValue.Value.ToString("0.################");
        return field.StringValue ?? string.Empty;
    }

    private static void WriteFilesSheet(ExcelPackage package, IReadOnlyList<object?[]> rows)
    {
        var sheet = CreateSheet(package, "فایل‌ها");
        WriteHeader(sheet, "شناسه", "نام فایل", "پسوند", "نوع محتوا", "حجم (بایت)", "وضعیت", "تاریخ ایجاد", "آخرین تغییر");
        for (var index = 0; index < rows.Count; index++) WriteRow(sheet, index + 2, rows[index]);
        FinalizeSheet(sheet);
    }

    private static void WriteHistorySheet(ExcelPackage package, IReadOnlyList<CaseHistoryItemDto> history)
    {
        var sheet = CreateSheet(package, "تاریخچه و لاگ‌ها");
        WriteHeader(sheet, "ردیف", "زمان", "کاربر", "عملیات", "نوع موجودیت", "جزئیات");
        for (var index = 0; index < history.Count; index++)
        {
            var item = history[index];
            WriteRow(sheet, index + 2, index + 1, item.Timestamp, item.ActorName, item.ActionType, item.EntityType, item.Detail);
        }
        FinalizeSheet(sheet);
    }

    private static ExcelWorksheet CreateSheet(ExcelPackage package, string name)
    {
        var sheet = package.Workbook.Worksheets.Add(name);
        sheet.View.RightToLeft = true;
        sheet.Cells.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        return sheet;
    }

    private static void WriteHeader(ExcelWorksheet sheet, params string[] headers)
    {
        for (var column = 0; column < headers.Length; column++) sheet.Cells[1, column + 1].Value = headers[column];
        using var range = sheet.Cells[1, 1, 1, headers.Length];
        range.Style.Font.Bold = true;
        range.Style.Font.Color.SetColor(Color.White);
        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
        range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(15, 118, 110));
    }

    private static void WriteRow(ExcelWorksheet sheet, int row, params object?[] values)
    {
        for (var column = 0; column < values.Length; column++) sheet.Cells[row, column + 1].Value = values[column];
    }

    private static void FinalizeSheet(ExcelWorksheet sheet)
    {
        if (sheet.Dimension is null) return;
        sheet.Cells[sheet.Dimension.Address].AutoFitColumns(12, 48);
        sheet.View.FreezePanes(2, 1);
        sheet.Cells[sheet.Dimension.Address].Style.WrapText = true;
        sheet.Cells[sheet.Dimension.Address].AutoFilter = true;
    }

    private static void ConfigureEpplusLicense()
    {
        var license = typeof(ExcelPackage).GetProperty("License", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
        license?.GetType().GetMethod("SetNonCommercialPersonal", new[] { typeof(string) })
            ?.Invoke(license, new object[] { "ArchiveSystem" });
    }
}
