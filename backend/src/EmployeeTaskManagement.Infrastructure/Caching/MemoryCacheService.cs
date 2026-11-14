using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using EmployeeTaskManagement.Application.Interfaces.Caching;
using Microsoft.Extensions.Caching.Memory;

namespace EmployeeTaskManagement.Infrastructure.Caching
{
    /// <summary>
    /// Thread-safe IMemoryCache wrapper with keyed stampede locks and O(1) scope-version invalidation.
    /// </summary>
    public sealed class MemoryCacheService : ICacheService
    {
        private readonly IMemoryCache _cache;
        private readonly ConcurrentDictionary<string, long> _versions = new(StringComparer.Ordinal);
        // Striped semaphore array avoids unbounded per-key lock dictionary growth.
        private readonly SemaphoreSlim[] _locks;

        public MemoryCacheService(IMemoryCache cache)
        {
            _cache = cache;
            _locks = new SemaphoreSlim[64];
            for (var i = 0; i < _locks.Length; i++) _locks[i] = new SemaphoreSlim(1, 1);
        }

        public async Task<T> GetOrCreateAsync<T>(
            string key,
            Func<CancellationToken, Task<T>> factory,
            TimeSpan ttl,
            CancellationToken ct = default)
        {
            if (_cache.TryGetValue(key, out T? existing) && existing is not null)
                return existing;

            var gate = _locks[(uint)key.GetHashCode() % (uint)_locks.Length];
            await gate.WaitAsync(ct);
            try
            {
                if (_cache.TryGetValue(key, out existing) && existing is not null)
                    return existing;

                var value = await factory(ct);
                _cache.Set(key, value, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = ttl
                });
                return value;
            }
            finally
            {
                gate.Release();
            }
        }

        public Task RemoveAsync(string key, CancellationToken ct = default)
        {
            _cache.Remove(key);
            return Task.CompletedTask;
        }

        public Task<long> GetVersionAsync(string scope, CancellationToken ct = default) =>
            Task.FromResult(_versions.GetOrAdd(scope, 1));

        public Task BumpVersionAsync(string scope, CancellationToken ct = default)
        {
            _versions.AddOrUpdate(scope, 2, (_, current) => current + 1);
            return Task.CompletedTask;
        }
    }
}
