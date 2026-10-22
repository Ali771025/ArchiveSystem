using System.Text.Json;
using ArchiveSystem.Domain.Enums;

namespace ArchiveSystem.Application.Common.Models.DynamicFields
{
    public sealed class DynamicTemplateDto
    {
        public Guid Id { get; set; }
        public long ClassificationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public List<DynamicFieldGroupDto> Groups { get; set; } = new();
    }

    public sealed class DynamicFieldGroupDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Order { get; set; }
        public List<DynamicFieldDefinitionDto> Fields { get; set; } = new();
    }

    public sealed class DynamicFieldDefinitionDto
    {
        public Guid Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public DynamicFieldType FieldType { get; set; }
        public bool IsRequired { get; set; }
        public decimal? MinNumber { get; set; }
        public decimal? MaxNumber { get; set; }
        public int? MinLength { get; set; }
        public int? MaxLength { get; set; }
        public string? Regex { get; set; }
        public string? ErrorMessage { get; set; }
        public bool IsMultiSelect { get; set; }
        public bool ShowAsFilter { get; set; }
        public DynamicFieldFilterType FilterType { get; set; } = DynamicFieldFilterType.Contains;
        public int Order { get; set; }
        public bool IsActive { get; set; } = true;
        public List<DynamicFieldOptionDto> Options { get; set; } = new();
    }

    public sealed class DynamicFieldOptionDto
    {
        public Guid Id { get; set; }
        public string Value { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public int Order { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public sealed class CaseDynamicFieldsDto
    {
        public long CaseId { get; set; }
        public long ClassificationId { get; set; }
        public Guid TemplateId { get; set; }
        public List<CaseDynamicFieldValueDto> Values { get; set; } = new();
    }

    public sealed class CaseDynamicFieldValueDto
    {
        public Guid FieldId { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public DynamicFieldType FieldType { get; set; }
        public bool IsRequired { get; set; }
        public bool IsMultiSelect { get; set; }
        public string? StringValue { get; set; }
        public decimal? NumberValue { get; set; }
        public DateTime? DateValue { get; set; }
        public bool? BoolValue { get; set; }
        public List<Guid> OptionIds { get; set; } = new();
        public List<DynamicFieldOptionDto> Options { get; set; } = new();
    }

    public sealed class DynamicFilterDto
    {
        public Guid FieldId { get; set; }
        public string Op { get; set; } = string.Empty;
        public JsonElement? Value { get; set; }
        public JsonElement? FromValue { get; set; }
        public JsonElement? ToValue { get; set; }
        public List<JsonElement> Values { get; set; } = new();
    }

    public sealed class DynamicFilterGroupDto
    {
        public string Logic { get; set; } = "and";
        public List<DynamicFilterDto> Filters { get; set; } = new();
    }

    public sealed class SearchCasesResultDto
    {
        public List<CaseSearchItemDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public sealed class CaseSearchItemDto
    {
        public long Id { get; set; }
        public string CaseNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? AvatarImagePath { get; set; }
        public string Address { get; set; } = string.Empty;
        public string ExternalCaseNumber { get; set; } = string.Empty;
        public List<string> OwnerNames { get; set; } = new();
        public long ClassificationId { get; set; }
        public long? CreatedByUserId { get; set; }
        public string? CreatedByUserName { get; set; }
        public string? LoanStatus { get; set; }
        public int FileCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
    }
}
