using FluentValidation;

namespace ArchiveSystem.Application.Features.Taxpayers.Queries.GetTaxpayerById
{
    public sealed class GetTaxpayerByIdQueryValidator : AbstractValidator<GetTaxpayerByIdQuery>
    {
        public GetTaxpayerByIdQueryValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
