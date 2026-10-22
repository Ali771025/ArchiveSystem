using System.Text.Json;
using ArchiveSystem.Application.Common.Exceptions;
using FluentValidation;
using Microsoft.Data.SqlClient;

namespace ArchiveSystem.Presentation.Api.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private const string UnhandledErrorMessage = "بروزخطا لطفا با پشتیبانی تماس بگیرید";
        private const string DatabaseUnavailableMessage = "ارتباط با پایگاه داده برقرار نشد. لطفا از اتصال دیتابیس اطمینان حاصل کنید و دوباره تلاش کنید.";
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                // The client closed or replaced the request (for example while changing
                // report filters). This is not an application failure and must not be
                // logged or returned as an internal-server error.
                if (!context.Response.HasStarted)
                {
                    context.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
                }
            }
            catch (Exception exception)
            {
                await HandleExceptionAsync(context, exception);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var isDatabaseUnavailable = IsDatabaseUnavailable(exception);
            var (status, title) = isDatabaseUnavailable
                ? (StatusCodes.Status503ServiceUnavailable, DatabaseUnavailableMessage)
                : exception switch
            {
                ArchiveSystem.Application.Common.Exceptions.ValidationException => (StatusCodes.Status400BadRequest, "اعتبارسنجی ناموفق بود."),
                FluentValidation.ValidationException => (StatusCodes.Status400BadRequest, "اعتبارسنجی ناموفق بود."),
                NotFoundException => (StatusCodes.Status404NotFound, "Resource was not found."),
                ConflictException => (StatusCodes.Status409Conflict, "Conflict."),
                ForbiddenAccessException => (StatusCodes.Status403Forbidden, "Forbidden."),
                ServiceUnavailableException => (StatusCodes.Status503ServiceUnavailable, "Service unavailable."),
                System.UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Forbidden."),
                ArchiveSystem.Application.Common.Exceptions.UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Forbidden."),
                _ => (StatusCodes.Status500InternalServerError, UnhandledErrorMessage)
            };

            if (status >= StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(exception, "Unhandled exception. CorrelationId: {CorrelationId}", context.Items[CorrelationIdMiddleware.ItemKey]);
            }
            else
            {
                _logger.LogInformation(
                    "Request failed with {StatusCode}. CorrelationId: {CorrelationId}. Detail: {Detail}",
                    status,
                    context.Items[CorrelationIdMiddleware.ItemKey],
                    exception.Message);
            }

            var correlationId = context.Items[CorrelationIdMiddleware.ItemKey]?.ToString()
                                ?? context.TraceIdentifier;

            var payload = new ApiErrorResponse
            {
                Status = status,
                Title = title,
                Detail = isDatabaseUnavailable
                    ? DatabaseUnavailableMessage
                    : status == StatusCodes.Status500InternalServerError ? UnhandledErrorMessage : exception.Message,
                CorrelationId = correlationId,
                Errors = exception switch
                {
                    ArchiveSystem.Application.Common.Exceptions.ValidationException validationException => validationException.Errors,
                    FluentValidation.ValidationException fluentValidationException => fluentValidationException.Errors
                        .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
                        .ToDictionary(group => group.Key, group => group.ToArray()),
                    _ => null
                }
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = status;
            await context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
        }

        private static bool IsDatabaseUnavailable(Exception exception)
        {
            for (var current = exception; current is not null; current = current.InnerException)
            {
                if (current is SqlException sqlException && IsConnectionFailure(sqlException))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsConnectionFailure(SqlException exception) =>
            exception.Errors
                .Cast<SqlError>()
                .Any(error => error.Number is -2 or 2 or 53 or 233 or 4060 or 10053 or 10054 or 10060 or 11001 or 18456);
    }

    public sealed class ApiErrorResponse
    {
        public int Status { get; init; }
        public string Title { get; init; } = string.Empty;
        public string? Detail { get; init; }
        public string? CorrelationId { get; init; }
        public IDictionary<string, string[]>? Errors { get; init; }
    }
}
