using ArchiveSystem.Application.Common.Exceptions;
using ArchiveSystem.Application.Features.Dossiers.Models;
using ArchiveSystem.Application.Features.Dossiers.Ownership;
using ArchiveSystem.Domain.Enums;
using ArchiveSystem.Presentation.Api.Controllers;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace ArchiveSystem.Infrastructure.Tests.AccessControl
{
    public class DossiersControllerIntegrationTests
    {
        private const long DossierId = 1059;
        private const long OwnerId = 801;

        [Fact]
        public async Task GetOwners_DelegatesToOwnershipQuery_ReturnsOwners()
        {
            var sender = new Mock<ISender>();
            sender
                .Setup(x => x.Send(
                    It.Is<GetDossierOwnersQuery>(query => query.DossierId == DossierId && query.CaseNumber == null),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<DossierOwnerItemResponse>
                {
                    new()
                    {
                        OwnerId = OwnerId,
                        OwnerType = OwnerType.Natural,
                        NameOrTitle = "Sara Ahmadi",
                        NationalIdentifier = "1234567890",
                        Mobile = "09120000000"
                    }
                });

            var controller = new DossiersController(sender.Object);

            var result = await controller.GetOwners(DossierId, CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result);
            var payload = Assert.IsAssignableFrom<IReadOnlyCollection<DossierOwnerItemResponse>>(ok.Value);
            var owner = Assert.Single(payload);
            Assert.Equal(OwnerId, owner.OwnerId);
            Assert.Equal("Sara Ahmadi", owner.NameOrTitle);
        }

        [Fact]
        public async Task GetOwners_WhenHandlerCannotResolveDossier_PropagatesNotFound()
        {
            var sender = new Mock<ISender>();
            sender
                .Setup(x => x.Send(
                    It.Is<GetDossierOwnersQuery>(query => query.DossierId == 999999 && query.CaseNumber == null),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new NotFoundException("Case/Dossier was not found."));

            var controller = new DossiersController(sender.Object);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                controller.GetOwners(dossierId: 999999, CancellationToken.None));
        }
    }
}
