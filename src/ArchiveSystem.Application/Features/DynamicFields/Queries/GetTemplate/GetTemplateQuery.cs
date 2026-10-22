using ArchiveSystem.Application.Common.Models.DynamicFields;
using ArchiveSystem.Application.Features.Classifications.Security;
using ArchiveSystem.Application.Interfaces.Persistence;
using MediatR;

namespace ArchiveSystem.Application.Features.DynamicFields.Queries.GetTemplate
{
    public sealed class GetTemplateQuery : IRequest<DynamicTemplateDto?>
    {
        public GetTemplateQuery(long classificationId)
        {
            ClassificationId = classificationId;
        }

        public long ClassificationId { get; }
    }

    public sealed class GetTemplateQueryHandler : IRequestHandler<GetTemplateQuery, DynamicTemplateDto?>
    {
        private readonly IDynamicTemplateRepository _dynamicTemplateRepository;
        private readonly IClassificationAccessAuthorizer _authorizer;

        public GetTemplateQueryHandler(
            IDynamicTemplateRepository dynamicTemplateRepository,
            IClassificationAccessAuthorizer authorizer)
        {
            _dynamicTemplateRepository = dynamicTemplateRepository;
            _authorizer = authorizer;
        }

        public async Task<DynamicTemplateDto?> Handle(GetTemplateQuery request, CancellationToken cancellationToken)
        {
            await _authorizer.EnsureCanReadAsync(request.ClassificationId, cancellationToken);
            return await _dynamicTemplateRepository.GetByClassificationIdAsync(request.ClassificationId, cancellationToken);
        }
    }
}
