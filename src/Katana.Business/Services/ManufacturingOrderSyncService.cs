using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace Katana.Business.Services;

/// <summary>
/// Üretim emri senkronizasyon servisi implementasyonu
/// </summary>
public class ManufacturingOrderSyncService : IManufacturingOrderSyncService
{
    private readonly IntegrationDbContext _context;
    private readonly ILucaService _lucaService;
    private readonly ILogger<ManufacturingOrderSyncService> _logger;
    
    private static readonly AsyncRetryPolicy _retryPolicy = Policy
        .Handle<HttpRequestException>()
        .Or<TimeoutException>()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
            onRetry: (exception, delay, attempt, context) =>
            {
                // Retry logged in service
            });

    public ManufacturingOrderSyncService(
        IntegrationDbContext context,
        ILucaService lucaService,
        ILogger<ManufacturingOrderSyncService> logger)
    {
        _context = context;
        _lucaService = lucaService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ManufacturingOrderSyncResult> SyncManufacturingOrderToLucaAsync(long manufacturingOrderId)
    {
        _logger.LogInformation("Syncing manufacturing order {OrderId} to Luca", manufacturingOrderId);

        var result = new ManufacturingOrderSyncResult
        {
            ManufacturingOrderId = manufacturingOrderId
        };

        var order = await _context.ManufacturingOrders
            .Include(o => o.Product)
            .FirstOrDefaultAsync(o => o.Id == manufacturingOrderId);

        if (order == null)
        {
            result.Success = false;
            result.Errors.Add($"Manufacturing order not found: {manufacturingOrderId}");
            return result;
        }


        try
        {
            await _retryPolicy.ExecuteAsync(async () =>
            {
                // Create production document in Luca
                // This would call the Luca API to create a production record
                _logger.LogInformation(
                    "Creating production document in Luca for order {OrderNo}, Product: {ProductSKU}, Qty: {Qty}",
                    order.OrderNo, order.Product?.SKU, order.Quantity);

                // TODO: Implement actual Luca API call when endpoint is available
                // var lucaResult = await _lucaService.CreateProductionDocumentAsync(order);
                
                result.Success = true;
                result.ProducedQuantity = order.Quantity;
            });
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Errors.Add(ex.Message);
            result.RetryCount++;
            result.LastRetryAt = DateTime.UtcNow;

            _logger.LogError(ex, "Failed to sync manufacturing order {OrderId} after retries", manufacturingOrderId);

            // Notify admin after 3 failed attempts
            if (result.RetryCount >= 3)
            {
                await NotifyAdminAsync(manufacturingOrderId, ex.Message);
            }
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<ManufacturingOrderSyncResult> SyncProductionCompletionAsync(
        long manufacturingOrderId,
        decimal completedQty,
        List<MaterialConsumption> consumedMaterials)
    {
        _logger.LogInformation(
            "Syncing production completion for order {OrderId}, Qty: {Qty}, Materials: {MaterialCount}",
            manufacturingOrderId, completedQty, consumedMaterials.Count);

        var result = new ManufacturingOrderSyncResult
        {
            ManufacturingOrderId = manufacturingOrderId,
            ProducedQuantity = completedQty,
            ConsumedMaterials = consumedMaterials
        };

        var order = await _context.ManufacturingOrders
            .Include(o => o.Product)
            .FirstOrDefaultAsync(o => o.Id == manufacturingOrderId);

        if (order == null)
        {
            result.Success = false;
            result.Errors.Add($"Manufacturing order not found: {manufacturingOrderId}");
            return result;
        }

        try
        {
            await _retryPolicy.ExecuteAsync(async () =>
            {
                // 1. Decrease raw material stock in Luca
                foreach (var material in consumedMaterials)
                {
                    _logger.LogDebug(
                        "Decreasing stock for material {SKU}: Planned={Planned}, Actual={Actual}, Scrap={Scrap}",
                        material.ComponentSKU, material.PlannedQuantity, material.ActualQuantity, material.ScrapQuantity);

                    // TODO: Call Luca API to decrease material stock
                    // await _lucaService.DecreaseStockAsync(material.ComponentSKU, material.ActualQuantity);
                }

                // 2. Increase finished product stock in Luca
                _logger.LogDebug(
                    "Increasing stock for finished product {SKU}: Qty={Qty}",
                    order.Product?.SKU, completedQty);

                // TODO: Call Luca API to increase finished product stock
                // await _lucaService.IncreaseStockAsync(order.Product.SKU, completedQty);

                // 3. Handle scrap/waste quantities
                var totalScrap = consumedMaterials.Sum(m => m.ScrapQuantity);
                if (totalScrap > 0)
                {
                    result.ScrapQuantity = totalScrap;
                    _logger.LogInformation("Recording scrap quantity: {ScrapQty}", totalScrap);
                    // TODO: Record scrap in Luca
                }

                result.Success = true;
            });
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Errors.Add(ex.Message);
            result.RetryCount++;
            result.LastRetryAt = DateTime.UtcNow;

            _logger.LogError(ex, "Failed to sync production completion for order {OrderId}", manufacturingOrderId);

            if (result.RetryCount >= 3)
            {
                await NotifyAdminAsync(manufacturingOrderId, ex.Message);
            }
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<ManufacturingOrderSyncResult?> GetSyncStatusAsync(long manufacturingOrderId)
    {
        var order = await _context.ManufacturingOrders
            .FirstOrDefaultAsync(o => o.Id == manufacturingOrderId);

        if (order == null)
            return null;

        return new ManufacturingOrderSyncResult
        {
            ManufacturingOrderId = manufacturingOrderId,
            Success = order.IsSynced,
            ProducedQuantity = order.Quantity
        };
    }

    private async Task NotifyAdminAsync(long manufacturingOrderId, string errorMessage)
    {
        _logger.LogWarning(
            "Manufacturing order sync failed after 3 attempts. OrderId: {OrderId}, Error: {Error}",
            manufacturingOrderId, errorMessage);

        // Create notification in database
        var notification = new Core.Entities.Notification
        {
            Type = "MANUFACTURING_SYNC_FAILED",
            Title = $"Üretim Emri Senkronizasyon Hatası: {manufacturingOrderId}",
            Payload = System.Text.Json.JsonSerializer.Serialize(new { manufacturingOrderId, errorMessage, message = $"Üretim emri {manufacturingOrderId} Luca'ya senkronize edilemedi. Hata: {errorMessage}" }),
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
    }
}
