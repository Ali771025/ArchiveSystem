// src/ArchiveSystem.Domain/ValueObjects/FileSize.cs
namespace ArchiveSystem.Domain.ValueObjects
{
    public sealed class FileSize : IEquatable<FileSize>
    {
        public long Bytes { get; }

        private FileSize(long bytes)
        {
            if (bytes < 0)
                throw new ArgumentException("File size cannot be negative.", nameof(bytes));

            Bytes = bytes;
        }

        public static FileSize Create(long bytes)
        {
            return new FileSize(bytes);
        }

        public double GetSizeInKilobytes() => Bytes / 1024.0;
        public double GetSizeInMegabytes() => Bytes / (1024.0 * 1024.0);
        public double GetSizeInGigabytes() => Bytes / (1024.0 * 1024.0 * 1024.0);

        public override string ToString() => $"{Bytes} bytes";

        public override bool Equals(object? obj) => obj is FileSize other && Equals(other);

        public bool Equals(FileSize? other) => other != null && Bytes == other.Bytes;

        public override int GetHashCode() => Bytes.GetHashCode();

        public static bool operator ==(FileSize? left, FileSize? right) => EqualityComparer<FileSize>.Default.Equals(left, right);

        public static bool operator !=(FileSize? left, FileSize? right) => !(left == right);

        public static FileSize operator +(FileSize left, FileSize right) => new FileSize(left.Bytes + right.Bytes);
        public static FileSize operator -(FileSize left, FileSize right) => new FileSize(Math.Max(0, left.Bytes - right.Bytes));
    }
}