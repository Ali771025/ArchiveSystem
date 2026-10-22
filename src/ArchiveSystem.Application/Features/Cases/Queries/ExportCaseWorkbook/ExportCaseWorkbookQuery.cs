using ArchiveSystem.Application.Common.Models.DynamicFields;
using ArchiveSystem.Application.Features.Cases.Models;
using ArchiveSystem.Application.Features.Cases.Queries.GetCaseById;
using ArchiveSystem.Application.Features.DynamicFields.Queries.GetCaseDynamicFields;
using MediatR;

namespace ArchiveSystem.Application.Features.Cases.Queries.ExportCaseWorkbook;

public sealed record ExportCaseWorkbookQuery(long CaseId) : IRequest<CaseWorkbookFileDto>;

public sealed record CaseWorkbookFileDto(byte[] Content, string FileName, string ContentType);

public sealed class ExportCaseWorkbookQueryHandler : IRequestHandler<ExportCaseWorkbookQuery, CaseWorkbookFileDto>
{
    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private readonly ISender _sender;
    private readonly ICaseWorkbookExporter _exporter;

    public ExportCaseWorkbookQueryHandler(ISender sender, ICaseWorkbookExporter exporter)
    {
        _sender = sender;
        _exporter = exporter;
    }

    public async Task<CaseWorkbookFileDto> Handle(ExportCaseWorkbookQuery request, CancellationToken cancellationToken)
    {
        // GetCaseByIdQuery is the authorization boundary for reading the case.
        var details = await _sender.Send(new GetCaseByIdQuery { CaseId = request.CaseId }, cancellationToken);
        var dynamicFields = await _sender.Send(new GetCaseDynamicFieldsQuery(request.CaseId), cancellationToken);
        var content = await _exporter.GenerateAsync(details, dynamicFields, cancellationToken);
        var safeCaseNumber = string.Concat(details.CaseNumber.Select(character =>
            Path.GetInvalidFileNameChars().Contains(character) ? '_' : character));

        return new CaseWorkbookFileDto(content, $"case-{safeCaseNumber}.xlsx", ExcelContentType);
    }
}
