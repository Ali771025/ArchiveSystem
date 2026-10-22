// src/ArchiveSystem.Application/Common/Exceptions/DomainException.cs
namespace ArchiveSystem.Application.Common.Exceptions
{
    public abstract class DomainException : AppException
    {
        protected DomainException(string message, string errorCode = "DOMAIN_ERROR") : base(message, errorCode)
        {
        }

        protected DomainException(string message, Exception innerException, string errorCode = "DOMAIN_ERROR")
            : base(message, errorCode, innerException)
        {
        }
    }

    public class UnauthorizedAccessException : DomainException
    {
        public UnauthorizedAccessException(string message) : base(message, "UNAUTHORIZED") { }
        public UnauthorizedAccessException(string message, Exception innerException) : base(message, innerException, "UNAUTHORIZED") { }
    }

    public class ConflictException : DomainException
    {
        public ConflictException(string message) : base(message, "CONFLICT") { }
        public ConflictException(string message, Exception innerException) : base(message, innerException, "CONFLICT") { }
    }
}
