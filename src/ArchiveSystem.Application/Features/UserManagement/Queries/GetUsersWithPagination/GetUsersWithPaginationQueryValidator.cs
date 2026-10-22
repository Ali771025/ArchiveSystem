using FluentValidation;

namespace ArchiveSystem.Application.Features.UserManagement.Queries.GetUsersWithPagination
{
    public sealed class GetUsersWithPaginationQueryValidator : AbstractValidator<GetUsersWithPaginationQuery>
    {
        private static readonly HashSet<string> AllowedSortColumns = new(StringComparer.OrdinalIgnoreCase)
        {
            "UserName",
            "Email",
            "FirstName",
            "LastName",
            "NationalId",
            "PersonnelCode",
            "CreatedAt",
            "LastModified",
            "IsActive"
        };

        public GetUsersWithPaginationQueryValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThanOrEqualTo(1)
                .WithMessage("PageNumber must be greater than or equal to 1.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100)
                .WithMessage("PageSize must be between 1 and 100.");

            RuleFor(x => x.SortBy)
                .Must(sortBy => string.IsNullOrWhiteSpace(sortBy) || AllowedSortColumns.Contains(sortBy))
                .WithMessage("SortBy is invalid.");

            RuleFor(x => x.Username)
                .MaximumLength(256)
                .When(x => !string.IsNullOrWhiteSpace(x.Username));

            RuleFor(x => x.Name)
                .MaximumLength(200)
                .When(x => !string.IsNullOrWhiteSpace(x.Name));

            RuleFor(x => x.NationalId)
                .MaximumLength(20)
                .When(x => !string.IsNullOrWhiteSpace(x.NationalId));
        }
    }
}
