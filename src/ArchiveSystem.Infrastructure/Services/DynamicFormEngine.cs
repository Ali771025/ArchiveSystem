// src/ArchiveSystem.Infrastructure/Services/DynamicFormEngine.cs
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ArchiveSystem.Application.Common.Exceptions;

namespace ArchiveSystem.Infrastructure.Services
{
    public interface IDynamicFormEngine
    {
        Task<DynamicFormDefinition> GenerateFormDefinitionAsync(long classificationId);
        Task<Dictionary<string, object>> ValidateFormDataAsync(long classificationId, Dictionary<string, object> formData);
    }

    public class DynamicFormEngine : IDynamicFormEngine
    {
        private readonly ArchiveDbContext _context;
        private readonly ILogger<DynamicFormEngine> _logger;

        public DynamicFormEngine(ArchiveDbContext context, ILogger<DynamicFormEngine> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<DynamicFormDefinition> GenerateFormDefinitionAsync(long classificationId)
        {
            var classification = await _context.Classifications.FindAsync(classificationId);
            if (classification == null)
            {
                throw new ArgumentException($"Classification with ID {classificationId} not found");
            }

            var dynamicFieldGroups = JsonSerializer.Deserialize<JsonElement>(classification.DynamicFieldGroups);
            var formDefinition = new DynamicFormDefinition();

            if (dynamicFieldGroups.ValueKind == JsonValueKind.Array)
            {
                foreach (var group in dynamicFieldGroups.EnumerateArray())
                {
                    var groupName = group.GetProperty("name").GetString() ?? "Unnamed Group";
                    var groupDefinition = new FormGroupDefinition { Name = groupName };

                    if (group.TryGetProperty("fields", out var fieldsElement))
                    {
                        foreach (var field in fieldsElement.EnumerateArray())
                        {
                            var fieldDef = new FormFieldDefinition
                            {
                                Name = field.GetProperty("name").GetString() ?? "unnamed_field",
                                Label = field.GetProperty("label").GetString() ?? "Label",
                                Type = field.GetProperty("type").GetString() ?? "text",
                                Required = field.GetProperty("required").GetBoolean(),
                                Options = field.TryGetProperty("options", out var optionsElement) 
                                    ? JsonSerializer.Deserialize<Dictionary<string, string>>(optionsElement.GetRawText()) ?? new Dictionary<string, string>()
                                    : new Dictionary<string, string>(),
                                ValidationRegex = field.TryGetProperty("validationRegex", out var regexElement) 
                                    ? regexElement.GetString()
                                    : null,
                                DefaultValue = field.TryGetProperty("defaultValue", out var defaultValueElement)
                                    ? defaultValueElement.GetString()
                                    : null
                            };
                            
                            groupDefinition.Fields.Add(fieldDef);
                        }
                    }

                    formDefinition.Groups.Add(groupDefinition);
                }
            }

            return formDefinition;
        }

        public async Task<Dictionary<string, object>> ValidateFormDataAsync(long classificationId, Dictionary<string, object> formData)
        {
            var formDefinition = await GenerateFormDefinitionAsync(classificationId);
            var errors = new Dictionary<string, string>();
            var validatedData = new Dictionary<string, object>();

            foreach (var group in formDefinition.Groups)
            {
                foreach (var field in group.Fields)
                {
                    var fieldValue = formData.TryGetValue(field.Name, out var value) ? value : null;

                    // Check required fields
                    if (field.Required && (fieldValue == null || string.IsNullOrEmpty(fieldValue.ToString())))
                    {
                        errors[field.Name] = $"{field.Label} is required.";
                        continue;
                    }

                    // Validate using regex if provided
                    if (!string.IsNullOrEmpty(field.ValidationRegex) && fieldValue != null)
                    {
                        var regex = new System.Text.RegularExpressions.Regex(field.ValidationRegex);
                        if (!regex.IsMatch(fieldValue.ToString() ?? ""))
                        {
                            errors[field.Name] = $"{field.Label} does not match the required format.";
                            continue;
                        }
                    }

                    validatedData[field.Name] = fieldValue ?? field.DefaultValue ?? string.Empty;
                }
            }

            if (errors.Any())
            {
                var errorMessages = string.Join("; ", errors.Values);
                throw new ValidationException(errorMessages);
            }

            return validatedData;
        }
    }

    public class DynamicFormDefinition
    {
        public List<FormGroupDefinition> Groups { get; set; } = new();
    }

    public class FormGroupDefinition
    {
        public string Name { get; set; } = string.Empty;
        public List<FormFieldDefinition> Fields { get; set; } = new();
    }

    public class FormFieldDefinition
    {
        public string Name { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Type { get; set; } = "text"; // text, number, date, dropdown, checkbox, etc.
        public bool Required { get; set; }
        public Dictionary<string, string> Options { get; set; } = new();
        public string? ValidationRegex { get; set; }
        public string? DefaultValue { get; set; }
    }
}
