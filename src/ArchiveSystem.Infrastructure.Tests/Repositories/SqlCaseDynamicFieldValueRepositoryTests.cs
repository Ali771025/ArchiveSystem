using ArchiveSystem.Application.Common.Models.DynamicFields;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Domain.Enums;
using ArchiveSystem.Domain.ValueObjects;
using ArchiveSystem.Infrastructure.Persistence;
using ArchiveSystem.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Xunit;

namespace ArchiveSystem.Infrastructure.Tests.Repositories
{
    public sealed class SqlCaseDynamicFieldValueRepositoryTests
    {
        [Fact]
        public async Task ReplaceCaseValuesAsync_PersistsStringValueAcrossDbContexts()
        {
            await using var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<ArchiveDbContext>()
                .UseSqlite(connection)
                .Options;

            var fieldId = Guid.NewGuid();
            await using (var writeContext = new ArchiveDbContext(options))
            {
                await writeContext.Database.EnsureCreatedAsync();
                await SeedCaseAndStringFieldAsync(writeContext, fieldId);

                var repository = new SqlCaseDynamicFieldValueRepository(writeContext);
                await repository.ReplaceCaseValuesAsync(
                    caseId: 100,
                    values: new[]
                    {
                        new CaseDynamicFieldValueDto
                        {
                            FieldId = fieldId,
                            StringValue = "1405-12345"
                        }
                    });
            }

            await using var readContext = new ArchiveDbContext(options);
            var reloadedRepository = new SqlCaseDynamicFieldValueRepository(readContext);
            var values = await reloadedRepository.GetCaseValuesByCaseIdAsync(100);

            Assert.True(values.TryGetValue(fieldId, out var storedValue));
            Assert.Equal("1405-12345", storedValue!.StringValue);
        }

        [Fact]
        public async Task ReplaceCaseValuesAsync_UpdatesExistingStringValue()
        {
            await using var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<ArchiveDbContext>()
                .UseSqlite(connection)
                .Options;

            var fieldId = Guid.NewGuid();
            await using (var context = new ArchiveDbContext(options))
            {
                await context.Database.EnsureCreatedAsync();
                await SeedCaseAndStringFieldAsync(context, fieldId);

                var repository = new SqlCaseDynamicFieldValueRepository(context);
                await repository.ReplaceCaseValuesAsync(100, new[]
                {
                    new CaseDynamicFieldValueDto { FieldId = fieldId, StringValue = "old-value" }
                });

                await repository.ReplaceCaseValuesAsync(100, new[]
                {
                    new CaseDynamicFieldValueDto { FieldId = fieldId, StringValue = "new-value" }
                });
            }

            await using var readContext = new ArchiveDbContext(options);
            var reloadedRepository = new SqlCaseDynamicFieldValueRepository(readContext);
            var values = await reloadedRepository.GetCaseValuesByCaseIdAsync(100);

            Assert.Equal("new-value", values[fieldId].StringValue);
        }

        [Fact]
        public async Task SearchCasesAsync_AppliesFiltersSortingAndPaginationToUniqueClassificationScope()
        {
            await using var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<ArchiveDbContext>()
                .UseSqlite(connection)
                .Options;

            var root = new Classification(10, "شهرسازی");
            root.RemoveFromParent();
            var child = new Classification(20, "پرونده‌های شهرسازی");
            child.SetParent(root);
            var grandchild = new Classification(30, "ناحیه یک");
            grandchild.SetParent(child);
            var unrelated = new Classification(40, "درآمد");
            unrelated.RemoveFromParent();

            await using var context = new ArchiveDbContext(options);
            await context.Database.EnsureCreatedAsync();
            context.Classifications.AddRange(root, child, grandchild, unrelated);
            context.Cases.AddRange(
                new Case(100, "CASE-ROOT", "Root case", root.Id, externalCaseNumber: "URB-ROOT"),
                new Case(200, "CASE-CHILD", "Child case", child.Id, externalCaseNumber: "URB-CHILD"),
                new Case(300, "CASE-LEAF", "Leaf case", grandchild.Id, externalCaseNumber: "URB-LEAF"),
                new Case(400, "CASE-OTHER", "Other case", unrelated.Id, externalCaseNumber: "URB-OTHER"));
            var activeFile = new ArchiveFile(
                501,
                "active.pdf",
                FileSize.Create(100),
                FileExtension.Create(".pdf"),
                "application/pdf",
                "cases/100/active.pdf",
                caseId: 100);
            var deletedFile = new ArchiveFile(
                502,
                "deleted.pdf",
                FileSize.Create(100),
                FileExtension.Create(".pdf"),
                "application/pdf",
                "cases/100/deleted.pdf",
                caseId: 100);
            deletedFile.SoftDelete();
            context.Files.AddRange(activeFile, deletedFile);
            await context.SaveChangesAsync();

            var repository = new SqlCaseDynamicFieldValueRepository(context);
            var classificationScope = new[] { root.Id, child.Id, child.Id, grandchild.Id };

            var firstPage = await repository.SearchCasesAsync(
                classificationScope,
                bypassAuthorizationFilters: true,
                allowedClassificationIds: Array.Empty<long>(),
                excludedClassificationId: null,
                dynamicFilterLogic: "and",
                dynamicFilterGroups: Array.Empty<DynamicFilterGroupDto>(),
                fieldDefinitions: new Dictionary<Guid, DynamicFieldDefinitionDto>(),
                ownerName: null,
                externalCaseNumber: "URB",
                createdBy: null,
                fromDate: null,
                toDate: null,
                tags: Array.Empty<long>(),
                page: 1,
                pageSize: 2,
                sortBy: "classificationTitle",
                sortDescending: false);

            var secondPage = await repository.SearchCasesAsync(
                classificationScope,
                bypassAuthorizationFilters: true,
                allowedClassificationIds: Array.Empty<long>(),
                excludedClassificationId: null,
                dynamicFilterLogic: "and",
                dynamicFilterGroups: Array.Empty<DynamicFilterGroupDto>(),
                fieldDefinitions: new Dictionary<Guid, DynamicFieldDefinitionDto>(),
                ownerName: null,
                externalCaseNumber: "URB",
                createdBy: null,
                fromDate: null,
                toDate: null,
                tags: Array.Empty<long>(),
                page: 2,
                pageSize: 2,
                sortBy: "classificationTitle",
                sortDescending: false);

            Assert.Equal(3, firstPage.TotalCount);
            Assert.Equal(2, firstPage.Items.Count);
            Assert.Single(secondPage.Items);
            Assert.Equal(
                new[] { 100L, 200L, 300L },
                firstPage.Items
                    .Concat(secondPage.Items)
                    .Select(item => item.Id)
                    .OrderBy(id => id));
            Assert.Equal(
                1,
                firstPage.Items
                    .Concat(secondPage.Items)
                    .Single(item => item.Id == 100)
                    .FileCount);
        }

        [Fact]
        public async Task SearchCasesAsync_UsesOrBetweenConditionsInsideAGroup()
        {
            await using var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<ArchiveDbContext>()
                .UseSqlite(connection)
                .Options;

            var firstFieldId = Guid.NewGuid();
            var secondFieldId = Guid.NewGuid();
            const long classificationId = 10;

            await using var context = new ArchiveDbContext(options);
            await context.Database.EnsureCreatedAsync();
            context.Classifications.Add(new Classification(classificationId, "شهرسازی"));
            context.Cases.AddRange(
                new Case(100, "CASE-100", "First condition", classificationId),
                new Case(200, "CASE-200", "Second condition", classificationId),
                new Case(300, "CASE-300", "No condition", classificationId));
            context.CaseFieldValueStrings.AddRange(
                new CaseFieldValueString(100, firstFieldId, "RENOVATION-123"),
                new CaseFieldValueString(200, secondFieldId, "ZONE-5"));
            await context.SaveChangesAsync();

            var repository = new SqlCaseDynamicFieldValueRepository(context);
            var definitions = new Dictionary<Guid, DynamicFieldDefinitionDto>
            {
                [firstFieldId] = new()
                {
                    Id = firstFieldId,
                    FieldType = DynamicFieldType.String,
                    ShowAsFilter = true
                },
                [secondFieldId] = new()
                {
                    Id = secondFieldId,
                    FieldType = DynamicFieldType.String,
                    ShowAsFilter = true
                }
            };
            var groups = new[]
            {
                new DynamicFilterGroupDto
                {
                    Logic = "or",
                    Filters = new List<DynamicFilterDto>
                    {
                        new()
                        {
                            FieldId = firstFieldId,
                            Op = "contains",
                            Value = JsonSerializer.SerializeToElement("RENOVATION")
                        },
                        new()
                        {
                            FieldId = secondFieldId,
                            Op = "equals",
                            Value = JsonSerializer.SerializeToElement("ZONE-5")
                        }
                    }
                }
            };

            var result = await repository.SearchCasesAsync(
                new[] { classificationId },
                bypassAuthorizationFilters: true,
                allowedClassificationIds: Array.Empty<long>(),
                excludedClassificationId: null,
                dynamicFilterLogic: "and",
                dynamicFilterGroups: groups,
                fieldDefinitions: definitions,
                ownerName: null,
                externalCaseNumber: null,
                createdBy: null,
                fromDate: null,
                toDate: null,
                tags: Array.Empty<long>(),
                page: 1,
                pageSize: 20,
                sortBy: "caseNumber",
                sortDescending: false);

            Assert.Equal(2, result.TotalCount);
            Assert.Equal(new[] { 100L, 200L }, result.Items.Select(item => item.Id));
        }

        private static async Task SeedCaseAndStringFieldAsync(ArchiveDbContext context, Guid fieldId)
        {
            const long classificationId = 10;
            var templateId = Guid.NewGuid();
            var groupId = Guid.NewGuid();

            context.Classifications.Add(new Classification(classificationId, "Root"));
            context.Cases.Add(new Case(100, "CASE-100", "Case 100", classificationId));
            context.DynamicTemplates.Add(new DynamicTemplate(templateId, classificationId, "Case template"));
            context.DynamicFieldGroups.Add(new DynamicFieldGroup(groupId, templateId, "Main", 0));
            context.DynamicFieldDefinitions.Add(new DynamicFieldDefinition(
                fieldId,
                groupId,
                "renovation_code",
                "Renovation code",
                DynamicFieldType.String,
                isRequired: false,
                minNumber: null,
                maxNumber: null,
                minLength: null,
                maxLength: null,
                regex: null,
                isMultiSelect: false,
                showAsFilter: true,
                DynamicFieldFilterType.Contains,
                order: 0,
                isActive: true));

            await context.SaveChangesAsync();
        }
    }
}
