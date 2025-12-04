using Katana.Business.Interfaces;
using Katana.Business.UseCases.Sync;
using Katana.Core.Interfaces;
using Katana.Core.DTOs;
using Katana.Data.Context;
using Katana.Data.Models;
using Katana.Core.Entities;
using Microsoft.AspNetCore.SignalR;
using Katana.API.Hubs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace Katana.API.Workers;

/// <summary>
/// Background service that periodically syncs sales orders from Katana API
/// and creates pending stock adjustments for admin approval.
/// </summary>
public class KatanaSalesOrderSyncWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<KatanaSalesOrderSyncWorker> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5); // Her 5 dakikada bir kontrol et

    // Retry policy - Katana API çağrıları için
    private static readonly AsyncRetryPolicy _katanaApiRetryPolicy = Policy
        .Handle<HttpRequestException>()
        .Or<TimeoutException>()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
            onRetry: (exception, delay, attempt, context) =>
            {
                if (context.TryGetValue("logger", out var loggerObj) && loggerObj is ILogger logger)
                {
                    logger.LogWarning(exception,
                        "Katana API retry attempt {Attempt}/3 after {Delay}s",
                        attempt, delay.TotalSeconds);
                }
            });

    public KatanaSalesOrderSyncWorker(IServiceProvider services, ILogger<KatanaSalesOrderSyncWorker> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("KatanaSalesOrderSyncWorker started");

        // İlk çalıştırmada biraz bekle (uygulama tamamen başlayana kadar)
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncSalesOrdersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing sales orders from Katana");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("KatanaSalesOrderSyncWorker stopped");
    }

    private async Task SyncSalesOrdersAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var katanaService = scope.ServiceProvider.GetRequiredService<IKatanaService>();
        var pendingService = scope.ServiceProvider.GetRequiredService<IPendingStockAdjustmentService>();
        var context = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();

        _logger.LogInformation("Starting Katana sales order sync...");

        try
        {
            // Son 7 gündeki siparişleri çek
            var fromDate = DateTime.UtcNow.AddDays(-7);
            
            // ✅ Composite key kontrolü - Sipariş güncellemelerini yakala
            // ExternalOrderId + ProductId + Quantity ile duplicate prevention
            var processedItems = await context.PendingStockAdjustments
                .Where(p => p.ExternalOrderId != null)
                .Select(p => new 
                { 
                    p.ExternalOrderId, 
                    p.Sku,
                    p.Quantity 
                })
                .ToListAsync(cancellationToken);
            
            // HashSet ile O(1) lookup performance
            var processedItemsSet = new HashSet<string>(
                processedItems.Select(p => $"{p.ExternalOrderId}|{p.Sku}|{p.Quantity}")
            );
            
            _logger.LogInformation("Found {Count} already processed order items", processedItemsSet.Count);

            // Ürün listesini al (variant ID -> SKU mapping için)
            var products = await katanaService.GetProductsAsync();
            var variantToProduct = new Dictionary<long, (int ProductId, string Sku)>();
            foreach (var p in products)
            {
                // Katana'da variant_id genellikle product id ile ilişkili
                // Bu mapping'i daha detaylı yapmak gerekebilir
                if (long.TryParse(p.Id, out var variantId))
                {
                    variantToProduct[variantId] = (0, p.SKU ?? p.Id);
                }
            }

            var newOrdersCount = 0;
            var newItemsCount = 0;
            var skippedItemsCount = 0;

            // ✅ Memory-efficient batched processing (1000+ orders)
            // Her batch ayrı işlensin, GC çalışabilsin
            await foreach (var orderBatch in katanaService.GetSalesOrdersBatchedAsync(fromDate, batchSize: 100))
            {
                _logger.LogInformation("Processing batch of {Count} orders", orderBatch.Count);

                foreach (var order in orderBatch)
                {
                    // Sipariş numarası veya ID'si
                    var orderId = !string.IsNullOrEmpty(order.OrderNo) ? order.OrderNo : order.Id.ToString();

                    // Sadece tamamlanmamış siparişleri işle
                    var status = order.Status?.ToLower() ?? "";
                    if (status == "cancelled" || status == "done" || status == "shipped")
                    {
                        continue;
                    }

                    var orderHasNewItems = false;

                    // Sipariş kalemlerini işle (SalesOrderRows)
                    if (order.SalesOrderRows != null && order.SalesOrderRows.Count > 0)
                    {
                        foreach (var row in order.SalesOrderRows)
                        {
                            // Variant ID'den ürün bilgisi al
                            string sku;
                            int productId = 0;
                            
                            if (variantToProduct.TryGetValue(row.VariantId, out var productInfo))
                            {
                                sku = productInfo.Sku;
                                productId = productInfo.ProductId;
                            }
                            else
                            {
                                sku = $"VARIANT-{row.VariantId}";
                            }

                            var quantity = (int)row.Quantity;
                            var negativeQuantity = -Math.Abs(quantity);
                            
                            // ✅ Composite key ile duplicate check
                            var itemKey = $"{orderId}|{sku}|{negativeQuantity}";
                            if (processedItemsSet.Contains(itemKey))
                            {
                                _logger.LogDebug("Skipping already processed item: Order {OrderId}, SKU: {SKU}, Qty: {Qty}",
                                    orderId, sku, negativeQuantity);
                                skippedItemsCount++;
                                continue;
                            }

                            // Negatif miktar (stok çıkışı) olarak kaydet
                            var pending = new PendingStockAdjustment
                            {
                                ExternalOrderId = orderId,
                                ProductId = productId,
                                Sku = sku,
                                Quantity = negativeQuantity, // Sipariş = stok çıkışı
                                RequestedBy = "Katana-Sync",
                                RequestedAt = order.CreatedAt,
                                Status = "Pending",
                                Notes = $"Katana sipariş #{orderId}: {quantity}x {sku}"
                            };

                            await pendingService.CreateAsync(pending);
                            newItemsCount++;
                            orderHasNewItems = true;

                            _logger.LogDebug("Created pending adjustment for order {OrderId}, SKU: {SKU}, Qty: {Qty}",
                                orderId, sku, negativeQuantity);
                        }
                    }
                    
                    if (orderHasNewItems)
                    {
                        newOrdersCount++;
                    }
                }

                // Batch işlendikten sonra SaveChanges
                await context.SaveChangesAsync(cancellationToken);
                
                // GC'yi tetikle (memory leak önleme)
                GC.Collect(0, GCCollectionMode.Optimized);
            }

            if (newOrdersCount > 0)
            {
                _logger.LogInformation(
                    "Synced {OrderCount} new orders with {ItemCount} items from Katana ({SkippedItems} duplicate items skipped)",
                    newOrdersCount, newItemsCount, skippedItemsCount);

                // 1. Luca'ya stok kartı senkronizasyonu
                await SyncProductsToLucaWithRetryAsync(scope);

                // 2. Onaylanan siparişleri Luca'ya fatura olarak gönder
                await SyncApprovedOrdersToLucaWithRetryAsync(scope, cancellationToken);

                // 3. Yeni sipariş bildirimi oluştur
                await CreateNewOrderNotificationAsync(scope, newOrdersCount, newItemsCount, cancellationToken);
            }
            else
            {
                _logger.LogInformation("No new sales orders to process");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Katana sales order sync");
            throw;
        }
    }

    private async Task SyncProductsToLucaWithRetryAsync(IServiceScope scope)
    {
        try
        {
            var syncService = scope.ServiceProvider.GetService<ISyncService>();
            if (syncService != null)
            {
                _logger.LogInformation("Triggering Luca product sync for new orders...");
                
                var context = new Context("SyncProductsToLuca");
                context["logger"] = _logger;

                var syncResult = await _katanaApiRetryPolicy.ExecuteAsync(async (ctx) =>
                {
                    return await syncService.SyncProductsToLucaAsync(new SyncOptionsDto
                    {
                        DryRun = false,
                        ForceSendDuplicates = false,
                        PreferBarcodeMatch = true
                    });
                }, context);

                if (syncResult.IsSuccess)
                {
                    _logger.LogInformation("Luca product sync completed. New cards: {New}, Sent: {Sent}",
                        syncResult.NewCreated, syncResult.SentRecords);
                }
                else
                {
                    _logger.LogWarning("Luca product sync completed with issues: {Message}", syncResult.Message);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to trigger Luca product sync - will retry next cycle");
        }
    }

    private async Task SyncApprovedOrdersToLucaWithRetryAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        try
        {
            var orderInvoiceSync = scope.ServiceProvider.GetService<OrderInvoiceSyncService>();
            var context = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();

            if (orderInvoiceSync == null)
            {
                _logger.LogWarning("OrderInvoiceSyncService not available - skipping invoice sync");
                return;
            }

            // Onaylanan ama Luca'ya gönderilmemiş siparişleri bul
            var approvedAdjustments = await context.PendingStockAdjustments
                .Where(p => p.Status == "Approved" && p.ExternalOrderId != null)
                .GroupBy(p => p.ExternalOrderId)
                .Select(g => g.First())
                .ToListAsync(cancellationToken);

            if (!approvedAdjustments.Any())
            {
                _logger.LogInformation("No approved orders to sync to Luca");
                return;
            }

            _logger.LogInformation("Found {Count} approved orders to sync to Luca", approvedAdjustments.Count);

            var retryContext = new Context("SyncOrderToLuca");
            retryContext["logger"] = _logger;

            foreach (var adjustment in approvedAdjustments)
            {
                try
                {
                    // OrderInvoiceSyncService orderId (int) bekliyor
                    // ExternalOrderId string olduğu için parse etmemiz gerekiyor
                    if (int.TryParse(adjustment.ExternalOrderId, out var orderId))
                    {
                        await orderInvoiceSync.SyncSalesOrderToLucaAsync(orderId);
                        _logger.LogInformation("Successfully synced order {OrderId} to Luca", adjustment.ExternalOrderId);
                    }
                    else
                    {
                        _logger.LogWarning("Cannot sync order {OrderId} - invalid order ID format", 
                            adjustment.ExternalOrderId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to sync order {OrderId} to Luca - will retry next cycle", 
                        adjustment.ExternalOrderId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to process approved orders for Luca sync");
        }
    }

    private async Task CreateNewOrderNotificationAsync(IServiceScope scope, int orderCount, int itemCount, 
        CancellationToken cancellationToken)
    {
        try
        {
            var hubContext = scope.ServiceProvider.GetService<IHubContext<NotificationHub>>();
            var context = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();
            
            var notification = new Notification
            {
                Type = "NewSalesOrder",
                Title = $"🛒 {orderCount} Yeni Sipariş Geldi!",
                Payload = System.Text.Json.JsonSerializer.Serialize(new { 
                    orderCount, 
                    itemCount,
                    message = $"Katana'dan {orderCount} yeni sipariş ({itemCount} ürün) alındı."
                }),
                Link = "/admin",
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };
            context.Notifications.Add(notification);
            await context.SaveChangesAsync(cancellationToken);

            // SignalR ile gerçek zamanlı bildirim gönder
            if (hubContext != null)
            {
                await hubContext.Clients.All.SendAsync("NewSalesOrder", new
                {
                    id = notification.Id,
                    title = notification.Title,
                    type = notification.Type,
                    orderCount,
                    itemCount,
                    createdAt = notification.CreatedAt
                }, cancellationToken);
            }

            _logger.LogInformation("Created notification for {OrderCount} new orders", orderCount);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create notification for new orders");
        }
    }
}
