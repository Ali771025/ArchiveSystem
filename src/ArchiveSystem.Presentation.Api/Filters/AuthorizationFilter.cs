// src/ArchiveSystem.Presentation.Api/Filters/AuthorizationFilter.cs
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using ArchiveSystem.Infrastructure.Services;

namespace ArchiveSystem.Presentation.Api.Filters
{
    public class AdvancedAuthorizationFilter : IAsyncAuthorizationFilter
    {
        private readonly string _permission;
        private readonly string _resourceType;

        public AdvancedAuthorizationFilter(string permission, string resourceType = "Case")
        {
            _permission = permission;
            _resourceType = resourceType;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var userService = context.HttpContext.RequestServices.GetRequiredService<IUserService>();
            var userId = GetUserId(context.HttpContext);

            if (userId <= 0)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            // Extract resource ID from route
            var resourceId = GetResourceId(context, _resourceType);

            if (resourceId <= 0)
            {
                // For create operations, we might not have an ID yet
                if (_permission == "Create")
                {
                    // Check if user can create in general or in a specific classification
                    var canCreate = await CanUserCreate(context, userId);
                    if (!canCreate)
                    {
                        context.Result = new ForbidResult();
                        return;
                    }
                }
                else
                {
                    context.Result = new BadRequestResult();
                    return;
                }
            }
            else
            {
                var hasPermission = await userService.HasPermissionAsync(userId, resourceId, _permission);
                if (!hasPermission)
                {
                    context.Result = new ForbidResult();
                    return;
                }
            }
        }

        private long GetUserId(HttpContext context)
        {
            var userIdClaim = context.User.FindFirst("sub")?.Value;
            return long.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        private long GetResourceId(AuthorizationFilterContext context, string resourceType)
        {
            var routeValues = context.RouteData.Values;
            var idParam = routeValues["id"]?.ToString() ?? 
                         routeValues["caseId"]?.ToString() ?? 
                         routeValues["fileId"]?.ToString() ?? 
                         routeValues["classificationId"]?.ToString();

            return long.TryParse(idParam, out var id) ? id : 0;
        }

        private async Task<bool> CanUserCreate(AuthorizationFilterContext context, long userId)
        {
            // Implementation would check if user can create in specific classifications
            // This could involve checking against user's assigned classifications or roles
            var userService = context.HttpContext.RequestServices.GetRequiredService<IUserService>();
            return await userService.CanUserCreateAsync(userId);
        }
    }
}
