// src/ArchiveSystem.Worker.Ocr/Services/OcrJsonSerializer.cs
using System.Text.Json;

namespace ArchiveSystem.Worker.Ocr.Services
{
    public static class OcrJsonSerializer
    {
        public static readonly JsonSerializerOptions PayloadOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }
}
