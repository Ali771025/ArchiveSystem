namespace ArchiveSystem.Application.Common.Exceptions
{
    public abstract class AppException : Exception
    {
        public string ErrorCode { get; }

        protected AppException(string message, string errorCode) : base(message)
        {
            ErrorCode = errorCode;
        }

        protected AppException(string message, string errorCode, Exception innerException)
            : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }
}
