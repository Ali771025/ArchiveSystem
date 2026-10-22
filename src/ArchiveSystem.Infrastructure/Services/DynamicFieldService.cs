// src/ArchiveSystem.Infrastructure/Services/DynamicFieldService.cs
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using ArchiveSystem.Application.Common.Exceptions;

namespace ArchiveSystem.Infrastructure.Services
{
    public interface IDynamicFieldService
    {
        Task<Dictionary<string, object>> ValidateDynamicFieldsAsync(long classificationId, Dictionary<string, object> inputFields);
        Task<DynamicFieldSchema> GetSchemaAsync(long classificationId);
    }

    public class DynamicFieldService : IDynamicFieldService
    {
        private readonly ArchiveDbContext _context;
        private readonly ILogger<DynamicFieldService> _logger;

        public DynamicFieldService(ArchiveDbContext context, ILogger<DynamicFieldService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Dictionary<string, object>> ValidateDynamicFieldsAsync(long classificationId, Dictionary<string, object> inputFields)
        {
            var classification = await _context.Classifications.FindAsync(classificationId);
            if (classification == null)
            {
                throw new ArgumentException($"Classification with ID {classificationId} not found");
            }

            var schema = JsonSerializer.Deserialize<DynamicFieldSchema>(classification.DynamicFieldGroups) ?? new DynamicFieldSchema();
            var validationResult = new Dictionary<string, object>();
            var errors = new List<string>();

            foreach (var group in schema.Groups)
            {
                foreach (var field in group.Fields)
                {
                    var inputKey = field.Name;
                    if (!inputFields.TryGetValue(inputKey, out var inputValue))
                    {
                        if (field.Required)
                        {
                            errors.Add($"Field '{field.Label}' is required");
                        }
                        continue;
                    }

                    // Validate type
                    var isValidType = ValidateFieldType(inputValue, field.Type);
                    if (!isValidType)
                    {
                        errors.Add($"Field '{field.Label}' has invalid type. Expected: {field.Type}");
                        continue;
                    }

                    // Validate regex if provided
                    if (!string.IsNullOrEmpty(field.ValidationRegex))
                    {
                        var inputStr = inputValue?.ToString() ?? string.Empty;
                        var regex = new Regex(field.ValidationRegex);
                        if (!regex.IsMatch(inputStr))
                        {
                            errors.Add($"Field '{field.Label}' does not match the required format");
                            continue;
                        }
                    }

                    // Validate options if it's a dropdown
                    if (field.Type == "dropdown" && field.Options != null && field.Options.Any())
                    {
                        var inputStr = inputValue?.ToString() ?? string.Empty;
                        if (!field.Options.ContainsKey(inputStr))
                        {
                            errors.Add($"Field '{field.Label}' has invalid option value");
                            continue;
                        }
                    }

                    // If validation passes, add to result
                    validationResult[inputKey] = inputValue ?? string.Empty;
                }
            }

            if (errors.Any())
            {
                var errorMessage = string.Join("; ", errors);
                _logger.LogWarning("Dynamic field validation failed: {ErrorMessage}", errorMessage);
                throw new ValidationException(errorMessage);
            }

            return validationResult;
        }

        public async Task<DynamicFieldSchema> GetSchemaAsync(long classificationId)
        {
            var classification = await _context.Classifications.FindAsync(classificationId);
            if (classification == null)
            {
                throw new ArgumentException($"Classification with ID {classificationId} not found");
            }

            return JsonSerializer.Deserialize<DynamicFieldSchema>(classification.DynamicFieldGroups) 
                   ?? new DynamicFieldSchema();
        }

        private static bool ValidateFieldType(object value, string expectedType)
        {
            if (value == null) return true; // Will be handled by required validation

            return expectedType.ToLower() switch
            {
                "text" => value is string,
                "number" => value is int or long or float or double or decimal,
                "boolean" => value is bool,
                "date" => value is DateTime or string,
                "dropdown" => value is string,
                "textarea" => value is string,
                "email" => value is string,
                "phone" => value is string,
                _ => true // Allow custom types
            };
        }
    }

    public class DynamicFieldSchema
    {
        public List<FieldGroup> Groups { get; set; } = new();
    }

    public class FieldGroup
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<DynamicField> Fields { get; set; } = new();
    }

    public class DynamicField
    {
        public string Name { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Type { get; set; } = "text"; // text, number, boolean, date, dropdown, textarea, email, phone
        public bool Required { get; set; }
        public string? ValidationRegex { get; set; }
        public Dictionary<string, string>? Options { get; set; } // For dropdowns
        public object? DefaultValue { get; set; }
        public string? Placeholder { get; set; }
        public int? MaxLength { get; set; }
        public int? MinLength { get; set; }
        public string? HelpText { get; set; }
    }
}
