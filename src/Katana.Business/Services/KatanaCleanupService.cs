using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Katana.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Katana.Business.Services;

public class KatanaCleanupService : IKatanaCleanupService
{
    private readonly ICleanupRepository _cleanupRepository;
    private readonly IKatanaService _katanaService;
    private readonly ILogger<KatanaCleanupService> _logger;

    public KatanaCleanupService(
        ICleanupRepository cleanupRepository,
        IKatanaService katanaService,
        ILogger<KatanaCleanupService> logger)
    {
        _cleanupRepository = cleanupRepository;
        _katanaService = katanaService;
        _logger = logger;
    }

    public async Task<OrderProductAnalysisResult> AnalyzeOrderProductsAsync()
    {
        _logger.LogInformation("Starting order product analysis");

        var orderProducts = await _cleanupRepository.GetOrderProductsAsync();
        var approvedOrders = await _cleanupRepository.GetApprovedOrdersAsync();

        var result = new OrderProductAnalysisResult
        {
            TotalApprovedOrders = approvedOrders.Count,
            TotalProductsSentToKatana = orderProducts.Count,
            OrderProducts = orderProducts
        };

        // Calculate unique SKUs
        var uniqueSkus = orderProducts.Select(p => p.SKU).Distinct().ToList();
        result.UniqueSkuCount = uniqueSkus.Count;

        // Find duplicates - SKUs that appear in multiple orders
        var skuGroups = orderProducts.GroupBy(p => p.SKU);
        foreach (var group in skuGroups.Where(g => g.Count() > 1))
        {
            result.SkuDuplicates[group.Key] = group.Count();
        }

        // Group products by order for detailed analysis
        var productsByOrder = orderProducts
            .GroupBy(p => p.OrderId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(p => p.SKU).ToList()
            );

        _logger.LogInformation(
            "Analysis complete: {Orders} orders, {Products} products, {UniqueSkus} unique SKUs, {Duplicates} duplicates",
            result.TotalApprovedOrders,
            result.TotalProductsSentToKatana,
            result.UniqueSkuCount,
            result.SkuDuplicates.Count);

        // Log duplicate details
        if (result.SkuDuplicates.Any())
        {
            _logger.LogInformation(
                "Found {Count} duplicate SKUs across orders",
                result.SkuDuplicates.Count);

            foreach (var dup in result.SkuDuplicates.Take(10))
            {
                _logger.LogDebug(
                    "SKU {SKU} appears {Count} times",
                    dup.Key,
                    dup.Value);
            }
        }

        return result;
    }

    public async Task<KatanaCleanupResult> DeleteFromKatanaAsync(List<string> skus, bool dryRun = true)
    {
        var sw = Stopwatch.StartNew();
        var result = new KatanaCleanupResult
        {
            TotalAttempted = skus.Count
        };

        _logger.LogInformation(
            "Starting Katana cleanup for {Count} SKUs (DryRun: {DryRun})",
            skus.Count,
            dryRun);

        if (dryRun)
        {
            _logger.LogInformation("DRY RUN MODE - No actual deletions will occur");
            result.Success = true;
            result.SuccessCount = skus.Count;
            sw.Stop();
            result.Duration = sw.Elapsed;
            return result;
        }

        // Process deletions in batches of 10
        var batches = skus.Chunk(10);
        foreach (var batch in batches)
        {
            var tasks = batch.Select(async sku =>
            {
                try
                {
                    await DeleteProductFromKatanaAsync(sku);
                    result.SuccessCount++;
                    _logger.LogInformation("Successfully deleted SKU: {SKU}", sku);
                }
                catch (Exception ex)
                {
                    result.FailCount++;
                    result.Errors.Add(new CleanupError
                    {
                        Message = $"Failed to delete SKU: {sku}",
                        ErrorType = "KatanaDeletionError",
                        Details = ex.Message,
                        StackTrace = ex.StackTrace
                    });
                    _logger.LogError(ex, "Failed to delete SKU: {SKU}", sku);
                }
            });

            await Task.WhenAll(tasks);
        }

        sw.Stop();
        result.Duration = sw.Elapsed;
        result.Success = result.FailCount == 0;

        _logger.LogInformation(
            "Katana cleanup complete: {Success}/{Total} successful, {Fail} failed, Duration: {Duration}",
            result.SuccessCount,
            result.TotalAttempted,
            result.FailCount,
            result.Duration);

        return result;
    }

    public async Task<ResetResult> ResetOrderApprovalsAsync(bool dryRun = true)
    {
        var sw = Stopwatch.StartNew();
        var result = new ResetResult();

        _logger.LogInformation("Starting order reset (DryRun: {DryRun})", dryRun);

        if (dryRun)
        {
            var orders = await _cleanupRepository.GetApprovedOrdersAsync();
            result.OrdersReset = orders.Count;
            result.LinesAffected = orders.Sum(o => o.Lines?.Count ?? 0);
            result.Success = true;

            _logger.LogInformation(
                "DRY RUN: Would reset {Orders} orders with {Lines} lines",
                result.OrdersReset,
                result.LinesAffected);

            sw.Stop();
            result.Duration = sw.Elapsed;
            return result;
        }

        var approvedOrders = await _cleanupRepository.GetApprovedOrdersAsync();

        foreach (var order in approvedOrders)
        {
            try
            {
                await ResetOrderStatusAsync(order.Id);
                await ClearKatanaOrderIdsAsync(order.Id);
                await _cleanupRepository.ClearOrderMappingsAsync(order.Id);

                result.OrdersReset++;
                result.LinesAffected += order.Lines?.Count ?? 0;
                result.MappingsCleared++;

                _logger.LogInformation("Reset order {OrderId}", order.Id);
            }
            catch (Exception ex)
            {
                result.Errors.Add(new ResetError
                {
                    OrderId = order.Id,
                    Message = "Failed to reset order",
                    Details = ex.Message
                });
                _logger.LogError(ex, "Failed to reset order {OrderId}", order.Id);
            }
        }

        sw.Stop();
        result.Duration = sw.Elapsed;
        result.Success = result.Errors.Count == 0;

        _logger.LogInformation(
            "Order reset complete: {Orders} orders, {Lines} lines, {Mappings} mappings, Duration: {Duration}",
            result.OrdersReset,
            result.LinesAffected,
            result.MappingsCleared,
            result.Duration);

        return result;
    }

    public async Task<CleanupReport> GenerateCleanupReportAsync()
    {
        _logger.LogInformation("Generating cleanup report");

        var analysis = await AnalyzeOrderProductsAsync();

        var report = new CleanupReport
        {
            Analysis = analysis,
            GeneratedAt = DateTime.UtcNow
        };

        _logger.LogInformation(
            "Report generated: {Orders} orders, {Products} products, {UniqueSkus} unique SKUs",
            analysis.TotalApprovedOrders,
            analysis.TotalProductsSentToKatana,
            analysis.UniqueSkuCount);

        return report;
    }

    public async Task<BackupResult> CreateBackupAsync()
    {
        _logger.LogInformation("Creating database backup");

        try
        {
            var backupId = Guid.NewGuid().ToString();
            var backupPath = $"backup_{backupId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.bak";

            // TODO: Implement actual backup logic
            _logger.LogWarning("Backup functionality not yet implemented");

            return new BackupResult
            {
                Success = true,
                BackupId = backupId,
                BackupPath = backupPath,
                CreatedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create backup");
            return new BackupResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<RollbackResult> RollbackAsync(string backupId)
    {
        _logger.LogInformation("Rolling back to backup {BackupId}", backupId);

        try
        {
            // TODO: Implement actual rollback logic
            _logger.LogWarning("Rollback functionality not yet implemented");

            return new RollbackResult
            {
                Success = true,
                BackupId = backupId,
                RestoredAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rollback");
            return new RollbackResult
            {
                Success = false,
                BackupId = backupId,
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task DeleteProductFromKatanaAsync(string sku)
    {
        _logger.LogDebug("Attempting to delete product {SKU} from Katana", sku);

        // First, find the product by SKU
        var product = await _katanaService.GetProductBySkuAsync(sku);
        if (product == null)
        {
            _logger.LogWarning("Product {SKU} not found in Katana", sku);
            throw new InvalidOperationException($"Product {sku} not found in Katana");
        }

        // Parse product ID to int
        if (!int.TryParse(product.Id, out int productId))
        {
            _logger.LogError("Invalid product ID format for SKU {SKU}: {Id}", sku, product.Id);
            throw new InvalidOperationException($"Invalid product ID format: {product.Id}");
        }

        // Delete the product using Katana API with retry policy
        var maxRetries = 3;
        var retryDelay = TimeSpan.FromSeconds(2);

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var success = await _katanaService.DeleteProductAsync(productId);
                if (success)
                {
                    _logger.LogInformation("Successfully deleted product {SKU} (ID: {Id}) from Katana", sku, productId);
                    return;
                }
                else
                {
                    _logger.LogWarning("Failed to delete product {SKU} (ID: {Id}) from Katana, attempt {Attempt}/{MaxRetries}", 
                        sku, productId, attempt, maxRetries);
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "HTTP error deleting product {SKU}, attempt {Attempt}/{MaxRetries}", 
                    sku, attempt, maxRetries);

                if (attempt == maxRetries)
                {
                    throw;
                }

                await Task.Delay(retryDelay * attempt);
            }
            catch (TimeoutException ex)
            {
                _logger.LogWarning(ex, "Timeout deleting product {SKU}, attempt {Attempt}/{MaxRetries}", 
                    sku, attempt, maxRetries);

                if (attempt == maxRetries)
                {
                    throw;
                }

                await Task.Delay(retryDelay * attempt);
            }
        }

        throw new InvalidOperationException($"Failed to delete product {sku} after {maxRetries} attempts");
    }

    private async Task ResetOrderStatusAsync(int orderId)
    {
        await _cleanupRepository.ResetOrderAsync(orderId);
    }

    private async Task ClearKatanaOrderIdsAsync(int orderId)
    {
        var lines = await _cleanupRepository.GetOrderLinesAsync(orderId);
        foreach (var line in lines)
        {
            line.KatanaOrderId = null;
        }
        await _cleanupRepository.SaveChangesAsync();
    }
}
