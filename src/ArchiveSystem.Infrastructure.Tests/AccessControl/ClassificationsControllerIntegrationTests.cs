using ArchiveSystem.Application.Features.Classifications.Commands.CreateClassification;
using ArchiveSystem.Application.Features.Classifications.Commands.DeleteClassification;
using ArchiveSystem.Application.Features.Classifications.Commands.RenameClassification;
using ArchiveSystem.Application.Features.Classifications.Models;
using ArchiveSystem.Presentation.Api.Controllers;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace ArchiveSystem.Infrastructure.Tests.AccessControl
{
    public class ClassificationsControllerIntegrationTests
    {
        private const long ClassificationId = 25;

        [Fact]
        public async Task Create_DelegatesToCreateCommand_ReturnsCreatedResult()
        {
            var sender = new Mock<ISender>();
            sender
                .Setup(x => x.Send(
                    It.Is<CreateClassificationCommand>(command => command.Title == "Root Node" && command.ParentId == null),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ClassificationMutationResultDto
                {
                    Id = ClassificationId,
                    Title = "Root Node",
                    ParentId = null
                });

            var controller = new ClassificationsController(sender.Object);

            var result = await controller.Create(
                new CreateClassificationRequest
                {
                    Title = "Root Node",
                    ParentId = null
                },
                CancellationToken.None);

            var created = Assert.IsType<CreatedAtActionResult>(result);
            var payload = Assert.IsType<ClassificationMutationResultDto>(created.Value);
            Assert.Equal(ClassificationId, payload.Id);
        }

        [Fact]
        public async Task Update_DelegatesToRenameCommand_ReturnsOk()
        {
            var sender = new Mock<ISender>();
            sender
                .Setup(x => x.Send(
                    It.Is<RenameClassificationCommand>(command => command.Id == ClassificationId && command.Title == "Updated"),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ClassificationMutationResultDto
                {
                    Id = ClassificationId,
                    Title = "Updated"
                });

            var controller = new ClassificationsController(sender.Object);

            var result = await controller.Update(
                ClassificationId,
                new UpdateClassificationRequest
                {
                    Title = "Updated"
                },
                CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result);
            var payload = Assert.IsType<ClassificationMutationResultDto>(ok.Value);
            Assert.Equal("Updated", payload.Title);
        }

        [Fact]
        public async Task Delete_DelegatesToDeleteCommand_ReturnsNoContent()
        {
            var sender = new Mock<ISender>();
            sender
                .Setup(x => x.Send(
                    It.Is<DeleteClassificationCommand>(command => command.Id == ClassificationId),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Unit.Value);

            var controller = new ClassificationsController(sender.Object);

            var result = await controller.Delete(ClassificationId, CancellationToken.None);

            Assert.IsType<NoContentResult>(result);
        }
    }
}
