using System.Text.Json;

namespace ArchiveSystem.Application.Common;

public static class OwnerDisplayNameParser
{
    private static readonly string[] DirectNameProperties =
    {
        "fullName", "nameOrTitle", "companyName", "ownerName", "label", "name"
    };

    public static IReadOnlyList<string> ParseNames(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Array.Empty<string>();
        }

        try
        {
            using var document = JsonDocument.Parse(value);
            var names = new List<string>();
            CollectNames(document.RootElement, names);
            return names
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch (JsonException)
        {
            return new[] { value.Trim() };
        }
    }

    private static void CollectNames(JsonElement element, ICollection<string> names)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    CollectNames(item, names);
                }
                break;

            case JsonValueKind.Object:
                if (TryReadDirectName(element, out var directName))
                {
                    names.Add(directName);
                    break;
                }

                var firstName = ReadString(element, "firstName");
                var lastName = ReadString(element, "lastName");
                var fullName = string.Join(" ", new[] { firstName, lastName }.Where(x => !string.IsNullOrWhiteSpace(x)));
                if (!string.IsNullOrWhiteSpace(fullName))
                {
                    names.Add(fullName);
                }
                break;

            case JsonValueKind.String:
                var text = element.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    var nestedNames = ParseNames(text);
                    foreach (var nestedName in nestedNames)
                    {
                        names.Add(nestedName);
                    }
                }
                break;
        }
    }

    private static bool TryReadDirectName(JsonElement element, out string value)
    {
        foreach (var propertyName in DirectNameProperties)
        {
            var candidate = ReadString(element, propertyName);
            if (!string.IsNullOrWhiteSpace(candidate))
            {
                value = candidate;
                return true;
            }
        }

        value = string.Empty;
        return false;
    }

    private static string ReadString(JsonElement element, string propertyName)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase) &&
                property.Value.ValueKind == JsonValueKind.String)
            {
                return property.Value.GetString()?.Trim() ?? string.Empty;
            }
        }

        return string.Empty;
    }
}
