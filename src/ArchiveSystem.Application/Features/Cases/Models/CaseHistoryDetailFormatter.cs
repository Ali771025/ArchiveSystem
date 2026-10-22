using System.Text.Json;
using ArchiveSystem.Application.Features.Reports.Models;

namespace ArchiveSystem.Application.Features.Cases.Models
{
    internal static class CaseHistoryDetailFormatter
    {
        public static string Format(ActivityReportItemDto item)
        {
            if (!string.Equals(item.ActionType, "CASE_MOVED", StringComparison.OrdinalIgnoreCase))
            {
                return $"{item.EntityType}:{item.EntityId}";
            }

            var source = ReadClassificationDescription(item.BeforeJson);
            var target = ReadClassificationDescription(item.AfterJson);

            return source is not null && target is not null
                ? $"از {source} به {target}"
                : $"{item.EntityType}:{item.EntityId}";
        }

        private static string? ReadClassificationDescription(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                if (root.TryGetProperty("classificationPath", out var pathElement))
                {
                    var path = pathElement.GetString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        return path;
                    }
                }

                if (root.TryGetProperty("classificationId", out var idElement) &&
                    idElement.TryGetInt64(out var id))
                {
                    return $"طبقه‌بندی {id}";
                }
            }
            catch (JsonException)
            {
                return null;
            }

            return null;
        }
    }
}
