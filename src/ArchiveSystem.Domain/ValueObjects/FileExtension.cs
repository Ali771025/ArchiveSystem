// src/ArchiveSystem.Domain/ValueObjects/FileExtension.cs
using System.Text.RegularExpressions;

namespace ArchiveSystem.Domain.ValueObjects
{
    public sealed class FileExtension : IEquatable<FileExtension>
    {
        private static readonly Regex ExtensionRegex = new(@"^\.[a-zA-Z0-9]{1,10}$", RegexOptions.Compiled);

        public string Value { get; }

        private FileExtension(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Extension cannot be null or empty.", nameof(value));

            if (!ExtensionRegex.IsMatch(value))
                throw new ArgumentException($"Invalid extension format: {value}", nameof(value));

            Value = value.ToLowerInvariant();
        }

        public static FileExtension Create(string extension)
        {
            if (extension == null)
                throw new ArgumentNullException(nameof(extension));

            if (!extension.StartsWith("."))
                extension = "." + extension;

            return new FileExtension(extension);
        }

        public override string ToString() => Value;

        public override bool Equals(object? obj) => obj is FileExtension other && Equals(other);

        public bool Equals(FileExtension? other) => other != null && Value.Equals(other.Value, StringComparison.OrdinalIgnoreCase);

        public override int GetHashCode() => Value.GetHashCode(StringComparison.OrdinalIgnoreCase);

        public static bool operator ==(FileExtension? left, FileExtension? right) => EqualityComparer<FileExtension>.Default.Equals(left, right);

        public static bool operator !=(FileExtension? left, FileExtension? right) => !(left == right);
    }
}