namespace ArchiveSystem.Application.Common.Exceptions
{
    public sealed class NotAllowedExtensionFileException : AppException
    {
        public NotAllowedExtensionFileException(string extension)
            : base($"File extension '{extension}' is not allowed.", "NOT_ALLOWED_EXTENSION_FILE")
        {
        }
    }
}
