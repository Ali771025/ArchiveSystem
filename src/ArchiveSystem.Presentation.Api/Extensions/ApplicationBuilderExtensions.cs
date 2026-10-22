// src/ArchiveSystem.Presentation.Api/Extensions/ApplicationBuilderExtensions.cs
using ArchiveSystem.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace ArchiveSystem.Presentation.Api.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
        {
            app.UseExceptionHandler(appError =>
            {
                appError.Run(async context =>
                {
                    context.Response.ContentType = "application/json";
                    var environment = context.RequestServices.GetRequiredService<IWebHostEnvironment>();

                    var contextFeature = context.Features.Get<IExceptionHandlerFeature>();
                    if (contextFeature != null)
                    {
                        var error = contextFeature.Error;

                        var response = new
                        {
                            message = error.Message,
                            details = error.InnerException?.Message ?? error.Message,
                            stackTrace = environment.IsDevelopment() ? error.StackTrace : null
                        };

                        context.Response.StatusCode = error switch
                        {
                            ValidationException => 400,
                            NotFoundException => 404,
                            _ => 500
                        };

                        var jsonResponse = JsonSerializer.Serialize(response);
                        await context.Response.WriteAsync(jsonResponse);
                    }
                });
            });

            return app;
        }
    }
}
