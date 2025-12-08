using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Katana.Business.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Katana.Infrastructure.Caching
{
    /// <summary>
    /// Redis-based implementation of stock card cache
    /// Provides persistent SKU → StockCardId mapping across session restarts
    /// </summary>
    public class RedisStockCardCache : IStockCardCache
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<RedisStockCardCache> _logger;
        private const string KEY_PREFIX = "luca:stockcard:";
        private const string COUNT_KEY = "luca:stockcard:count";
        private const string ALL_KEYS_KEY = "luca:stockcard:all_keys";

        // Cache options: 7 days expiration (persists across session restarts)
        private static readonly DistributedCacheEntryOptions _cacheOptions = new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7),
            SlidingExpiration = TimeSpan.FromDays(1)
        };

        public RedisStockCardCache(
            IDistributedCache cache,
            ILogger<RedisStockCardCache> logger)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<long?> GetStockCardIdAsync(string sku)
        {
            if (string.IsNullOrWhiteSpace(sku))
                return null;

            try
            {
                var key = GetKey(sku);
                var cachedValue = await _cache.GetStringAsync(key);

                if (string.IsNullOrEmpty(cachedValue))
                    return null;

                if (long.TryParse(cachedValue, out var stockCardId))
                {
                    _logger.LogDebug("✅ Cache HIT for SKU '{Sku}' → {StockCardId}", sku, stockCardId);
                    return stockCardId;
                }

                _logger.LogWarning("⚠️ Invalid cache value for SKU '{Sku}': {Value}", sku, cachedValue);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Redis GET error for SKU '{Sku}'", sku);
                return null;
            }
        }

        public async Task<Dictionary<string, long>> GetStockCardIdsAsync(IEnumerable<string> skus)
        {
            var result = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

            if (skus == null || !skus.Any())
                return result;

            var tasks = skus.Distinct().Select(async sku =>
            {
                var stockCardId = await GetStockCardIdAsync(sku);
                return (sku, stockCardId);
            });

            var results = await Task.WhenAll(tasks);

            foreach (var (sku, stockCardId) in results)
            {
                if (stockCardId.HasValue)
                    result[sku] = stockCardId.Value;
            }

            _logger.LogInformation("📦 Bulk cache lookup: {Found}/{Total} SKUs found in cache",
                result.Count, skus.Count());

            return result;
        }

        public async Task SetStockCardIdAsync(string sku, long stockCardId)
        {
            if (string.IsNullOrWhiteSpace(sku))
                return;

            try
            {
                var key = GetKey(sku);
                await _cache.SetStringAsync(key, stockCardId.ToString(), _cacheOptions);

                // Track this key in the all_keys set (for count/clear operations)
                await AddKeyToTrackedSetAsync(sku);

                _logger.LogDebug("✅ Cache SET: SKU '{Sku}' → {StockCardId}", sku, stockCardId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Redis SET error for SKU '{Sku}'", sku);
            }
        }

        public async Task SetStockCardIdsAsync(Dictionary<string, long> mapping)
        {
            if (mapping == null || mapping.Count == 0)
                return;

            var startTime = DateTime.UtcNow;
            var tasks = mapping.Select(kvp => SetStockCardIdAsync(kvp.Key, kvp.Value)).ToList();

            await Task.WhenAll(tasks);

            var elapsed = DateTime.UtcNow - startTime;
            _logger.LogInformation("✅ Bulk cache SET: {Count} SKUs cached in {Elapsed}ms",
                mapping.Count, elapsed.TotalMilliseconds);
        }

        public async Task<bool> IsCacheWarmedAsync()
        {
            try
            {
                var count = await GetCacheCountAsync();
                return count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error checking cache warmup status");
                return false;
            }
        }

        public async Task<long> GetCacheCountAsync()
        {
            try
            {
                var countStr = await _cache.GetStringAsync(COUNT_KEY);
                if (string.IsNullOrEmpty(countStr))
                    return 0;

                return long.TryParse(countStr, out var count) ? count : 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting cache count");
                return 0;
            }
        }

        public async Task ClearCacheAsync()
        {
            try
            {
                _logger.LogWarning("🧹 Clearing entire stock card cache...");

                // Get all tracked keys
                var allKeysJson = await _cache.GetStringAsync(ALL_KEYS_KEY);
                if (!string.IsNullOrEmpty(allKeysJson))
                {
                    var allKeys = JsonSerializer.Deserialize<HashSet<string>>(allKeysJson);
                    if (allKeys != null && allKeys.Any())
                    {
                        var tasks = allKeys.Select(sku => _cache.RemoveAsync(GetKey(sku))).ToList();
                        await Task.WhenAll(tasks);
                        _logger.LogInformation("🧹 Cleared {Count} cache entries", allKeys.Count);
                    }
                }

                // Clear metadata
                await _cache.RemoveAsync(COUNT_KEY);
                await _cache.RemoveAsync(ALL_KEYS_KEY);

                _logger.LogInformation("✅ Cache cleared successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error clearing cache");
            }
        }

        public async Task<(bool IsHealthy, long EntryCount, string Status)> GetCacheStatusAsync()
        {
            try
            {
                var count = await GetCacheCountAsync();
                var isHealthy = count > 0;
                var status = isHealthy
                    ? $"Healthy: {count} entries cached"
                    : "Cache is empty or not warmed";

                return (isHealthy, count, status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting cache status");
                return (false, 0, $"Error: {ex.Message}");
            }
        }

        public async Task WarmupCacheAsync(Dictionary<string, long> stockCards)
        {
            if (stockCards == null || stockCards.Count == 0)
            {
                _logger.LogWarning("⚠️ Warmup called with empty stock card dictionary");
                return;
            }

            _logger.LogInformation("🔥 Warming up cache with {Count} stock cards...", stockCards.Count);
            var startTime = DateTime.UtcNow;

            await SetStockCardIdsAsync(stockCards);

            // Update count
            await _cache.SetStringAsync(COUNT_KEY, stockCards.Count.ToString(), _cacheOptions);

            var elapsed = DateTime.UtcNow - startTime;
            _logger.LogInformation("✅ Cache warmup complete: {Count} entries in {Elapsed}ms",
                stockCards.Count, elapsed.TotalMilliseconds);
        }

        private string GetKey(string sku) => $"{KEY_PREFIX}{sku.ToUpperInvariant()}";

        private async Task AddKeyToTrackedSetAsync(string sku)
        {
            try
            {
                var allKeysJson = await _cache.GetStringAsync(ALL_KEYS_KEY);
                var allKeys = string.IsNullOrEmpty(allKeysJson)
                    ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    : JsonSerializer.Deserialize<HashSet<string>>(allKeysJson) ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                if (allKeys.Add(sku))
                {
                    var updatedJson = JsonSerializer.Serialize(allKeys);
                    await _cache.SetStringAsync(ALL_KEYS_KEY, updatedJson, _cacheOptions);

                    // Update count
                    await _cache.SetStringAsync(COUNT_KEY, allKeys.Count.ToString(), _cacheOptions);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error tracking key '{Sku}'", sku);
            }
        }
    }
}
