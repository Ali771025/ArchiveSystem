using System.Text.Json;
using ArchiveSystem.Presentation.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ArchiveSystem.Infrastructure.Tests.Security
{
    public class GlobalExceptionMiddlewareSecurityTests
    {
        [Fact]
        public async Task InvokeAsync_UnhandledException_ReturnsGenericMessageWithoutStackTrace()
        {
            var middleware = new GlobalExceptionMiddleware(
                _ => throw new InvalidOperationException("database password leaked in stack"),
                NullLogger<GlobalExceptionMiddleware>.Instance);

            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            context.Items[CorrelationIdMiddleware.ItemKey] = "test-correlation";

            await middleware.InvokeAsync(context);

            context.Response.Body.Position = 0;
            var payload = await JsonSerializer.DeserializeAsync<JsonElement>(
                context.Response.Body,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));

            Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
            Assert.Equal("بروزخطا لطفا با پشتیبانی تماس بگیرید", payload.GetProperty("title").GetString());
            Assert.Equal("بروزخطا لطفا با پشتیبانی تماس بگیرید", payload.GetProperty("detail").GetString());
            Assert.DoesNotContain("InvalidOperationException", payload.ToString(), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("database password leaked", payload.ToString(), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("stack", payload.ToString(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
