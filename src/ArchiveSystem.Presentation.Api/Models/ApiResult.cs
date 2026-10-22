namespace ArchiveSystem.Presentation.Api.Models
{
    public sealed class ApiResult<T>
    {
        public bool Success { get; init; }
        public string? ErrorCode { get; init; }
        public string Message { get; init; } = string.Empty;
        public T? Data { get; init; }
        public IDictionary<string, string[]>? Errors { get; init; }

        public static ApiResult<T> Ok(T data, string message = "Operation completed successfully.")
        {
            return new ApiResult<T>
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        public static ApiResult<T> Error(string errorCode, string message, IDictionary<string, string[]>? errors = null)
        {
            return new ApiResult<T>
            {
                Success = false,
                ErrorCode = errorCode,
                Message = message,
                Errors = errors
            };
        }
    }
}
