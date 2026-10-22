namespace ArchiveSystem.Presentation.Api.Middleware
{
    public class CorrelationIdMiddleware
    {
        public const string HeaderName = "X-Correlation-ID";
        public const string ItemKey = "CorrelationId";

        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var providedValue)
                && !string.IsNullOrWhiteSpace(providedValue)
                ? providedValue.ToString()
                : Guid.NewGuid().ToString("N");

            context.Items[ItemKey] = correlationId;
            context.Response.Headers[HeaderName] = correlationId;

            await _next(context);
        }
    }
}
