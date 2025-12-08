using System.Threading.Tasks;
using System.Collections.Generic;

namespace Katana.Business.Interfaces
{
    /// <summary>
    /// Redis-based cache interface for Luca stock cards
    /// Provides SKU → StockCardId mapping with persistent storage
    /// </summary>
    public interface IStockCardCache
    {
        /// <summary>
        /// Get stock card ID by SKU from Redis cache
        /// </summary>
        /// <param name="sku">Stock keeping unit (product SKU)</param>
        /// <returns>Stock card ID if found, null otherwise</returns>
        Task<long?> GetStockCardIdAsync(string sku);

        /// <summary>
        /// Get multiple stock card IDs by SKUs from Redis cache
        /// </summary>
        /// <param name="skus">List of SKUs to lookup</param>
        /// <returns>Dictionary of SKU → StockCardId mappings</returns>
        Task<Dictionary<string, long>> GetStockCardIdsAsync(IEnumerable<string> skus);

        /// <summary>
        /// Set stock card ID for a SKU in Redis cache
        /// </summary>
        /// <param name="sku">Stock keeping unit</param>
        /// <param name="stockCardId">Luca stock card ID</param>
        Task SetStockCardIdAsync(string sku, long stockCardId);

        /// <summary>
        /// Set multiple stock card IDs in Redis cache (bulk operation)
        /// </summary>
        /// <param name="mapping">Dictionary of SKU → StockCardId mappings</param>
        Task SetStockCardIdsAsync(Dictionary<string, long> mapping);

        /// <summary>
        /// Check if cache is warmed up (contains data)
        /// </summary>
        /// <returns>True if cache has at least one entry</returns>
        Task<bool> IsCacheWarmedAsync();

        /// <summary>
        /// Get total count of cached entries
        /// </summary>
        /// <returns>Number of SKU → StockCardId mappings in cache</returns>
        Task<long> GetCacheCountAsync();

        /// <summary>
        /// Clear all cache entries (use with caution)
        /// </summary>
        Task ClearCacheAsync();

        /// <summary>
        /// Get cache status information (for diagnostics)
        /// </summary>
        /// <returns>Cache status details including entry count and health</returns>
        Task<(bool IsHealthy, long EntryCount, string Status)> GetCacheStatusAsync();

        /// <summary>
        /// Warmup cache with initial stock card data from Luca API
        /// </summary>
        /// <param name="stockCards">Dictionary of SKU → StockCardId to preload</param>
        Task WarmupCacheAsync(Dictionary<string, long> stockCards);
    }
}
