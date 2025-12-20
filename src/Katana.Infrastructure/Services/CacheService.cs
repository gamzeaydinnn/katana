using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace Katana.Infrastructure.Services;
public class CacheService
{
    private readonly IMemoryCache _cache;

    public CacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public T? Get<T>(string key)
    {
        return _cache.TryGetValue(key, out T? value) ? value : default;
    }

    public T GetOrAdd<T>(string key, Func<ICacheEntry, T> factory, TimeSpan ttl)
    {
        return _cache.GetOrCreate(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = ttl;
            return factory(entry);
        })!;
    }

    public Task<T> GetOrAddAsync<T>(string key, Func<ICacheEntry, Task<T>> factory, TimeSpan ttl)
    {
        return _cache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = ttl;
            return await factory(entry);
        })!;
    }

    public void Remove(string key) => _cache.Remove(key);
}

