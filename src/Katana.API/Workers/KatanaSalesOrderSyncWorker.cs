using Katana.Business.Interfaces;
using Katana.Core.Interfaces;
using Katana.Data.Context;
using Katana.Data.Models;
using Katana.Core.Entities;
using Microsoft.AspNetCore.SignalR;
using Katana.API.Hubs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
            var salesOrders = await katanaService.GetSalesOrdersAsync(fromDate);

            if (salesOrders == null || salesOrders.Count == 0)
            {
                _logger.LogInformation("No sales orders found from Katana");
                return;
            }

            _logger.LogInformation("Found {Count} sales orders from Katana", salesOrders.Count);

            // Daha önce işlenmiş siparişleri al
            var processedOrderIds = await context.PendingStockAdjustments
                .Where(p => p.ExternalOrderId != null)
                .Select(p => p.ExternalOrderId)
                .Distinct()
                .ToListAsync(cancellationToken);

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

            foreach (var order in salesOrders)
            {
                // Sipariş numarası veya ID'si
                var orderId = !string.IsNullOrEmpty(order.OrderNo) ? order.OrderNo : order.Id.ToString();

                // Daha önce işlenmişse atla
                if (processedOrderIds.Contains(orderId))
                {
                    continue;
                }

                // Sadece tamamlanmamış siparişleri işle
                var status = order.Status?.ToLower() ?? "";
                if (status == "cancelled" || status == "done" || status == "shipped")
                {
                    continue;
                }

                newOrdersCount++;

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

                        // Negatif miktar (stok çıkışı) olarak kaydet
                        var pending = new PendingStockAdjustment
                        {
                            ExternalOrderId = orderId,
                            ProductId = productId,
                            Sku = sku,
                            Quantity = -Math.Abs(quantity), // Sipariş = stok çıkışı
                            RequestedBy = "Katana-Sync",
                            RequestedAt = order.CreatedAt,
                            Status = "Pending",
                            Notes = $"Katana sipariş #{orderId}: {quantity}x {sku}"
                        };

                        await pendingService.CreateAsync(pending);
                        newItemsCount++;

                        _logger.LogDebug("Created pending adjustment for order {OrderId}, SKU: {SKU}, Qty: {Qty}",
                            orderId, sku, -quantity);
                    }
                }
            }

            if (newOrdersCount > 0)
            {
                _logger.LogInformation("Synced {OrderCount} new orders with {ItemCount} items from Katana",
                    newOrdersCount, newItemsCount);

                // Yeni sipariş bildirimi oluştur
                try
                {
                    var hubContext = scope.ServiceProvider.GetService<IHubContext<NotificationHub>>();
                    
                    var notification = new Notification
                    {
                        Type = "NewSalesOrder",
                        Title = $"🛒 {newOrdersCount} Yeni Sipariş Geldi!",
                        Payload = System.Text.Json.JsonSerializer.Serialize(new { 
                            orderCount = newOrdersCount, 
                            itemCount = newItemsCount,
                            message = $"Katana'dan {newOrdersCount} yeni sipariş ({newItemsCount} ürün) alındı."
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
                            orderCount = newOrdersCount,
                            itemCount = newItemsCount,
                            createdAt = notification.CreatedAt
                        }, cancellationToken);
                    }

                    _logger.LogInformation("Created notification for {OrderCount} new orders", newOrdersCount);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to create notification for new orders");
                }
            }
            else
            {
                _logger.LogDebug("No new orders to sync from Katana");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync sales orders from Katana");
            throw;
        }
    }
}
