using FluentValidation;

namespace ArchiveSystem.Application.Features.UserManagement.Queries.GetUsersWithPagination
{
    public sealed class GetUserByIdQueryValidator : AbstractValidator<GetUserByIdQuery>
    {
        public GetUserByIdQueryValidator()
        {
            RuleFor(x => x.UserId)
                .GreaterThan(0)
                .WithMessage("UserId must be greater than 0.");
        }
    }
}
