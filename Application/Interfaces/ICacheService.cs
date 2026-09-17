using System;
using System.Threading.Tasks;

namespace dagangOnline.Application.Interfaces;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
    Task RemoveAsync(string key);
    string BuildKey(string tenantId, string scope, string language, string normalizedQuery, string version = "v1");
}
