using ArchiveSystem.Application.Common.Models.DynamicFields;
using ArchiveSystem.Application.Features.DynamicFields.Queries.SearchCases;
using ArchiveSystem.Application.Interfaces.Persistence;
using ArchiveSystem.Domain;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Domain.Repositories;
using Moq;
using Xunit;

namespace ArchiveSystem.Application.Tests.Cases
{
    public sealed class SearchCasesArchiveFilteringTests
    {
        [Fact]
        public async Task SearchWithoutClassification_ExcludesArchivedCasesSystemClassification()
        {
            var dynamicTemplateRepository = new Mock<IDynamicTemplateRepository>(MockBehavior.Strict);
            var caseDynamicFieldValueRepository = new Mock<ICaseDynamicFieldValueRepository>(MockBehavior.Strict);
            var classificationRepository = new Mock<IClassificationRepository>(MockBehavior.Strict);
            var archiveClassification = new Classification(99, "بایگانی راکد");
            archiveClassification.MarkAsSystem(SystemClassificationKeys.ArchivedCases);

            classificationRepository
                .Setup(x => x.GetBySystemKeyAsync(SystemClassificationKeys.ArchivedCases, It.IsAny<CancellationToken>()))
                .ReturnsAsync(archiveClassification);

            caseDynamicFieldValueRepository
                .Setup(x => x.SearchCasesAsync(
                    It.Is<IReadOnlyCollection<long>>(ids => ids.Count == 0),
                    true,
                    It.IsAny<IReadOnlyCollection<long>>(),
                    99,
                    "and",
                    It.IsAny<IReadOnlyCollection<DynamicFilterGroupDto>>(),
                    It.IsAny<IReadOnlyDictionary<Guid, DynamicFieldDefinitionDto>>(),
                    null,
                    null,
                    null,
                    null,
                    null,
                    It.IsAny<IReadOnlyCollection<long>>(),
                    1,
                    20,
                    "updatedAt",
                    true,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SearchCasesResultDto
                {
                    Items = new List<CaseSearchItemDto>(),
                    TotalCount = 0,
                    Page = 1,
                    PageSize = 20
                });

            var handler = new SearchCasesQueryHandler(
                dynamicTemplateRepository.Object,
                caseDynamicFieldValueRepository.Object,
                classificationRepository.Object);

            await handler.Handle(new SearchCasesQuery
            {
                BypassAuthorizationFilters = true
            }, CancellationToken.None);

            caseDynamicFieldValueRepository.VerifyAll();
        }

        [Fact]
        public async Task SearchWithClassification_IncludesSelectedClassificationAndAllDescendants()
        {
            var dynamicTemplateRepository = new Mock<IDynamicTemplateRepository>(MockBehavior.Strict);
            var caseDynamicFieldValueRepository = new Mock<ICaseDynamicFieldValueRepository>(MockBehavior.Strict);
            var classificationRepository = new Mock<IClassificationRepository>(MockBehavior.Strict);
            var root = new Classification(10, "شهرسازی");
            var child = new Classification(20, "پرونده‌های شهرسازی", root.Id);
            var grandchild = new Classification(30, "ناحیه یک", child.Id);
            var unrelatedRoot = new Classification(40, "درآمد");

            classificationRepository
                .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { root, child, grandchild, unrelatedRoot });

            caseDynamicFieldValueRepository
                .Setup(x => x.SearchCasesAsync(
                    It.Is<IReadOnlyCollection<long>>(ids =>
                        ids.Count == 3 &&
                        ids.Contains(root.Id) &&
                        ids.Contains(child.Id) &&
                        ids.Contains(grandchild.Id) &&
                        !ids.Contains(unrelatedRoot.Id)),
                    true,
                    It.IsAny<IReadOnlyCollection<long>>(),
                    null,
                    "and",
                    It.IsAny<IReadOnlyCollection<DynamicFilterGroupDto>>(),
                    It.IsAny<IReadOnlyDictionary<Guid, DynamicFieldDefinitionDto>>(),
                    null,
                    null,
                    null,
                    null,
                    null,
                    It.IsAny<IReadOnlyCollection<long>>(),
                    1,
                    20,
                    "updatedAt",
                    true,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SearchCasesResultDto
                {
                    Items = new List<CaseSearchItemDto>(),
                    TotalCount = 0,
                    Page = 1,
                    PageSize = 20
                });

            var handler = new SearchCasesQueryHandler(
                dynamicTemplateRepository.Object,
                caseDynamicFieldValueRepository.Object,
                classificationRepository.Object);

            await handler.Handle(new SearchCasesQuery
            {
                ClassificationId = root.Id,
                BypassAuthorizationFilters = true
            }, CancellationToken.None);

            caseDynamicFieldValueRepository.VerifyAll();
        }
    }
}
