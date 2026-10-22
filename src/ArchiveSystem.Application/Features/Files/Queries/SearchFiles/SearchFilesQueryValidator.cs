// src/ArchiveSystem.Application/Features/Files/Queries/SearchFiles/SearchFilesQueryValidator.cs
using FluentValidation;

namespace ArchiveSystem.Application.Features.Files.Queries.SearchFiles
{
    public class SearchFilesQueryValidator : AbstractValidator<SearchFilesQuery>
    {
        public SearchFilesQueryValidator()
        {
            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1)
                .WithMessage("Page number must be greater than or equal to 1.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100)
                .WithMessage("Page size must be between 1 and 100.");

            RuleFor(x => x.FromDate)
                .LessThanOrEqualTo(x => x.ToDate)
                .When(x => x.FromDate.HasValue && x.ToDate.HasValue)
                .WithMessage("From date cannot be greater than to date.");

            RuleFor(x => x.Term)
                .MaximumLength(500)
                .When(x => !string.IsNullOrEmpty(x.Term))
                .WithMessage("Search term cannot exceed 500 characters.");
        }
    }
}