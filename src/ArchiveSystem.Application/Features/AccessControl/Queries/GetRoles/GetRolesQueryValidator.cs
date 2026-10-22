using FluentValidation;

namespace ArchiveSystem.Application.Features.AccessControl.Queries.GetRoles
{
    public sealed class GetRolesQueryValidator : AbstractValidator<GetRolesQuery>
    {
        public GetRolesQueryValidator()
        {
        }
    }
}
