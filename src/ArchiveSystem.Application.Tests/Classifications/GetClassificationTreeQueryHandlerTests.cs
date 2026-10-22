using ArchiveSystem.Application.Features.Classifications.Queries.GetClassificationTree;
using ArchiveSystem.Application.Features.Classifications.Security;
using ArchiveSystem.Domain;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Domain.Enums;
using ArchiveSystem.Domain.Repositories;
using Moq;
using Xunit;

namespace ArchiveSystem.Application.Tests.Classifications
{
    public class GetClassificationTreeQueryHandlerTests
    {
        [Fact]
        public async Task Handle_IncludesArchivedCasesSystemNode_WhenUserHasNoClassificationAcl()
        {
            var activeClassification = new Classification(10, "پرونده‌های فعال");
            var archivedClassification = new Classification(99, "بایگانی راکد");
            archivedClassification.MarkAsSystem(SystemClassificationKeys.ArchivedCases);

            var classificationRepository = new Mock<IClassificationRepository>(MockBehavior.Strict);
            classificationRepository
                .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { activeClassification, archivedClassification });
            classificationRepository
                .Setup(x => x.GetDossierCountsByClassificationAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<long, int>
                {
                    [archivedClassification.Id] = 3
                });

            var authorizer = new Mock<IClassificationAccessAuthorizer>(MockBehavior.Strict);
            authorizer
                .Setup(x => x.GetAccessibleSetAsync(
                    It.IsAny<IReadOnlyCollection<Classification>>(),
                    ClassificationPermissionMask.Read,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HashSet<long>());

            var handler = new GetClassificationTreeQueryHandler(
                classificationRepository.Object,
                authorizer.Object);

            var result = await handler.Handle(new GetClassificationTreeQuery(), CancellationToken.None);

            var archivedNode = Assert.Single(result);
            Assert.Equal(archivedClassification.Id, archivedNode.Id);
            Assert.Equal("بایگانی راکد", archivedNode.Title);
            Assert.Equal(SystemClassificationKeys.ArchivedCases, archivedNode.SystemKey);
            Assert.Equal(3, archivedNode.CaseCount);
        }

        [Fact]
        public async Task Handle_IncludesAncestors_WhenOnlyChildClassificationIsReadable()
        {
            var root = new Classification(10, "شهرسازی");
            var child = new Classification(21, "۲۱", root.Id);

            var classificationRepository = new Mock<IClassificationRepository>(MockBehavior.Strict);
            classificationRepository
                .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { root, child });
            classificationRepository
                .Setup(x => x.GetDossierCountsByClassificationAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<long, int>
                {
                    [root.Id] = 5,
                    [child.Id] = 2
                });

            var authorizer = new Mock<IClassificationAccessAuthorizer>(MockBehavior.Strict);
            authorizer
                .Setup(x => x.GetAccessibleSetAsync(
                    It.IsAny<IReadOnlyCollection<Classification>>(),
                    ClassificationPermissionMask.Read,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HashSet<long> { child.Id });

            var handler = new GetClassificationTreeQueryHandler(
                classificationRepository.Object,
                authorizer.Object);

            var result = await handler.Handle(new GetClassificationTreeQuery(), CancellationToken.None);

            var visibleRoot = Assert.Single(result);
            Assert.Equal(root.Id, visibleRoot.Id);
            Assert.Equal(2, visibleRoot.CaseCount);
            var visibleChild = Assert.Single(visibleRoot.Children);
            Assert.Equal(child.Id, visibleChild.Id);
            Assert.Equal(2, visibleChild.CaseCount);
        }

        [Fact]
        public async Task Handle_AggregatesDirectAndNestedDossierCounts_ForEveryVisibleParent()
        {
            var root = new Classification(10, "ریشه");
            var firstChild = new Classification(20, "زیرپوشه اول", root.Id);
            var secondChild = new Classification(30, "زیرپوشه دوم", root.Id);

            var classificationRepository = new Mock<IClassificationRepository>(MockBehavior.Strict);
            classificationRepository
                .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { root, firstChild, secondChild });
            classificationRepository
                .Setup(x => x.GetDossierCountsByClassificationAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<long, int>
                {
                    [root.Id] = 1,
                    [firstChild.Id] = 1,
                    [secondChild.Id] = 1
                });

            var authorizer = new Mock<IClassificationAccessAuthorizer>(MockBehavior.Strict);
            authorizer
                .Setup(x => x.GetAccessibleSetAsync(
                    It.IsAny<IReadOnlyCollection<Classification>>(),
                    ClassificationPermissionMask.Read,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HashSet<long> { root.Id, firstChild.Id, secondChild.Id });

            var handler = new GetClassificationTreeQueryHandler(
                classificationRepository.Object,
                authorizer.Object);

            var result = await handler.Handle(new GetClassificationTreeQuery(), CancellationToken.None);

            var visibleRoot = Assert.Single(result);
            Assert.Equal(3, visibleRoot.CaseCount);
            Assert.Equal(2, visibleRoot.Children.Count);
            Assert.All(visibleRoot.Children, child => Assert.Equal(1, child.CaseCount));
        }
    }
}
