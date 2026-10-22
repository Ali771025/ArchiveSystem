using ArchiveSystem.Application.Common.Models.DynamicFields;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Domain.Enums;
using ArchiveSystem.Infrastructure.Persistence;
using ArchiveSystem.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ArchiveSystem.Infrastructure.Tests.Repositories
{
    public sealed class SqlDynamicTemplateRepositoryTests
    {
        [Fact]
        public async Task GetActiveHierarchyByClassificationIdAsync_ReturnsTemplatesFromRootToSelectedClassification()
        {
            await using var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<ArchiveDbContext>()
                .UseSqlite(connection)
                .Options;

            var root = new Classification(101, "Category 1");
            root.RemoveFromParent();
            var child = new Classification(102, "Category 1-1");
            child.SetParent(root);
            var leaf = new Classification(103, "Category 1-1-1");
            leaf.SetParent(child);

            var rootTemplateId = Guid.NewGuid();
            var childTemplateId = Guid.NewGuid();
            var leafTemplateId = Guid.NewGuid();
            var rootGroupId = Guid.NewGuid();
            var childGroupId = Guid.NewGuid();
            var leafGroupId = Guid.NewGuid();

            await using (var seedContext = new ArchiveDbContext(options))
            {
                await seedContext.Database.EnsureCreatedAsync();
                seedContext.Classifications.AddRange(root, child, leaf);
                seedContext.DynamicTemplates.AddRange(
                    new DynamicTemplate(rootTemplateId, root.Id, "Root template"),
                    new DynamicTemplate(childTemplateId, child.Id, "Child template"),
                    new DynamicTemplate(leafTemplateId, leaf.Id, "Leaf template"));
                seedContext.DynamicFieldGroups.AddRange(
                    new DynamicFieldGroup(rootGroupId, rootTemplateId, "Root fields", 0),
                    new DynamicFieldGroup(childGroupId, childTemplateId, "Child fields", 0),
                    new DynamicFieldGroup(leafGroupId, leafTemplateId, "Leaf fields", 0));
                seedContext.DynamicFieldDefinitions.AddRange(
                    CreateField(Guid.NewGuid(), rootGroupId, "permit_number", "Permit number", DynamicFieldType.String, 0),
                    CreateField(Guid.NewGuid(), childGroupId, "permit_date", "Permit date", DynamicFieldType.Date, 0),
                    CreateField(Guid.NewGuid(), leafGroupId, "registration_plate", "Registration plate", DynamicFieldType.String, 0));
                await seedContext.SaveChangesAsync();
            }

            await using var queryContext = new ArchiveDbContext(options);
            var repository = new SqlDynamicTemplateRepository(queryContext);

            var effectiveTemplates = await repository.GetActiveHierarchyByClassificationIdAsync(leaf.Id);
            var directTemplate = await repository.GetByClassificationIdAsync(leaf.Id);

            Assert.Equal(
                new[] { root.Id, child.Id, leaf.Id },
                effectiveTemplates.Select(template => template.ClassificationId));
            Assert.Equal(
                new[] { "permit_number", "permit_date", "registration_plate" },
                effectiveTemplates
                    .SelectMany(template => template.Groups)
                    .SelectMany(group => group.Fields)
                    .Select(field => field.Key));
            Assert.Equal(new[] { "registration_plate" }, directTemplate!
                .Groups
                .SelectMany(group => group.Fields)
                .Select(field => field.Key));
        }

        [Fact]
        public async Task UpsertForClassificationAsync_AddsThirdFieldWithoutDeletingReferencedOptions()
        {
            await using var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<ArchiveDbContext>()
                .UseSqlite(connection)
                .Options;

            const long classificationId = 57;
            const long caseId = 100;
            var templateId = Guid.NewGuid();
            var groupId = Guid.NewGuid();
            var selectFieldId = Guid.NewGuid();
            var secondFieldId = Guid.NewGuid();
            var thirdFieldId = Guid.NewGuid();
            var optionId = Guid.NewGuid();

            await using (var seedContext = new ArchiveDbContext(options))
            {
                await seedContext.Database.EnsureCreatedAsync();
                seedContext.Classifications.Add(new Classification(classificationId, "Root"));
                seedContext.Cases.Add(new Case(caseId, "CASE-100", "Case 100", classificationId));
                seedContext.DynamicTemplates.Add(new DynamicTemplate(templateId, classificationId, "Template"));
                seedContext.DynamicFieldGroups.Add(new DynamicFieldGroup(groupId, templateId, "Main", 0));
                seedContext.DynamicFieldDefinitions.Add(CreateField(
                    selectFieldId,
                    groupId,
                    "status",
                    "Status",
                    DynamicFieldType.Enum,
                    order: 0));
                seedContext.DynamicFieldDefinitions.Add(CreateField(
                    secondFieldId,
                    groupId,
                    "description",
                    "Description",
                    DynamicFieldType.String,
                    order: 1));
                seedContext.DynamicFieldOptions.Add(new DynamicFieldOption(
                    optionId,
                    selectFieldId,
                    "active",
                    "Active",
                    order: 0,
                    isActive: true));
                seedContext.CaseFieldValueOptions.Add(new CaseFieldValueOption(caseId, selectFieldId, optionId));
                await seedContext.SaveChangesAsync();
            }

            await using (var updateContext = new ArchiveDbContext(options))
            {
                var repository = new SqlDynamicTemplateRepository(updateContext);
                var result = await repository.UpsertForClassificationAsync(
                    classificationId,
                    new DynamicTemplateDto
                    {
                        Id = templateId,
                        ClassificationId = classificationId,
                        Name = "Template",
                        Groups =
                        {
                            new DynamicFieldGroupDto
                            {
                                Id = groupId,
                                Title = "Main",
                                Order = 0,
                                Fields =
                                {
                                    CreateFieldDto(selectFieldId, "status", "Status", DynamicFieldType.Enum, 0, optionId),
                                    CreateFieldDto(
                                        secondFieldId,
                                        "description",
                                        "Description",
                                        DynamicFieldType.String,
                                        1,
                                        errorMessage: "لطفاً فقط عدد وارد کنید"),
                                    CreateFieldDto(thirdFieldId, "tracking_code", "Tracking code", DynamicFieldType.String, 2)
                                }
                            }
                        }
                    });

                Assert.Equal(3, result.Groups.Single().Fields.Count);
                Assert.Equal(
                    "لطفاً فقط عدد وارد کنید",
                    result.Groups.Single().Fields.Single(field => field.Id == secondFieldId).ErrorMessage);
            }

            await using var assertContext = new ArchiveDbContext(options);
            Assert.True(await assertContext.CaseFieldValueOptions.AnyAsync(value =>
                value.CaseId == caseId && value.FieldId == selectFieldId && value.OptionId == optionId));
            Assert.True(await assertContext.DynamicFieldOptions.AnyAsync(option => option.Id == optionId));
            Assert.True(await assertContext.DynamicFieldDefinitions.AnyAsync(field => field.Id == thirdFieldId));
            Assert.Equal(
                "لطفاً فقط عدد وارد کنید",
                await assertContext.DynamicFieldDefinitions
                    .Where(field => field.Id == secondFieldId)
                    .Select(field => field.ErrorMessage)
                    .SingleAsync());
        }

        private static DynamicFieldDefinition CreateField(
            Guid id,
            Guid groupId,
            string key,
            string label,
            DynamicFieldType fieldType,
            int order) =>
            new(
                id,
                groupId,
                key,
                label,
                fieldType,
                isRequired: false,
                minNumber: null,
                maxNumber: null,
                minLength: null,
                maxLength: null,
                regex: null,
                isMultiSelect: false,
                showAsFilter: false,
                filterType: fieldType == DynamicFieldType.String
                    ? DynamicFieldFilterType.Contains
                    : DynamicFieldFilterType.Equals,
                order,
                isActive: true);

        private static DynamicFieldDefinitionDto CreateFieldDto(
            Guid id,
            string key,
            string label,
            DynamicFieldType fieldType,
            int order,
            Guid? optionId = null,
            string? errorMessage = null) =>
            new()
            {
                Id = id,
                Key = key,
                Label = label,
                FieldType = fieldType,
                IsMultiSelect = false,
                FilterType = fieldType == DynamicFieldType.String
                    ? DynamicFieldFilterType.Contains
                    : DynamicFieldFilterType.Equals,
                Order = order,
                IsActive = true,
                ErrorMessage = errorMessage,
                Options = optionId.HasValue
                    ? new List<DynamicFieldOptionDto>
                    {
                        new()
                        {
                            Id = optionId.Value,
                            Value = "active",
                            Label = "Active",
                            Order = 0,
                            IsActive = true
                        }
                    }
                    : new List<DynamicFieldOptionDto>()
            };
    }
}
