using System.Security.Claims;
using ArchiveSystem.Application.AccessControl;
using ArchiveSystem.Application.Common.Exceptions;
using ArchiveSystem.Application.Features.Cases.Commands.CreateCase;
using ArchiveSystem.Application.Features.Cases.Commands.DeleteCase;
using ArchiveSystem.Application.Features.Cases.Commands.MoveCase;
using ArchiveSystem.Application.Features.Cases.Interfaces;
using ArchiveSystem.Application.Features.Classifications.Validation;
using ArchiveSystem.Application.Features.Reports.Interfaces;
using ArchiveSystem.Application.Features.Reports.Models;
using ArchiveSystem.Application.Interfaces.Persistence;
using ArchiveSystem.Domain;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Domain.Repositories;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace ArchiveSystem.Application.Tests.Cases
{
    public class CaseCommandHandlerAccessTests
    {
        [Fact]
        public async Task CreateCase_WithNonLeafClassification_ThrowsValidationException()
        {
            var caseRepository = new Mock<ICaseRepository>(MockBehavior.Strict);
            var classificationRepository = new Mock<IClassificationRepository>(MockBehavior.Strict);
            var accessControlService = new Mock<IAccessControlService>(MockBehavior.Strict);
            var auditWriter = new Mock<IAuditWriter>(MockBehavior.Strict);
            var httpContextAccessor = BuildHttpContextAccessor(101);
            var caseNumberGenerator = new Mock<ICaseNumberGenerator>(MockBehavior.Strict);

            classificationRepository
                .Setup(x => x.GetByIdAsync(10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Classification(10, "Root"));
            accessControlService
                .Setup(x => x.HasGlobalPermissionAsync(101, "admin.access", It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            accessControlService
                .Setup(x => x.CanWriteClassificationAsync(101, 10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            classificationRepository
                .Setup(x => x.HasChildrenAsync(10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var handler = new CreateCaseCommandHandler(
                caseRepository.Object,
                classificationRepository.Object,
                accessControlService.Object,
                auditWriter.Object,
                httpContextAccessor,
                caseNumberGenerator.Object);

            var command = new CreateCaseCommand
            {
                ClassificationId = 10,
                ExternalCaseNumber = "EXT-100",
                Address = "Sample Address",
                Owners = new List<CreateCaseOwnerDto>()
            };

            await Assert.ThrowsAsync<ArchiveSystem.Application.Common.Exceptions.ValidationException>(
                () => handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task MoveCase_WithNonLeafTargetClassification_ThrowsValidationException()
        {
            var caseRepository = new Mock<ICaseRepository>(MockBehavior.Strict);
            var classificationRepository = new Mock<IClassificationRepository>(MockBehavior.Strict);
            var accessControlService = new Mock<IAccessControlService>(MockBehavior.Strict);
            var auditWriter = new Mock<IAuditWriter>(MockBehavior.Strict);
            var leafValidator = new Mock<IValidator<AssignDossierToClassificationRule>>(MockBehavior.Strict);
            var httpContextAccessor = BuildHttpContextAccessor(101);

            caseRepository
                .Setup(x => x.GetByIdAsync(55, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Domain.Entities.Case(55, "CASE-55", "Case 55", classificationId: 10));
            accessControlService
                .Setup(x => x.HasGlobalPermissionAsync(101, "admin.access", It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            accessControlService
                .Setup(x => x.CanReadClassificationAsync(101, 10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            accessControlService
                .Setup(x => x.CanMoveClassificationAsync(101, 10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            classificationRepository
                .Setup(x => x.GetByIdAsync(20, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Classification(20, "Target"));
            leafValidator
                .Setup(x => x.ValidateAsync(
                    It.Is<AssignDossierToClassificationRule>(rule => rule.ClassificationId == 20),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(new[]
                {
                    new ValidationFailure(nameof(AssignDossierToClassificationRule.ClassificationId), "Documents can only be attached to final sub-categories.")
                }));

            var handler = new MoveCaseCommandHandler(
                caseRepository.Object,
                classificationRepository.Object,
                accessControlService.Object,
                auditWriter.Object,
                leafValidator.Object,
                httpContextAccessor);

            var command = new MoveCaseCommand
            {
                CaseId = 55,
                ClassificationId = 20
            };

            await Assert.ThrowsAsync<ArchiveSystem.Application.Common.Exceptions.ValidationException>(
                () => handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task MoveCase_WithoutClassificationAcl_MovesCase()
        {
            var caseRepository = new Mock<ICaseRepository>(MockBehavior.Strict);
            var classificationRepository = new Mock<IClassificationRepository>(MockBehavior.Strict);
            var accessControlService = new Mock<IAccessControlService>(MockBehavior.Strict);
            var auditWriter = new Mock<IAuditWriter>(MockBehavior.Strict);
            var leafValidator = new Mock<IValidator<AssignDossierToClassificationRule>>(MockBehavior.Strict);
            var httpContextAccessor = BuildHttpContextAccessor(101);
            var entity = new Domain.Entities.Case(55, "CASE-55", "Case 55", classificationId: 10);

            caseRepository
                .Setup(x => x.GetByIdAsync(55, It.IsAny<CancellationToken>()))
                .ReturnsAsync(entity);
            classificationRepository
                .Setup(x => x.GetByIdAsync(20, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Classification(20, "Target"));
            classificationRepository
                .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[]
                {
                    new Classification(10, "Source"),
                    new Classification(20, "Target")
                });
            accessControlService
                .Setup(x => x.HasGlobalPermissionAsync(101, "admin.access", It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            accessControlService
                .Setup(x => x.CanMoveClassificationAsync(101, 10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            accessControlService
                .Setup(x => x.CanMoveClassificationAsync(101, 20, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            leafValidator
                .Setup(x => x.ValidateAsync(
                    It.Is<AssignDossierToClassificationRule>(rule => rule.ClassificationId == 20),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());
            caseRepository
                .Setup(x => x.MoveToClassificationAsync(55, 20, It.IsAny<CancellationToken>()))
                .Callback(() => entity.MoveToClassification(20))
                .ReturnsAsync(true);
            auditWriter
                .Setup(x => x.WriteAsync(
                    It.Is<MovementLogWriteModel>(model =>
                        model.BeforeJson != null &&
                        model.BeforeJson.Contains("\"classificationPath\":\"Source\"") &&
                        model.AfterJson != null &&
                        model.AfterJson.Contains("\"classificationPath\":\"Target\"")),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var handler = new MoveCaseCommandHandler(
                caseRepository.Object,
                classificationRepository.Object,
                accessControlService.Object,
                auditWriter.Object,
                leafValidator.Object,
                httpContextAccessor);

            var command = new MoveCaseCommand
            {
                CaseId = 55,
                ClassificationId = 20
            };

            await handler.Handle(command, CancellationToken.None);

            Assert.Equal(20, entity.ClassificationId);
        }

        [Fact]
        public async Task DeleteCase_WithWriteAccess_ArchivesCaseAndWritesAudit()
        {
            var caseRepository = new Mock<ICaseRepository>(MockBehavior.Strict);
            var classificationRepository = new Mock<IClassificationRepository>(MockBehavior.Strict);
            var accessControlService = new Mock<IAccessControlService>(MockBehavior.Strict);
            var auditWriter = new Mock<IAuditWriter>(MockBehavior.Strict);
            var httpContextAccessor = BuildHttpContextAccessor(101);
            var entity = new Domain.Entities.Case(55, "CASE-55", "Case 55", classificationId: 10);
            var archiveClassification = new Classification(99, "بایگانی راکد");
            archiveClassification.MarkAsSystem(SystemClassificationKeys.ArchivedCases);

            caseRepository
                .Setup(x => x.GetByIdAsync(55, It.IsAny<CancellationToken>()))
                .ReturnsAsync(entity);
            classificationRepository
                .Setup(x => x.GetBySystemKeyAsync(SystemClassificationKeys.ArchivedCases, It.IsAny<CancellationToken>()))
                .ReturnsAsync(archiveClassification);
            accessControlService
                .Setup(x => x.HasGlobalPermissionAsync(101, "admin.access", It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            accessControlService
                .Setup(x => x.CanWriteClassificationAsync(101, 10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            caseRepository
                .Setup(x => x.MoveToClassificationAsync(55, 99, It.IsAny<CancellationToken>()))
                .Callback(() => entity.MoveToClassification(99))
                .ReturnsAsync(true);
            auditWriter
                .Setup(x => x.WriteAsync(
                    It.Is<MovementLogWriteModel>(model =>
                        model.ActionType == "CASE_ARCHIVED" &&
                        model.EntityType == "Case" &&
                        model.CaseId == 55 &&
                        model.ClassificationId == 99),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var handler = new DeleteCaseCommandHandler(
                caseRepository.Object,
                classificationRepository.Object,
                accessControlService.Object,
                auditWriter.Object,
                httpContextAccessor);

            var result = await handler.Handle(new DeleteCaseCommand { CaseId = 55 }, CancellationToken.None);

            Assert.False(entity.IsDeleted);
            Assert.Null(entity.DeletedAt);
            Assert.Equal(99, entity.ClassificationId);
            Assert.Equal(new DeleteCaseResult(55, 10, 99), result);
            caseRepository.Verify(x => x.MoveToClassificationAsync(55, 99, It.IsAny<CancellationToken>()), Times.Once);
            auditWriter.VerifyAll();
        }

        [Fact]
        public async Task DeleteCase_WithReadOnlyAccess_ThrowsClearForbiddenError()
        {
            var caseRepository = new Mock<ICaseRepository>(MockBehavior.Strict);
            var classificationRepository = new Mock<IClassificationRepository>(MockBehavior.Strict);
            var accessControlService = new Mock<IAccessControlService>(MockBehavior.Strict);
            var auditWriter = new Mock<IAuditWriter>(MockBehavior.Strict);
            var httpContextAccessor = BuildHttpContextAccessor(101);

            caseRepository
                .Setup(x => x.GetByIdAsync(55, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Domain.Entities.Case(55, "CASE-55", "Case 55", classificationId: 10));
            accessControlService
                .Setup(x => x.HasGlobalPermissionAsync(101, "admin.access", It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            accessControlService
                .Setup(x => x.CanWriteClassificationAsync(101, 10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var handler = new DeleteCaseCommandHandler(
                caseRepository.Object,
                classificationRepository.Object,
                accessControlService.Object,
                auditWriter.Object,
                httpContextAccessor);

            var exception = await Assert.ThrowsAsync<ForbiddenAccessException>(
                () => handler.Handle(new DeleteCaseCommand { CaseId = 55 }, CancellationToken.None));

            Assert.Equal(
                "شما فقط مجوز مشاهده این طبقه‌بندی را دارید و امکان بایگانی پرونده برای شما فعال نیست.",
                exception.Message);
        }

        private static IHttpContextAccessor BuildHttpContextAccessor(long userId)
        {
            var context = new DefaultHttpContext();
            context.User = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[]
                    {
                        new Claim("sub", userId.ToString()),
                        new Claim(ClaimTypes.NameIdentifier, userId.ToString())
                    },
                    "TestAuth"));

            return new HttpContextAccessor
            {
                HttpContext = context
            };
        }
    }
}
