using System.Security.Claims;
using Serilog.Context;

namespace ArchiveSystem.Presentation.Api.Middleware
{
    public sealed class RequestContextLoggingMiddleware
    {
        private readonly RequestDelegate _next;

        public RequestContextLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var userId = context.User.FindFirstValue("sub")
                ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? "anonymous";
            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            using (LogContext.PushProperty("UserId", userId))
            using (LogContext.PushProperty("ClientIpAddress", clientIp))
            {
                await _next(context);
            }
        }
    }
}
