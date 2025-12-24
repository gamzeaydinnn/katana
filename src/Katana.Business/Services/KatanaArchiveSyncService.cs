using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Katana.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Katana.Business.Services;

/// <summary>
/// Service for synchronizing product archives between local database and Katana API.
/// Archives products in Katana that don't exist in local database.
/// Local database is the source of truth.
/// </summary>
public class KatanaArchiveSyncService : IKatanaArchiveSyncService
{
    private readonly IKatanaService _katanaService;
    private readonly IProductService _productService;
    private readonly ILogger<KatanaArchiveSyncService> _logger;
    
    private const int MaxRetries = 3;
    private const int BaseDelayMs = 1000;

    public KatanaArchiveSyncService(
        IKatanaService katanaService,
        IProductService productService,
        ILogger<KatanaArchiveSyncService> logger)
    {
        _katanaService = katanaService;
        _productService = productService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<ProductArchivePreview>> GetArchivePreviewAsync()
    {
        _logger.LogInformation("Starting archive preview - fetching products from both sources");

        // Get all products from local database
        var localProducts = await _productService.GetAllProductsAsync();
        var localSkuSet = new HashSet<string>(
            localProducts.Select(p => p.SKU?.Trim() ?? string.Empty),
            StringComparer.OrdinalIgnoreCase);

        _logger.LogInformation("Found {Count} products in local database", localSkuSet.Count);

        // Get all products from Katana
        var katanaProducts = await _katanaService.GetProductsAsync();
        _logger.LogInformation("Found {Count} products in Katana", katanaProducts.Count);

        // Find products in Katana that don't exist in local database
        var productsToArchive = katanaProducts
            .Where(k => !string.IsNullOrWhiteSpace(k.SKU) && !localSkuSet.Contains(k.SKU.Trim()))
            .Select(k => new ProductArchivePreview
            {
                KatanaProductId = int.TryParse(k.Id, out var id) ? id : 0,
                SKU = k.SKU?.Trim() ?? string.Empty,
                Name = k.Name ?? string.Empty,
                IsAlreadyArchived = !k.IsActive
            })
            .Where(p => p.KatanaProductId > 0)
            .ToList();

        _logger.LogInformation("Found {Count} products to archive (in Katana but not in local DB)", productsToArchive.Count);

        return productsToArchive;
    }

    /// <inheritdoc />
    public async Task<ArchiveSyncResult> SyncArchiveAsync(bool previewOnly = false)
    {
        var result = new ArchiveSyncResult
        {
            SyncStartedAt = DateTime.UtcNow,
            IsPreviewOnly = previewOnly
        };

        try
        {
            _logger.LogInformation("Starting archive sync (previewOnly: {PreviewOnly})", previewOnly);

            // Get counts for reporting
            var localProducts = await _productService.GetAllProductsAsync();
            result.TotalLocalProducts = localProducts.Count();

            var katanaProducts = await _katanaService.GetProductsAsync();
            result.TotalKatanaProducts = katanaProducts.Count;

            // Get products to archive
            var productsToArchive = await GetArchivePreviewAsync();
            result.ProductsToArchive = productsToArchive.Count;
            result.ArchivedProducts = productsToArchive;

            _logger.LogInformation(
                "Archive sync summary: Local={Local}, Katana={Katana}, ToArchive={ToArchive}",
                result.TotalLocalProducts, result.TotalKatanaProducts, result.ProductsToArchive);

            if (previewOnly)
            {
                _logger.LogInformation("Preview mode - no changes will be made");
                result.SyncCompletedAt = DateTime.UtcNow;
                return result;
            }

            // Execute archive operations with retry logic
            foreach (var product in productsToArchive)
            {
                var success = await ArchiveWithRetryAsync(product, result);
                
                if (success)
                {
                    result.ArchivedSuccessfully++;
                }
                else
                {
                    result.ArchiveFailed++;
                }

                // Rate limiting - small delay between requests
                await Task.Delay(100);
            }

            result.SyncCompletedAt = DateTime.UtcNow;

            _logger.LogInformation(
                "Archive sync completed: Success={Success}, Failed={Failed}, Duration={Duration}s",
                result.ArchivedSuccessfully, result.ArchiveFailed, result.DurationSeconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Archive sync failed with exception");
            result.SyncCompletedAt = DateTime.UtcNow;
            result.Errors.Add(new ArchiveError
            {
                ErrorMessage = $"Sync failed: {ex.Message}"
            });
            return result;
        }
    }

    /// <summary>
    /// Archives a product with exponential backoff retry logic for rate limiting.
    /// </summary>
    private async Task<bool> ArchiveWithRetryAsync(ProductArchivePreview product, ArchiveSyncResult result)
    {
        for (int attempt = 0; attempt < MaxRetries; attempt++)
        {
            try
            {
                _logger.LogDebug("Archiving product: ID={Id}, SKU={SKU}, Attempt={Attempt}", 
                    product.KatanaProductId, product.SKU, attempt + 1);

                var success = await _katanaService.ArchiveProductAsync(product.KatanaProductId);

                if (success)
                {
                    _logger.LogInformation("✅ Archived product: ID={Id}, SKU={SKU}", 
                        product.KatanaProductId, product.SKU);
                    return true;
                }
                else
                {
                    _logger.LogWarning("❌ Archive returned false for product: ID={Id}, SKU={SKU}", 
                        product.KatanaProductId, product.SKU);
                    
                    // Don't retry if the API explicitly returned false (not a rate limit)
                    result.Errors.Add(new ArchiveError
                    {
                        KatanaProductId = product.KatanaProductId,
                        SKU = product.SKU,
                        ErrorMessage = "Archive operation returned false"
                    });
                    return false;
                }
            }
            catch (HttpRequestException ex) when (IsRateLimitError(ex))
            {
                var delay = CalculateBackoffDelay(attempt);
                _logger.LogWarning(
                    "Rate limit hit for product {Id}. Retrying in {Delay}ms (attempt {Attempt}/{MaxRetries})",
                    product.KatanaProductId, delay, attempt + 1, MaxRetries);
                
                await Task.Delay(delay);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error archiving product: ID={Id}, SKU={SKU}, Attempt={Attempt}", 
                    product.KatanaProductId, product.SKU, attempt + 1);
                
                if (attempt == MaxRetries - 1)
                {
                    result.Errors.Add(new ArchiveError
                    {
                        KatanaProductId = product.KatanaProductId,
                        SKU = product.SKU,
                        ErrorMessage = ex.Message
                    });
                    return false;
                }
                
                // Exponential backoff for other errors too
                var delay = CalculateBackoffDelay(attempt);
                await Task.Delay(delay);
            }
        }

        result.Errors.Add(new ArchiveError
        {
            KatanaProductId = product.KatanaProductId,
            SKU = product.SKU,
            ErrorMessage = $"Failed after {MaxRetries} retry attempts"
        });
        return false;
    }

    /// <summary>
    /// Calculates exponential backoff delay: 1s, 2s, 4s
    /// </summary>
    private int CalculateBackoffDelay(int attempt)
    {
        return BaseDelayMs * (int)Math.Pow(2, attempt);
    }

    /// <summary>
    /// Checks if the exception is a rate limit error (HTTP 429)
    /// </summary>
    private static bool IsRateLimitError(HttpRequestException ex)
    {
        return ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests ||
               ex.Message.Contains("429") ||
               ex.Message.Contains("rate limit", StringComparison.OrdinalIgnoreCase);
    }
}
