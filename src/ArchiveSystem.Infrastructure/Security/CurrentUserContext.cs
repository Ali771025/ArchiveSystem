using System.Security.Claims;
using ArchiveSystem.Application.Common.Interfaces.Security;
using Microsoft.AspNetCore.Http;

namespace ArchiveSystem.Infrastructure.Security
{
    public sealed class CurrentUserContext : ICurrentUserContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public bool IsAuthenticated =>
            _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;

        public long? UserId
        {
            get
            {
                var principal = _httpContextAccessor.HttpContext?.User;
                var rawUserId = principal?.FindFirst("sub")?.Value
                                ?? principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                return long.TryParse(rawUserId, out var userId) ? userId : null;
            }
        }

        public bool IsInRole(string role) =>
            _httpContextAccessor.HttpContext?.User.IsInRole(role) == true;
    }
}
