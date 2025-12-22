using Katana.Business.Interfaces;
using Katana.Business.Services;
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
using System.Collections.Generic;

namespace Katana.API.Workers;

/// <summary>
/// Background service that periodically syncs sales orders from Katana API
/// and creates pending stock adjustments for admin approval.
/// 
/// SYNC FLOW (runs every 5 minutes):
/// 1. Fetch ONLY open orders from Katana (status=NOT_SHIPPED)
///    - Uses GetSalesOrdersBatchedAsync with fromDate=null
///    - This ensures old orders like SO-41, SO-47 are included
/// 
/// 2. For each order (SalesOrderDto):
///    a. Customer Mapping: Resolve Katana customer ID to local database ID
///    b. If customer not found: Fetch from Katana and create locally
///    c. Create SalesOrder entity:
///       - CustomerId = local database ID (1, 2, 3...) NOT Katana ID (91190794...)
///       - Status = raw Katana status ("NOT_SHIPPED", "OPEN", etc.)
///       - All fields mapped from Katana DTO
///    d. Create SalesOrderLine entities with variant mapping
///    e. Save to database (duplicate prevention via KatanaOrderId)
/// 
/// 3. Create PendingStockAdjustment for admin approval
///    - Only for open orders (skips cancelled/shipped/delivered)
///    - Duplicate prevention via composite key (OrderId|SKU|Quantity)
/// 
/// 4. Trigger downstream syncs:
///    - Sync products to Luca (stock cards)
///    - Sync approved orders to Luca (invoices)
///    - Create notification for new orders
/// 
/// NOTE: Does NOT use KatanaApiClient.GetSalesOrdersAsync (legacy).
///       Directly uses IKatanaService.GetSalesOrdersBatchedAsync.
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
        var variantMappingService = scope.ServiceProvider.GetRequiredService<IVariantMappingService>();

        _logger.LogInformation("Starting Katana sales order sync...");

        try
        {
            // ✅ Tüm "Open" siparişleri çek (Katana UI ile aynı mantık)
            // fromDate = null → Katana API'den tüm açık siparişleri getirir
            DateTime? fromDate = null;
            
            // ✅ Mevcut SalesOrders tablosundaki Katana Order ID'lerini al (duplicate prevention)
            var existingKatanaOrderIdsList = await context.SalesOrders
                .Select(s => s.KatanaOrderId)
                .ToListAsync(cancellationToken);
            var existingKatanaOrderIds = new HashSet<long>(existingKatanaOrderIdsList);

            // ✅ OrderNo bazlı duplicate prevention için hazırla
            var existingOrderNosList = await context.SalesOrders
                .Where(s => !string.IsNullOrWhiteSpace(s.OrderNo))
                .Select(s => s.OrderNo!)
                .ToListAsync(cancellationToken);
            var existingOrderNos = new HashSet<string>(existingOrderNosList, StringComparer.OrdinalIgnoreCase);
            
            _logger.LogInformation("Found {Count} existing sales orders in database (KatanaIds)", existingKatanaOrderIds.Count);
            _logger.LogInformation("Found {Count} existing sales orders in database (OrderNos)", existingOrderNos.Count);
            
            // 🔍 DEBUG: Mevcut siparişleri logla
            if (existingKatanaOrderIds.Count > 0)
            {
                _logger.LogWarning("🔍 DEBUG: First 10 existing Katana Order IDs: {Ids}", 
                    string.Join(", ", existingKatanaOrderIds.Take(10)));
            }
            
            // ✅ Composite key kontrolü - Sipariş güncellemelerini yakala
            // ExternalOrderId + SKU ile duplicate prevention (qty değişse de aynı satır ikinci kez açılmasın)
            var processedItems = await context.PendingStockAdjustments
                .Where(p => p.ExternalOrderId != null)
                .Select(p => new 
                { 
                    p.ExternalOrderId, 
                    p.Sku
                })
                .ToListAsync(cancellationToken);
            
            // HashSet ile O(1) lookup performance
            var processedItemsSet = new HashSet<string>(
                processedItems.Select(p => $"{p.ExternalOrderId}|{(p.Sku ?? string.Empty).Trim().ToUpperInvariant()}")
            );
            
            _logger.LogInformation("Found {Count} already processed order items", processedItemsSet.Count);

            // Ürün listesini al (variant ID -> SKU mapping için)
            var products = await katanaService.GetProductsAsync();
            var skuToProductId = await context.Products
                .Where(p => !string.IsNullOrWhiteSpace(p.SKU))
                .ToDictionaryAsync(p => p.SKU!, p => p.Id, StringComparer.OrdinalIgnoreCase);

            var variantToProduct = new Dictionary<long, (int ProductId, string Sku, string? ProductName)>();
            foreach (var p in products)
            {
                if (long.TryParse(p.Id, out var variantId))
                {
                    var sku = p.SKU ?? p.Id;
                    var productId = skuToProductId.TryGetValue(sku, out var localId) ? localId : 0;
                    variantToProduct[variantId] = (productId, sku, p.Name);
                }
            }

            var variantMappingCache = new Dictionary<long, VariantMapping?>();
            
            // ✅ Müşteri mapping'i için Katana customer ID -> local Customer ID
            var customerMapping = await context.Customers
                .Where(c => c.ReferenceId != null)
                .ToDictionaryAsync(c => c.ReferenceId!, c => c.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);

            // 🔥 Tüm Katana müşterilerini önceden çek ve cache'le
            _logger.LogInformation("Fetching all customers from Katana for caching...");
            var allKatanaCustomers = await katanaService.GetCustomersAsync();
            
            // ✅ FIX: Dictionary key'i long yap (string yerine) - direct comparison için
            var katanaCustomerCache = allKatanaCustomers.ToDictionary(
                c => c.Id,  // long key - NO ToString()!
                c => c
            );
            _logger.LogInformation("Cached {Count} customers from Katana", katanaCustomerCache.Count);
            
            // 🔍 DEBUG: Cache içeriğini logla
            _logger.LogWarning("🔍 DEBUG: Customer Cache Contents (first 5):");
            foreach (var kvp in katanaCustomerCache.Take(5))
            {
                _logger.LogWarning("  Cache Key: {Key} (Type: {Type}) → Customer ID: {Id}, Name: '{Name}'",
                    kvp.Key, kvp.Key.GetType().Name, kvp.Value.Id, kvp.Value.Name);
            }

            var newOrdersCount = 0;
            var newItemsCount = 0;
            var skippedItemsCount = 0;
            var savedSalesOrdersCount = 0;

            // ✅ Memory-efficient batched processing (1000+ orders)
            // Her batch ayrı işlensin, GC çalışabilsin
            // fromDate = null → Tüm "Open" siparişleri çek (Katana UI ile aynı mantık)
            await foreach (var orderBatch in katanaService.GetSalesOrdersBatchedAsync(fromDate, batchSize: 100))
            {
                _logger.LogInformation("Processing batch of {Count} orders", orderBatch.Count);

                foreach (var order in orderBatch)
                {
                    // Sipariş numarası veya ID'si
                    var orderNo = !string.IsNullOrWhiteSpace(order.OrderNo) ? order.OrderNo.Trim() : $"SO-{order.Id}";
                    var orderId = orderNo;

                    // Sadece tamamlanmamış siparişleri işle (PendingStockAdjustment için)
                    // NOT_SHIPPED/OPEN siparişler için pending adjustment oluştur
                    // CANCELLED, DONE, SHIPPED, DELIVERED siparişler için pending adjustment oluşturma
                    var status = order.Status?.ToLower() ?? "";
                    var skipPendingAdjustment = status == "cancelled" || status == "done" || status == "shipped" || status == "delivered" || status == "fully_shipped";

                    // ✅ SalesOrders tablosuna kaydet (tüm siparişler için - duplicate check ile)
                    var isDuplicateByKatanaId = existingKatanaOrderIds.Contains(order.Id);
                    var isDuplicateByOrderNo = existingOrderNos.Contains(orderNo);

                    if (isDuplicateByKatanaId || isDuplicateByOrderNo)
                    {
                        _logger.LogWarning("Duplicate order detected, skipping. OrderNo={OrderNo}, KatanaId={KatanaId}, duplicateByKatanaId={ById}, duplicateByOrderNo={ByNo}",
                            orderNo, order.Id, isDuplicateByKatanaId, isDuplicateByOrderNo);
                        continue;
                    }

                    if (!existingKatanaOrderIds.Contains(order.Id))
                    {
                        var localCustomerId = 0;
                        var katanaCustomerIdStr = order.CustomerId.ToString();
                        if (customerMapping.TryGetValue(katanaCustomerIdStr, out var mappedCustomerId))
                        {
                            localCustomerId = mappedCustomerId;
                        }
                        
                        // Müşteri bulunamadıysa Katana'dan çekip oluştur
                        if (localCustomerId == 0)
                        {
                            // 🔍 DEBUG: Müşteri arama detayları
                            _logger.LogWarning("🔍 DEBUG: Looking for customer - Order.CustomerId={OrderCustomerId} (Type: {Type}), " +
                                "String Key='{StringKey}'",
                                order.CustomerId,
                                order.CustomerId.GetType().Name,
                                katanaCustomerIdStr);
                            
                            KatanaCustomerDto? katanaCustomer = null;
                            // ✅ FIX: long key ile direkt arama (string yerine)
                            if (katanaCustomerCache.TryGetValue(order.CustomerId, out var cachedCustomer))
                            {
                                katanaCustomer = cachedCustomer;
                                _logger.LogDebug("✅ Found customer in cache: {CustomerId}", order.CustomerId);
                            }
                            else
                            {
                                _logger.LogWarning("❌ Customer NOT FOUND in cache! Key: {Key}, Cache Keys Sample: {Sample}",
                                    order.CustomerId,
                                    string.Join(", ", katanaCustomerCache.Keys.Take(3)));
                            }
                            
                            if (katanaCustomer != null)
                            {
                                // Adres bilgilerini Addresses listesinden al
                                var defaultAddress = katanaCustomer.Addresses?.FirstOrDefault();
                                
                                var newCustomer = new Customer
                                {
                                    Title = katanaCustomer.Name ?? $"Customer-{order.CustomerId}",
                                    ReferenceId = katanaCustomerIdStr,
                                    Email = katanaCustomer.Email,
                                    Phone = katanaCustomer.Phone,
                                    Address = defaultAddress?.Line1,
                                    City = defaultAddress?.City,
                                    Country = defaultAddress?.Country,
                                    TaxNo = GetMax11SafeTaxNo(order.CustomerId),
                                    Currency = katanaCustomer.Currency ?? "TRY",
                                    IsActive = true,
                                    CreatedAt = DateTime.UtcNow
                                };
                                context.Customers.Add(newCustomer);
                                await context.SaveChangesAsync(cancellationToken);
                                
                                localCustomerId = newCustomer.Id;
                                customerMapping[katanaCustomerIdStr] = localCustomerId;
                                _logger.LogInformation("✅ Yeni müşteri oluşturuldu: {CustomerName} (ID: {CustomerId})", newCustomer.Title, newCustomer.Id);
                            }
                            else
                            {
                                // Müşteri Katana'da bulunamadı - "Unknown Customer" olarak oluştur
                                _logger.LogWarning("⚠️ Müşteri Katana'da bulunamadı (CustomerId: {CustomerId}), 'Unknown Customer' olarak oluşturuluyor", order.CustomerId);
                                
                                var unknownCustomer = new Customer
                                {
                                    Title = $"Unknown Customer (Katana ID: {order.CustomerId})",
                                    ReferenceId = katanaCustomerIdStr,
                                    Email = null,
                                    Phone = null,
                                    TaxNo = GetMax11SafeTaxNo(order.CustomerId),
                                    Currency = order.Currency ?? "TRY",
                                    IsActive = false, // Inactive olarak işaretle
                                    CreatedAt = DateTime.UtcNow
                                };
                                context.Customers.Add(unknownCustomer);
                                await context.SaveChangesAsync(cancellationToken);
                                
                                localCustomerId = unknownCustomer.Id;
                                customerMapping[katanaCustomerIdStr] = localCustomerId;
                                _logger.LogInformation("✅ Unknown customer oluşturuldu: {CustomerName} (ID: {CustomerId})", unknownCustomer.Title, unknownCustomer.Id);
                            }
                        }
                        
                        var salesOrder = new SalesOrder
                        {
                            KatanaOrderId = order.Id,
                            OrderNo = order.OrderNo ?? $"SO-{order.Id}",
                            CustomerId = localCustomerId,
                            OrderCreatedDate = order.OrderCreatedDate ?? order.CreatedAt,
                            DeliveryDate = order.DeliveryDate,
                            Currency = order.Currency ?? "TRY",
                            ConversionRate = order.ConversionRate,
                            Status = order.Status ?? "NOT_SHIPPED",
                            Total = order.Total,
                            TotalInBaseCurrency = order.TotalInBaseCurrency,
                            AdditionalInfo = order.AdditionalInfo,
                            CustomerRef = order.CustomerRef,
                            Source = order.Source,
                            LocationId = order.LocationId,
                            CreatedAt = DateTime.UtcNow,
                            IsSyncedToLuca = false
                        };
                        
                        // Sipariş satırlarını ekle
                        if (order.SalesOrderRows != null && order.SalesOrderRows.Count > 0)
                        {
                            foreach (var row in order.SalesOrderRows)
                            {
                                var (resolvedProductId, resolvedSku) = await ResolveVariantMappingAsync(
                                    row.VariantId,
                                    variantToProduct.ToDictionary(x => x.Key, x => (x.Value.ProductId, x.Value.Sku)),
                                    variantMappingCache,
                                    variantMappingService);
                                
                                var productName = variantToProduct.TryGetValue(row.VariantId, out var pInfo) 
                                    ? pInfo.ProductName 
                                    : null;
                                
                                var orderLine = new SalesOrderLine
                                {
                                    KatanaRowId = row.Id,
                                    VariantId = row.VariantId,
                                    SKU = resolvedSku,
                                    ProductName = productName,
                                    Quantity = row.Quantity,
                                    PricePerUnit = row.PricePerUnit,
                                    PricePerUnitInBaseCurrency = row.PricePerUnitInBaseCurrency,
                                    Total = row.Total,
                                    TotalInBaseCurrency = row.TotalInBaseCurrency,
                                    TaxRate = null, // TaxRateId'den hesaplanabilir
                                    TaxRateId = row.TaxRateId,
                                    LocationId = row.LocationId,
                                    ProductAvailability = row.ProductAvailability,
                                    ProductExpectedDate = row.ProductExpectedDate,
                                    CreatedAt = DateTime.UtcNow
                                };
                                
                                salesOrder.Lines.Add(orderLine);
                            }
                        }
                        
                        _logger.LogInformation("INSERT_SALES_ORDER OrderNo={OrderNo}, KatanaId={KatanaId}, LineCount={LineCount}", orderNo, order.Id, salesOrder.Lines.Count);

                        context.SalesOrders.Add(salesOrder);
                        existingKatanaOrderIds.Add(order.Id); // Duplicate prevention için ekle
                        existingOrderNos.Add(orderNo);
                        savedSalesOrdersCount++;
                        
                        // 📊 Debug: Status mapping kontrolü
                        _logger.LogDebug("📊 Order {OrderNo}: Katana Status='{KatanaStatus}' → Stored Status='{StoredStatus}'",
                            salesOrder.OrderNo, order.Status, salesOrder.Status);
                        
                        _logger.LogDebug("Saved sales order to database: {OrderNo} (KatanaId: {KatanaId})", 
                            salesOrder.OrderNo, order.Id);
                    }

                    // PendingStockAdjustment için - sadece aktif siparişler
                    if (skipPendingAdjustment)
                    {
                        continue;
                    }

                    var orderHasNewItems = false;

                    // Sipariş kalemlerini işle (SalesOrderRows)
                    if (order.SalesOrderRows != null && order.SalesOrderRows.Count > 0)
                    {
                        foreach (var row in order.SalesOrderRows)
                        {
                            var (resolvedProductId, resolvedSku) = await ResolveVariantMappingAsync(
                                row.VariantId,
                                variantToProduct.ToDictionary(x => x.Key, x => (x.Value.ProductId, x.Value.Sku)),
                                variantMappingCache,
                                variantMappingService);

                            string sku = resolvedSku;
                            int productId = resolvedProductId;

                            var quantity = (int)row.Quantity;
                            
                            // ✅ Composite key ile duplicate check (ExternalOrderId + SKU)
                            var itemKey = $"{orderId}|{sku}";
                            if (processedItemsSet.Contains(itemKey))
                            {
                                _logger.LogDebug("Skipping already processed item: Order {OrderId}, SKU: {SKU}, Qty: {Qty}",
                                    orderId, sku, quantity);
                                skippedItemsCount++;
                                continue;
                            }

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
                            orderHasNewItems = true;

                            _logger.LogDebug("Created pending adjustment for order {OrderId}, SKU: {SKU}, Qty: {Qty}",
                                orderId, sku, quantity);
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
            
            if (savedSalesOrdersCount > 0)
            {
                _logger.LogInformation("✅ Saved {Count} new sales orders to database", savedSalesOrdersCount);
            }

            if (newOrdersCount > 0)
            {
                _logger.LogInformation(
                    "Synced {OrderCount} new orders with {ItemCount} items from Katana ({SkippedItems} duplicate items skipped)",
                    newOrdersCount, newItemsCount, skippedItemsCount);

                // 1. Luca'ya stok kartı senkronizasyonu (yeni siparişler geldiyse)
                await SyncProductsToLucaWithRetryAsync(scope);

                // 2. Yeni sipariş bildirimi oluştur
                await CreateNewOrderNotificationAsync(scope, newOrdersCount, newItemsCount, cancellationToken);
            }
            else
            {
                _logger.LogInformation("No new sales orders to process");
            }

            // 3. Onaylanan siparişleri Luca'ya fatura olarak gönder (yeni sipariş olmasa da çalışmalı)
            await SyncApprovedOrdersToLucaWithRetryAsync(scope, cancellationToken);
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
            var orderInvoiceSync = scope.ServiceProvider.GetService<IOrderInvoiceSyncService>();
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

            foreach (var adjustment in approvedAdjustments)
            {
                try
                {
                    var externalOrderId = adjustment.ExternalOrderId?.Trim();
                    if (string.IsNullOrWhiteSpace(externalOrderId))
                    {
                        _logger.LogWarning("Cannot sync order - ExternalOrderId is empty. PendingAdjustmentId={Id}", adjustment.Id);
                        continue;
                    }

                    var localOrderId = await ResolveLocalSalesOrderIdAsync(context, externalOrderId, cancellationToken);
                    if (!localOrderId.HasValue)
                    {
                        _logger.LogWarning("Cannot sync order {ExternalOrderId} - local SalesOrder not found", externalOrderId);
                        continue;
                    }

                    await orderInvoiceSync.SyncSalesOrderToLucaAsync(localOrderId.Value);
                    _logger.LogInformation("Successfully synced order {ExternalOrderId} (LocalId={LocalId}) to Luca", externalOrderId, localOrderId.Value);
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

    private static async Task<int?> ResolveLocalSalesOrderIdAsync(IntegrationDbContext context, string externalOrderId, CancellationToken ct)
    {
        if (int.TryParse(externalOrderId, out var localId))
        {
            var exists = await context.SalesOrders.AsNoTracking().AnyAsync(o => o.Id == localId, ct);
            return exists ? localId : null;
        }

        if (long.TryParse(externalOrderId, out var katanaOrderId))
        {
            var byKatanaId = await context.SalesOrders.AsNoTracking()
                .Where(o => o.KatanaOrderId == katanaOrderId)
                .Select(o => (int?)o.Id)
                .FirstOrDefaultAsync(ct);
            if (byKatanaId.HasValue) return byKatanaId.Value;
        }

        var byOrderNo = await context.SalesOrders.AsNoTracking()
            .Where(o => o.OrderNo == externalOrderId)
            .Select(o => (int?)o.Id)
            .FirstOrDefaultAsync(ct);
        if (byOrderNo.HasValue) return byOrderNo.Value;

        // Backward-compat: geçmişte yanlışlıkla "SO-" prefix'i çiftlenen siparişlerde arama kolaylığı
        if (externalOrderId.StartsWith("SO-SO-", StringComparison.OrdinalIgnoreCase))
        {
            var normalized = "SO-" + externalOrderId.Substring("SO-SO-".Length);
            var byNormalized = await context.SalesOrders.AsNoTracking()
                .Where(o => o.OrderNo == normalized)
                .Select(o => (int?)o.Id)
                .FirstOrDefaultAsync(ct);
            if (byNormalized.HasValue) return byNormalized.Value;
        }

        return null;
    }

    private static async Task<(int ProductId, string Sku)> ResolveVariantMappingAsync(
        long variantId,
        IDictionary<long, (int ProductId, string Sku)> fallback,
        Dictionary<long, VariantMapping?> cache,
        IVariantMappingService variantMappingService)
    {
        if (!cache.TryGetValue(variantId, out var cached))
        {
            cached = await variantMappingService.GetMappingAsync(variantId);
            cache[variantId] = cached;
        }

        if (cached != null)
        {
            return (cached.ProductId, cached.Sku);
        }

        if (fallback.TryGetValue(variantId, out var fallbackValue))
        {
            var created = await variantMappingService.CreateOrUpdateAsync(variantId, fallbackValue.ProductId, fallbackValue.Sku);
            cache[variantId] = created;
            return (created.ProductId, created.Sku);
        }

        return (0, $"VARIANT-{variantId}");
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

    private static string GetMax11SafeTaxNo(long customerId)
    {
        var id = customerId.ToString();
        if (id.Length > 10) id = id.Substring(id.Length - 10);
        return $"U{id}";
    }
}
