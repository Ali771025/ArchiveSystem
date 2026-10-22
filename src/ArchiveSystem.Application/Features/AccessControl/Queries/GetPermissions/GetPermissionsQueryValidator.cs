using FluentValidation;

namespace ArchiveSystem.Application.Features.AccessControl.Queries.GetPermissions
{
    public sealed class GetPermissionsQueryValidator : AbstractValidator<GetPermissionsQuery>
    {
        public GetPermissionsQueryValidator()
        {
        }
    }
}
