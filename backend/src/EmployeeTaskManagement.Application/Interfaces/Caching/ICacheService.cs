using System;
using System.Threading;
using System.Threading.Tasks;

namespace EmployeeTaskManagement.Application.Interfaces.Caching
{
    public interface ICacheService
    {
        Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan ttl, CancellationToken ct = default);
        Task RemoveAsync(string key, CancellationToken ct = default);
        Task<long> GetVersionAsync(string scope, CancellationToken ct = default);
        Task BumpVersionAsync(string scope, CancellationToken ct = default);
    }

    public static class CacheScopes
    {
        public const string Dashboard = "dashboard";
        public const string Users = "users";
        public const string Roles = "roles";
    }
}
