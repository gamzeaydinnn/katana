using System.Collections.Concurrent;
using System.Net;
using Microsoft.Extensions.Caching.Memory;
using Katana.Core.Interfaces;

namespace Katana.Infrastructure.APIClients;

public class LucaCookieJarStore : ILucaCookieJarStore
{
    private readonly IMemoryCache _cache;
    private readonly MemoryCacheEntryOptions _options;

    public LucaCookieJarStore(IMemoryCache cache)
    {
        _cache = cache;
        _options = new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(60),
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6),
            Size = 1
        };
    }

    public CookieContainer GetOrCreate(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("sessionId cannot be null or empty", nameof(sessionId));
        }

        if (_cache.TryGetValue(sessionId, out var existingObj))
        {
            if (existingObj is CookieContainer existing)
            {
                return existing;
            }
        }

        var container = new CookieContainer();

        // Cache with sliding expiration; keep small size accounting for limiters
        _cache.Set(sessionId, container, _options);
        return container;
    }

    public void Clear(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) return;
        _cache.Remove(sessionId);
    }
}
