using ArchiveSystem.Application.Common.Models.DynamicFields;
using ArchiveSystem.Application.Interfaces.Persistence;
using MediatR;

namespace ArchiveSystem.Application.Features.DynamicFields.Queries.GetEffectiveTemplate;

public sealed record GetEffectiveTemplateQuery(long ClassificationId) : IRequest<DynamicTemplateDto?>;

public sealed class GetEffectiveTemplateQueryHandler
    : IRequestHandler<GetEffectiveTemplateQuery, DynamicTemplateDto?>
{
    private readonly IDynamicTemplateRepository _dynamicTemplateRepository;

    public GetEffectiveTemplateQueryHandler(IDynamicTemplateRepository dynamicTemplateRepository)
    {
        _dynamicTemplateRepository = dynamicTemplateRepository;
    }

    public async Task<DynamicTemplateDto?> Handle(
        GetEffectiveTemplateQuery request,
        CancellationToken cancellationToken)
    {
        var templates = await _dynamicTemplateRepository.GetActiveHierarchyByClassificationIdAsync(
            request.ClassificationId,
            cancellationToken);

        if (templates.Count == 0)
        {
            return null;
        }

        var effectiveGroups = templates
            .SelectMany(template => template.Groups.OrderBy(group => group.Order))
            .Select((group, index) => new DynamicFieldGroupDto
            {
                Id = group.Id,
                Title = group.Title,
                Order = index,
                Fields = group.Fields
                    .OrderBy(field => field.Order)
                    .ToList()
            })
            .ToList();

        return new DynamicTemplateDto
        {
            Id = templates[^1].Id,
            ClassificationId = request.ClassificationId,
            Name = templates[^1].Name,
            IsActive = true,
            Groups = effectiveGroups
        };
    }
}
