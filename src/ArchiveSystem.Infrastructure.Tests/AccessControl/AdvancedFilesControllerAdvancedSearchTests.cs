using ArchiveSystem.Application.Common.Models.AdvancedSearch;
using ArchiveSystem.Application.AccessControl;
using ArchiveSystem.Application.Interfaces;
using ArchiveSystem.Domain.Services;
using ArchiveSystem.Presentation.Api.Controllers;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ArchiveSystem.Infrastructure.Tests.AccessControl
{
    public class AdvancedFilesControllerAdvancedSearchTests
    {
        [Fact]
        public async Task SearchFiles_WithInvalidQuery_ReturnsBadRequest()
        {
            var validator = new Mock<IValidator<AdvancedSearchQueryDto>>();
            validator
                .Setup(x => x.ValidateAsync(It.IsAny<AdvancedSearchQueryDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(new[]
                {
                    new ValidationFailure(nameof(AdvancedSearchQueryDto.PageSize), "Page size must be <= 100."),
                }));

            var controller = CreateController(
                advancedSearchService: Mock.Of<IAdvancedFileSearchService>(),
                validator: validator.Object);

            var result = await controller.SearchFiles(
                new AdvancedSearchQueryDto { PageNumber = 1, PageSize = 1000 },
                CancellationToken.None);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task SearchFiles_WithValidQuery_ReturnsOkPayload()
        {
            var expected = new PagedSearchResultDto<SearchResultItemDto>
            {
                Items = new List<SearchResultItemDto>
                {
                    new()
                    {
                        Id = Guid.NewGuid().ToString(),
                        DossierNumber = "D-1200",
                        FileName = "scan-1.pdf",
                        Highlights = new List<string> { "sample snippet" },
                    },
                },
                TotalHits = 1,
                PageNumber = 1,
                PageSize = 20,
                Aggregations = new AdvancedSearchAggregationsDto
                {
                    Tags = new Dictionary<string, long> { ["sample"] = 1 },
                    Dossiers = new Dictionary<string, long> { ["D-1200"] = 1 },
                },
            };

            var validator = new Mock<IValidator<AdvancedSearchQueryDto>>();
            validator
                .Setup(x => x.ValidateAsync(It.IsAny<AdvancedSearchQueryDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            var advancedSearchService = new Mock<IAdvancedFileSearchService>();
            advancedSearchService
                .Setup(x => x.AdvancedSearchAsync(It.IsAny<AdvancedSearchQueryDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected);

            var controller = CreateController(
                advancedSearchService: advancedSearchService.Object,
                validator: validator.Object);

            var result = await controller.SearchFiles(new AdvancedSearchQueryDto(), CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result);
            var payload = Assert.IsType<PagedSearchResultDto<SearchResultItemDto>>(ok.Value);
            Assert.Equal(1, payload.TotalHits);
            Assert.Single(payload.Items);
            Assert.Equal("scan-1.pdf", payload.Items[0].FileName);
            Assert.Equal(1, payload.Aggregations.Tags["sample"]);
        }

        private static AdvancedFilesController CreateController(
            IAdvancedFileSearchService advancedSearchService,
            IValidator<AdvancedSearchQueryDto> validator)
        {
            return new AdvancedFilesController(
                mediator: Mock.Of<IMediator>(),
                storageService: Mock.Of<IStorageService>(),
                accessControlService: Mock.Of<IAccessControlService>(),
                advancedFileSearchService: advancedSearchService,
                advancedSearchValidator: validator,
                logger: NullLogger<AdvancedFilesController>.Instance);
        }
    }
}
