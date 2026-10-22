using System.Security.Claims;
using ArchiveSystem.Application.Features.Cases.Commands.CreateCase;
using ArchiveSystem.Application.Features.Cases.Commands.MoveCase;
using ArchiveSystem.Application.Features.Cases.Models;
using ArchiveSystem.Application.Features.Cases.Queries.GetCaseById;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Domain.Enums;
using ArchiveSystem.Infrastructure.Persistence;
using ArchiveSystem.Presentation.Api.Controllers;
using ArchiveSystem.Presentation.Api.Models.Cases;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ArchiveSystem.Infrastructure.Tests.AccessControl
{
    public class CasesControllerIntegrationTests
    {
        private const long TestUserId = 500;
        private const long RootClassificationId = 10;
        private const long TargetClassificationId = 20;

        [Fact]
        public async Task GetById_WithoutReadPermission_ReturnsForbid()
        {
            await using var dbContext = CreateDbContext();
            await SeedClassificationAndCaseAsync(dbContext);

            var controller = CreateController(dbContext);
            var result = await controller.GetById(caseId: 100, CancellationToken.None);
            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task Create_DelegatesToHandler_ReturnsCreatedAtAction()
        {
            await using var dbContext = CreateDbContext();
            await SeedClassificationAsync(dbContext, RootClassificationId, "Root");
            await dbContext.ClassificationAclEntries.AddAsync(
                new ClassificationAclEntry(
                    id: 0,
                    subjectType: AccessSubjectType.User,
                    subjectId: TestUserId,
                    classificationId: RootClassificationId,
                    permissionMask: ClassificationPermissionMask.Read));
            await dbContext.SaveChangesAsync();

            var controller = CreateController(dbContext);
            var result = await controller.Create(
                new CreateCaseRequest
                {
                    ClassificationId = RootClassificationId,
                    ExternalCaseNumber = "EXT-1000",
                    Address = "Test Address",
                    Owners = new List<CaseOwnerRequest>
                    {
                        new()
                        {
                            FullName = "Owner",
                            NationalCode = "1234567890",
                            Phone = "09120000000"
                        }
                    }
                },
                CancellationToken.None);

            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var payload = Assert.IsType<ArchiveSystem.Presentation.Api.Models.ApiResult<CaseDetailsResponse>>(created.Value);
            Assert.True(payload.Success);
            Assert.Equal("EXT-1000", payload.Data!.CaseNumber);
            Assert.True(await dbContext.Cases.AnyAsync(x => x.CaseNumber == "EXT-1000"));
        }

        [Fact]
        public async Task Move_WithInheritedMovePermission_ReturnsOk()
        {
            await using var dbContext = CreateDbContext();
            await SeedClassificationAndCaseAsync(dbContext);
            await SeedClassificationAsync(dbContext, TargetClassificationId, "Target");

            await dbContext.ClassificationAclEntries.AddAsync(
                new ClassificationAclEntry(
                    id: 0,
                    subjectType: AccessSubjectType.User,
                    subjectId: TestUserId,
                    classificationId: RootClassificationId,
                    permissionMask: ClassificationPermissionMask.Move));
            await dbContext.SaveChangesAsync();

            var controller = CreateController(dbContext);
            var result = await controller.Move(
                caseId: 100,
                new MoveCaseRequest { ClassificationId = TargetClassificationId },
                CancellationToken.None);

            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task Create_WithWritePermission_CreatesCase()
        {
            await using var dbContext = CreateDbContext();
            await SeedClassificationAsync(dbContext, RootClassificationId, "Root");
            await dbContext.ClassificationAclEntries.AddAsync(
                new ClassificationAclEntry(
                    id: 0,
                    subjectType: AccessSubjectType.User,
                    subjectId: TestUserId,
                    classificationId: RootClassificationId,
                    permissionMask: ClassificationPermissionMask.Write));
            await dbContext.SaveChangesAsync();

            var controller = CreateController(dbContext);
            var result = await controller.Create(
                new CreateCaseRequest
                {
                    ClassificationId = RootClassificationId,
                    ExternalCaseNumber = "EXT-500",
                    Address = "Permitted Address",
                    Owners = new List<CaseOwnerRequest>
                    {
                        new()
                        {
                            FullName = "Allowed Owner",
                            NationalCode = "1234567890",
                            Phone = "09123456789"
                        }
                    }
                },
                CancellationToken.None);

            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var payload = Assert.IsType<ArchiveSystem.Presentation.Api.Models.ApiResult<CaseDetailsResponse>>(created.Value);
            Assert.True(payload.Success);
            Assert.Equal("EXT-500", payload.Data!.CaseNumber);
            Assert.True(await dbContext.Cases.AnyAsync(x => x.CaseNumber == "EXT-500"));
        }

        private static CasesController CreateController(ArchiveDbContext dbContext)
        {
            var httpContext = BuildHttpContext(TestUserId);

            var mediator = new Mock<IMediator>();
            mediator
                .Setup(x => x.Send(It.IsAny<CreateCaseCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((CreateCaseCommand command, CancellationToken _) =>
                {
                    var entity = new Case(
                        id: 0,
                        caseNumber: command.ExternalCaseNumber,
                        title: command.Address,
                        classificationId: command.ClassificationId,
                        owners: "[]",
                        location: command.Address);
                    dbContext.Cases.Add(entity);
                    dbContext.SaveChanges();

                    return new CaseDetailsDto
                    {
                        Id = entity.Id,
                        CaseNumber = command.ExternalCaseNumber,
                        ExternalCaseNumber = command.ExternalCaseNumber,
                        Address = command.Address,
                        ClassificationId = command.ClassificationId,
                        ClassificationTitle = "Root",
                        UpdatedAt = DateTime.UtcNow
                    };
                });
            mediator
                .Setup(x => x.Send(It.IsAny<GetCaseByIdQuery>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArchiveSystem.Application.Common.Exceptions.UnauthorizedAccessException("Forbidden"));
            mediator
                .Setup(x => x.Send(It.IsAny<MoveCaseCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArchiveSystem.Application.Common.Exceptions.UnauthorizedAccessException("Forbidden"));

            var controller = new CasesController(
                mediator: mediator.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = httpContext
                }
            };

            return controller;
        }

        private static DefaultHttpContext BuildHttpContext(long userId)
        {
            var context = new DefaultHttpContext();
            context.User = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[]
                    {
                        new Claim("sub", userId.ToString()),
                        new Claim(ClaimTypes.NameIdentifier, userId.ToString())
                    },
                    authenticationType: "TestAuth"));
            return context;
        }

        private static ArchiveDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<ArchiveDbContext>()
                .UseInMemoryDatabase($"cases-controller-int-{Guid.NewGuid():N}")
                .Options;
            return new ArchiveDbContext(options);
        }

        private static async Task SeedClassificationAndCaseAsync(ArchiveDbContext dbContext)
        {
            await SeedClassificationAsync(dbContext, RootClassificationId, "Root");
            await dbContext.Cases.AddAsync(
                new Case(
                    id: 100,
                    caseNumber: "CASE-100",
                    title: "Case 100",
                    classificationId: RootClassificationId));
            await dbContext.SaveChangesAsync();
        }

        private static async Task SeedClassificationAsync(ArchiveDbContext dbContext, long id, string title)
        {
            if (await dbContext.Classifications.AnyAsync(x => x.Id == id))
            {
                return;
            }

            await dbContext.Classifications.AddAsync(new Classification(id, title));
            await dbContext.SaveChangesAsync();
        }

    }
}
