using ArchiveSystem.Application.AccessControl;
using ArchiveSystem.Domain.Enums;
using Microsoft.Extensions.Caching.Memory;

namespace ArchiveSystem.Infrastructure.Services
{
    public class AuthorizationMemoryCache : IAuthorizationCache
    {
        private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(10);
        private readonly IMemoryCache _memoryCache;

        public AuthorizationMemoryCache(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        public Task<IReadOnlyDictionary<long, ClassificationPermissionMask>> GetOrCreateClassificationPermissionsAsync(
            long userId,
            Func<Task<IReadOnlyDictionary<long, ClassificationPermissionMask>>> factory,
            CancellationToken cancellationToken = default)
        {
            return GetOrCreateAsync($"auth:classification:{userId}", factory);
        }

        public Task<IReadOnlyCollection<long>> GetOrCreateRolesAsync(
            long userId,
            Func<Task<IReadOnlyCollection<long>>> factory,
            CancellationToken cancellationToken = default)
        {
            return GetOrCreateAsync($"auth:roles:{userId}", factory);
        }

        public Task<IReadOnlyDictionary<long, TagPermissionMask>> GetOrCreateTagPermissionsAsync(
            long userId,
            Func<Task<IReadOnlyDictionary<long, TagPermissionMask>>> factory,
            CancellationToken cancellationToken = default)
        {
            return GetOrCreateAsync($"auth:tag:{userId}", factory);
        }

        public void InvalidateUser(long userId)
        {
            _memoryCache.Remove($"auth:classification:{userId}");
            _memoryCache.Remove($"auth:roles:{userId}");
            _memoryCache.Remove($"auth:tag:{userId}");
        }

        public void InvalidateAll()
        {
            if (_memoryCache is MemoryCache memoryCache)
            {
                memoryCache.Compact(1.0);
            }
        }

        private async Task<TValue> GetOrCreateAsync<TValue>(string key, Func<Task<TValue>> factory)
        {
            if (_memoryCache.TryGetValue<TValue>(key, out var cached) && cached != null)
            {
                return cached;
            }

            var computed = await factory();
            _memoryCache.Set(key, computed, DefaultTtl);
            return computed;
        }
    }
}
