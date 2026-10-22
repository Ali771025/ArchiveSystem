using ArchiveSystem.Application.Common.Models.DynamicFields;
using ArchiveSystem.Application.Features.Cases.Models;

namespace ArchiveSystem.Application.Features.Cases.Queries.ExportCaseWorkbook;

public interface ICaseWorkbookExporter
{
    Task<byte[]> GenerateAsync(
        CaseDetailsDto caseDetails,
        CaseDynamicFieldsDto dynamicFields,
        CancellationToken cancellationToken = default);
}
