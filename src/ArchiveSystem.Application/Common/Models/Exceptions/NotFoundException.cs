// src/ArchiveSystem.Application/Common/Exceptions/NotFoundException.cs
namespace ArchiveSystem.Application.Common.Exceptions
{
    public class NotFoundException : Exception
    {
        public NotFoundException() : base() { }

        public NotFoundException(string message) : base(message) { }

        public NotFoundException(string message, Exception innerException) : base(message, innerException) { }
    }
}