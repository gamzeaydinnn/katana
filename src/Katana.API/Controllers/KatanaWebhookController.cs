using Microsoft.AspNetCore.Mvc;
using Katana.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Katana.Data.Models;
using Katana.Data.Context;
using Katana.Core.Entities;
using Katana.Core.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace Katana.API.Controllers;

/// <summary>
/// Katana webhook endpoint'leri - Stok değişiklikleri ve sipariş bildirimleri için
/// </summary>
[ApiController]
[Route("api/webhook/katana")]
[AllowAnonymous] 
public class KatanaWebhookController : ControllerBase
{
    private readonly IPendingStockAdjustmentService _pendingService;
    private readonly ILogger<KatanaWebhookController> _logger;
    private readonly IConfiguration _configuration;
    private readonly IntegrationDbContext _context;
    private readonly IKatanaService _katanaService;
    private readonly IVariantMappingService _variantMappingService;

    public KatanaWebhookController(
        IPendingStockAdjustmentService pendingService,
        ILogger<KatanaWebhookController> logger,
        IConfiguration configuration,
        IntegrationDbContext context,
        IKatanaService katanaService,
        IVariantMappingService variantMappingService)
    {
        _pendingService = pendingService;
        _logger = logger;
        _configuration = configuration;
        _context = context;
        _katanaService = katanaService;
        _variantMappingService = variantMappingService;
    }

    
    
    
    
    
    
    
    
    
    
    
    
    
    
    [HttpPost("stock-change")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReceiveStockChange([FromBody] KatanaStockChangeWebhook webhook)
    {
        
        var expectedApiKey = _configuration["KatanaApi:WebhookSecret"];
        var receivedApiKey = Request.Headers["X-Katana-Signature"].FirstOrDefault();

        if (string.IsNullOrEmpty(expectedApiKey) || !KatanaWebhookSecurity.SecureEquals(receivedApiKey, expectedApiKey))
        {
            _logger.LogWarning("Unauthorized webhook attempt from IP: {IP}", HttpContext.Connection.RemoteIpAddress);
            return Unauthorized(new { error = "Invalid webhook signature" });
        }

        try
        {
            _logger.LogInformation(
                "Received Katana webhook: Event={Event}, OrderId={OrderId}, SKU={SKU}, Quantity={Qty}",
                webhook.Event,
                webhook.OrderId,
                webhook.Sku,
                webhook.QuantityChange
            );

            
            var pendingAdjustment = new PendingStockAdjustment
            {
                ExternalOrderId = webhook.OrderId,
                ProductId = webhook.ProductId,
                Sku = webhook.Sku ?? $"PRODUCT-{webhook.ProductId}",
                Quantity = webhook.QuantityChange,
                RequestedBy = "Katana-API", 
                RequestedAt = webhook.Timestamp != default ? webhook.Timestamp : DateTimeOffset.UtcNow,
                Status = "Pending",
                Notes = $"Katana webhook: {webhook.Event}"
            };

            var created = await _pendingService.CreateAsync(pendingAdjustment);

            _logger.LogInformation(
                "Created pending adjustment #{PendingId} from Katana webhook (OrderId: {OrderId})",
                created.Id,
                webhook.OrderId
            );

            return Ok(new
            {
                success = true,
                pendingId = created.Id,
                message = "Stok değişikliği admin onayına gönderildi"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Katana webhook");
            return BadRequest(new { error = "Webhook processing failed", details = ex.Message });
        }
    }

    
    
    
    [HttpPost("test")]
    [ApiExplorerSettings(IgnoreApi = false)]
    public async Task<IActionResult> TestWebhook()
    {
        var testWebhook = new KatanaStockChangeWebhook
        {
            Event = "stock.updated",
            OrderId = $"TEST-{DateTime.UtcNow:yyyyMMddHHmmss}",
            ProductId = 999,
            Sku = "TEST-SKU-001",
            QuantityChange = -3,
            Timestamp = DateTime.UtcNow
        };

        
        Request.Headers["X-Katana-Signature"] = _configuration["KatanaApi:WebhookSecret"] ?? "test-secret";

        return await ReceiveStockChange(testWebhook);
    }

    /// <summary>
    /// Katana'dan sipariş webhook'u al - Yeni sipariş veya sipariş güncellemesi
    /// POST /api/webhook/katana/sales-order
    /// </summary>
    /// <remarks>
    /// Katana'dan gelen sipariş event'leri:
    /// - sales_order.created: Yeni sipariş oluşturuldu
    /// - sales_order.updated: Sipariş güncellendi
    /// - sales_order.status_changed: Sipariş durumu değişti
    /// - sales_order.cancelled: Sipariş iptal edildi
    /// 
    /// KatanaOrderId doğru şekilde kaydedilir ve duplicate kontrolü yapılır.
    /// </remarks>
    [HttpPost("sales-order")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReceiveSalesOrder([FromBody] KatanaSalesOrderWebhook webhook)
    {
        // Webhook signature doğrulama
        var expectedApiKey = _configuration["KatanaApi:WebhookSecret"];
        var receivedApiKey = Request.Headers["X-Katana-Signature"].FirstOrDefault();

        if (string.IsNullOrEmpty(expectedApiKey) || !KatanaWebhookSecurity.SecureEquals(receivedApiKey, expectedApiKey))
        {
            _logger.LogWarning("Unauthorized sales order webhook attempt from IP: {IP}", HttpContext.Connection.RemoteIpAddress);
            return Unauthorized(new { error = "Invalid webhook signature" });
        }

        try
        {
            _logger.LogInformation(
                "📥 Received Katana sales order webhook: Event={Event}, KatanaOrderId={KatanaOrderId}, OrderNo={OrderNo}, Status={Status}",
                webhook.Event,
                webhook.KatanaOrderId,
                webhook.OrderNo,
                webhook.Status
            );

            // Event tipine göre işlem
            var result = webhook.Event?.ToLowerInvariant() switch
            {
                "sales_order.created" => await HandleSalesOrderCreatedAsync(webhook),
                "sales_order.updated" => await HandleSalesOrderUpdatedAsync(webhook),
                "sales_order.status_changed" => await HandleSalesOrderStatusChangedAsync(webhook),
                "sales_order.cancelled" => await HandleSalesOrderCancelledAsync(webhook),
                _ => await HandleSalesOrderCreatedAsync(webhook) // Default: create/update
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Katana sales order webhook. KatanaOrderId={KatanaOrderId}", webhook.KatanaOrderId);
            return BadRequest(new { error = "Webhook processing failed", details = ex.Message });
        }
    }

    private async Task<object> HandleSalesOrderCreatedAsync(KatanaSalesOrderWebhook webhook)
    {
        // ✅ UPSERT: Mevcut sipariş kontrolü - varsa güncelle, yoksa oluştur
        var existingOrder = await _context.SalesOrders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.KatanaOrderId == webhook.KatanaOrderId);

        if (existingOrder != null)
        {
            // ✅ Onaylanmış siparişleri güncelleme (koruma)
            if (existingOrder.Status?.ToUpperInvariant() == "APPROVED" || 
                existingOrder.Status?.ToUpperInvariant() == "SHIPPED")
            {
                _logger.LogWarning("⚠️ Skipping update for approved/shipped order. KatanaOrderId={KatanaOrderId}, LocalId={LocalId}, Status={Status}",
                    webhook.KatanaOrderId, existingOrder.Id, existingOrder.Status);
                return new
                {
                    success = true,
                    action = "skipped",
                    message = $"Sipariş durumu ({existingOrder.Status}) güncellemeye izin vermiyor",
                    localOrderId = existingOrder.Id,
                    katanaOrderId = webhook.KatanaOrderId
                };
            }

            // ✅ UPSERT: Mevcut siparişi güncelle
            _logger.LogInformation("UPSERT: Updating existing order. KatanaOrderId={KatanaOrderId}, LocalId={LocalId}", 
                webhook.KatanaOrderId, existingOrder.Id);

            existingOrder.OrderNo = webhook.OrderNo ?? existingOrder.OrderNo;
            existingOrder.DeliveryDate = webhook.DeliveryDate ?? existingOrder.DeliveryDate;
            existingOrder.Currency = webhook.Currency ?? existingOrder.Currency;
            existingOrder.ConversionRate = webhook.ConversionRate ?? existingOrder.ConversionRate;
            existingOrder.Status = webhook.Status ?? existingOrder.Status;
            existingOrder.Total = webhook.Total ?? existingOrder.Total;
            existingOrder.TotalInBaseCurrency = webhook.TotalInBaseCurrency ?? existingOrder.TotalInBaseCurrency;
            existingOrder.AdditionalInfo = webhook.AdditionalInfo ?? existingOrder.AdditionalInfo;
            existingOrder.CustomerRef = webhook.CustomerRef ?? existingOrder.CustomerRef;
            existingOrder.LocationId = webhook.LocationId ?? existingOrder.LocationId;
            existingOrder.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("✅ UPSERT: Updated existing order from webhook. KatanaOrderId={KatanaOrderId}, LocalId={LocalId}, Status={Status}",
                webhook.KatanaOrderId, existingOrder.Id, existingOrder.Status);

            return new
            {
                success = true,
                action = "updated",
                message = "Sipariş güncellendi (upsert)",
                localOrderId = existingOrder.Id,
                katanaOrderId = webhook.KatanaOrderId,
                orderNo = existingOrder.OrderNo,
                status = existingOrder.Status
            };
        }

        // ✅ INSERT: Yeni sipariş oluştur
        var localCustomerId = await ResolveCustomerIdAsync(webhook.CustomerId);

        var salesOrder = new SalesOrder
        {
            KatanaOrderId = webhook.KatanaOrderId,
            OrderNo = webhook.OrderNo ?? $"SO-{webhook.KatanaOrderId}",
            CustomerId = localCustomerId,
            OrderCreatedDate = webhook.CreatedAt ?? DateTime.UtcNow,
            DeliveryDate = webhook.DeliveryDate,
            Currency = webhook.Currency ?? "TRY",
            ConversionRate = webhook.ConversionRate,
            Status = webhook.Status ?? "NOT_SHIPPED",
            Total = webhook.Total,
            TotalInBaseCurrency = webhook.TotalInBaseCurrency,
            AdditionalInfo = webhook.AdditionalInfo,
            CustomerRef = webhook.CustomerRef,
            Source = webhook.Source ?? "Katana-Webhook",
            LocationId = webhook.LocationId,
            CreatedAt = DateTime.UtcNow,
            IsSyncedToLuca = false
        };

        // Sipariş satırlarını ekle
        if (webhook.Lines != null && webhook.Lines.Count > 0)
        {
            foreach (var line in webhook.Lines)
            {
                var (productId, sku) = await ResolveVariantAsync(line.VariantId);
                
                var orderLine = new SalesOrderLine
                {
                    KatanaRowId = line.RowId,
                    VariantId = line.VariantId,
                    SKU = sku,
                    ProductName = line.ProductName,
                    Quantity = line.Quantity,
                    PricePerUnit = line.PricePerUnit,
                    PricePerUnitInBaseCurrency = line.PricePerUnitInBaseCurrency,
                    Total = line.Total,
                    TotalInBaseCurrency = line.TotalInBaseCurrency,
                    TaxRate = line.TaxRate,
                    TaxRateId = line.TaxRateId,
                    LocationId = line.LocationId,
                    CreatedAt = DateTime.UtcNow
                };
                
                salesOrder.Lines.Add(orderLine);
            }
        }

        _context.SalesOrders.Add(salesOrder);
        await _context.SaveChangesAsync();

        _logger.LogInformation("✅ Created sales order from webhook. KatanaOrderId={KatanaOrderId}, LocalId={LocalId}, OrderNo={OrderNo}, Status={Status}",
            webhook.KatanaOrderId, salesOrder.Id, salesOrder.OrderNo, salesOrder.Status);

        // PendingStockAdjustment oluştur (admin onayı için)
        if (webhook.Status?.ToUpperInvariant() != "CANCELLED" && webhook.Status?.ToUpperInvariant() != "SHIPPED")
        {
            await CreatePendingAdjustmentsForOrderAsync(salesOrder);
        }

        return new
        {
            success = true,
            action = "created",
            message = "Sipariş başarıyla oluşturuldu",
            localOrderId = salesOrder.Id,
            katanaOrderId = webhook.KatanaOrderId,
            orderNo = salesOrder.OrderNo,
            status = salesOrder.Status,
            lineCount = salesOrder.Lines.Count
        };
    }

    private async Task<object> HandleSalesOrderUpdatedAsync(KatanaSalesOrderWebhook webhook)
    {
        var existingOrder = await _context.SalesOrders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.KatanaOrderId == webhook.KatanaOrderId);

        if (existingOrder == null)
        {
            // Sipariş yoksa oluştur
            return await HandleSalesOrderCreatedAsync(webhook);
        }

        // Onaylanmış siparişleri güncelleme (koruma)
        if (existingOrder.Status?.ToUpperInvariant() == "APPROVED")
        {
            _logger.LogWarning("⚠️ Skipping update for approved order. KatanaOrderId={KatanaOrderId}, LocalId={LocalId}",
                webhook.KatanaOrderId, existingOrder.Id);
            return new
            {
                success = true,
                action = "skipped",
                message = "Onaylanmış sipariş güncellenemez",
                localOrderId = existingOrder.Id,
                katanaOrderId = webhook.KatanaOrderId
            };
        }

        // Güncelle
        existingOrder.DeliveryDate = webhook.DeliveryDate ?? existingOrder.DeliveryDate;
        existingOrder.Currency = webhook.Currency ?? existingOrder.Currency;
        existingOrder.ConversionRate = webhook.ConversionRate ?? existingOrder.ConversionRate;
        existingOrder.Status = webhook.Status ?? existingOrder.Status;
        existingOrder.Total = webhook.Total ?? existingOrder.Total;
        existingOrder.TotalInBaseCurrency = webhook.TotalInBaseCurrency ?? existingOrder.TotalInBaseCurrency;
        existingOrder.AdditionalInfo = webhook.AdditionalInfo ?? existingOrder.AdditionalInfo;
        existingOrder.CustomerRef = webhook.CustomerRef ?? existingOrder.CustomerRef;
        existingOrder.LocationId = webhook.LocationId ?? existingOrder.LocationId;
        existingOrder.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("✅ Updated sales order from webhook. KatanaOrderId={KatanaOrderId}, LocalId={LocalId}, Status={Status}",
            webhook.KatanaOrderId, existingOrder.Id, existingOrder.Status);

        return new
        {
            success = true,
            action = "updated",
            message = "Sipariş güncellendi",
            localOrderId = existingOrder.Id,
            katanaOrderId = webhook.KatanaOrderId,
            status = existingOrder.Status
        };
    }

    private async Task<object> HandleSalesOrderStatusChangedAsync(KatanaSalesOrderWebhook webhook)
    {
        var existingOrder = await _context.SalesOrders
            .FirstOrDefaultAsync(o => o.KatanaOrderId == webhook.KatanaOrderId);

        if (existingOrder == null)
        {
            _logger.LogWarning("Status change webhook for non-existent order. KatanaOrderId={KatanaOrderId}", webhook.KatanaOrderId);
            return new
            {
                success = false,
                action = "not_found",
                message = "Sipariş bulunamadı",
                katanaOrderId = webhook.KatanaOrderId
            };
        }

        var oldStatus = existingOrder.Status;
        existingOrder.Status = webhook.Status ?? existingOrder.Status;
        existingOrder.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("✅ Status changed from webhook. KatanaOrderId={KatanaOrderId}, OldStatus={OldStatus}, NewStatus={NewStatus}",
            webhook.KatanaOrderId, oldStatus, existingOrder.Status);

        return new
        {
            success = true,
            action = "status_changed",
            message = $"Sipariş durumu güncellendi: {oldStatus} → {existingOrder.Status}",
            localOrderId = existingOrder.Id,
            katanaOrderId = webhook.KatanaOrderId,
            oldStatus = oldStatus,
            newStatus = existingOrder.Status
        };
    }

    private async Task<object> HandleSalesOrderCancelledAsync(KatanaSalesOrderWebhook webhook)
    {
        var existingOrder = await _context.SalesOrders
            .FirstOrDefaultAsync(o => o.KatanaOrderId == webhook.KatanaOrderId);

        if (existingOrder == null)
        {
            return new
            {
                success = false,
                action = "not_found",
                message = "İptal edilecek sipariş bulunamadı",
                katanaOrderId = webhook.KatanaOrderId
            };
        }

        existingOrder.Status = "CANCELLED";
        existingOrder.UpdatedAt = DateTime.UtcNow;

        // İlgili pending adjustments'ları da iptal et
        var pendingAdjustments = await _context.PendingStockAdjustments
            .Where(p => p.ExternalOrderId == existingOrder.OrderNo && p.Status == "Pending")
            .ToListAsync();

        foreach (var pending in pendingAdjustments)
        {
            pending.Status = "Cancelled";
            pending.Notes = $"{pending.Notes} | Sipariş iptal edildi (Katana webhook)";
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("✅ Order cancelled from webhook. KatanaOrderId={KatanaOrderId}, LocalId={LocalId}, CancelledPendingCount={Count}",
            webhook.KatanaOrderId, existingOrder.Id, pendingAdjustments.Count);

        return new
        {
            success = true,
            action = "cancelled",
            message = "Sipariş iptal edildi",
            localOrderId = existingOrder.Id,
            katanaOrderId = webhook.KatanaOrderId,
            cancelledPendingAdjustments = pendingAdjustments.Count
        };
    }

    private async Task<int> ResolveCustomerIdAsync(long katanaCustomerId)
    {
        var katanaCustomerIdStr = katanaCustomerId.ToString();
        
        // Mevcut müşteri kontrolü
        var existingCustomer = await _context.Customers
            .FirstOrDefaultAsync(c => c.ReferenceId == katanaCustomerIdStr);

        if (existingCustomer != null)
            return existingCustomer.Id;

        // Katana'dan müşteri bilgisi çek
        try
        {
            var katanaCustomers = await _katanaService.GetCustomersAsync();
            var katanaCustomer = katanaCustomers.FirstOrDefault(c => c.Id == katanaCustomerId);

            if (katanaCustomer != null)
            {
                var defaultAddress = katanaCustomer.Addresses?.FirstOrDefault();
                
                var newCustomer = new Customer
                {
                    Title = katanaCustomer.Name ?? $"Customer-{katanaCustomerId}",
                    ReferenceId = katanaCustomerIdStr,
                    Email = katanaCustomer.Email,
                    Phone = katanaCustomer.Phone,
                    Address = defaultAddress?.Line1,
                    City = defaultAddress?.City,
                    Country = defaultAddress?.Country,
                    TaxNo = GetMax11SafeTaxNo(katanaCustomerId),
                    Currency = katanaCustomer.Currency ?? "TRY",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                
                _context.Customers.Add(newCustomer);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("✅ Created customer from webhook: {CustomerName} (ID: {CustomerId})", newCustomer.Title, newCustomer.Id);
                return newCustomer.Id;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch customer from Katana: {CustomerId}", katanaCustomerId);
        }

        // Fallback: Unknown customer oluştur
        var unknownCustomer = new Customer
        {
            Title = $"Unknown Customer (Katana ID: {katanaCustomerId})",
            ReferenceId = katanaCustomerIdStr,
            TaxNo = GetMax11SafeTaxNo(katanaCustomerId),
            Currency = "TRY",
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.Customers.Add(unknownCustomer);
        await _context.SaveChangesAsync();
        
        return unknownCustomer.Id;
    }

    private async Task<(int ProductId, string Sku)> ResolveVariantAsync(long variantId)
    {
        var mapping = await _variantMappingService.GetMappingAsync(variantId);
        if (mapping != null)
            return (mapping.ProductId, mapping.Sku);

        // Fallback
        return (0, $"VARIANT-{variantId}");
    }

    private async Task CreatePendingAdjustmentsForOrderAsync(SalesOrder order)
    {
        foreach (var line in order.Lines)
        {
            // Duplicate check
            var exists = await _context.PendingStockAdjustments
                .AnyAsync(p => p.ExternalOrderId == order.OrderNo && p.Sku == line.SKU);

            if (exists)
                continue;

            var pending = new PendingStockAdjustment
            {
                ExternalOrderId = order.OrderNo,
                ProductId = 0, // Will be resolved later
                Sku = line.SKU ?? $"VARIANT-{line.VariantId}",
                Quantity = -(int)Math.Abs(line.Quantity),
                RequestedBy = "Katana-Webhook",
                RequestedAt = DateTimeOffset.UtcNow,
                Status = "Pending",
                Notes = $"Katana webhook sipariş #{order.OrderNo}: {line.Quantity}x {line.SKU}"
            };

            await _pendingService.CreateAsync(pending);
        }
    }

    private static string GetMax11SafeTaxNo(long customerId)
    {
        var id = customerId.ToString();
        if (id.Length > 10) id = id.Substring(id.Length - 10);
        return $"U{id}";
    }

    /// <summary>
    /// Test endpoint - Sipariş webhook'u simüle et
    /// </summary>
    [HttpPost("test-sales-order")]
    [ApiExplorerSettings(IgnoreApi = false)]
    public async Task<IActionResult> TestSalesOrderWebhook()
    {
        var testWebhook = new KatanaSalesOrderWebhook
        {
            Event = "sales_order.created",
            KatanaOrderId = 999999,
            OrderNo = $"TEST-SO-{DateTime.UtcNow:yyyyMMddHHmmss}",
            CustomerId = 1,
            Status = "NOT_SHIPPED",
            Currency = "TRY",
            Total = 1000m,
            CreatedAt = DateTime.UtcNow,
            Lines = new List<KatanaSalesOrderLineWebhook>
            {
                new()
                {
                    RowId = 1,
                    VariantId = 12345,
                    ProductName = "Test Product",
                    Quantity = 5,
                    PricePerUnit = 200m,
                    Total = 1000m
                }
            }
        };

        Request.Headers["X-Katana-Signature"] = _configuration["KatanaApi:WebhookSecret"] ?? "test-secret";

        return await ReceiveSalesOrder(testWebhook);
    }
}

/// <summary>
/// Katana stok değişikliği webhook payload'u
/// </summary>
public class KatanaStockChangeWebhook
{
    
    public string Event { get; set; } = string.Empty;

    
    public string OrderId { get; set; } = string.Empty;

    
    public int ProductId { get; set; }

    
    public string? Sku { get; set; }

    
    public int QuantityChange { get; set; }

    
    public DateTime Timestamp { get; set; }

    
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Katana sipariş webhook payload'u
/// </summary>
public class KatanaSalesOrderWebhook
{
    /// <summary>Event tipi: sales_order.created, sales_order.updated, sales_order.status_changed, sales_order.cancelled</summary>
    [JsonPropertyName("event")]
    public string? Event { get; set; }

    /// <summary>Katana sipariş ID'si (benzersiz)</summary>
    [JsonPropertyName("id")]
    public long KatanaOrderId { get; set; }

    /// <summary>Sipariş numarası (örn: SO-123)</summary>
    [JsonPropertyName("order_no")]
    public string? OrderNo { get; set; }

    /// <summary>Katana müşteri ID'si</summary>
    [JsonPropertyName("customer_id")]
    public long CustomerId { get; set; }

    /// <summary>Sipariş durumu: NOT_SHIPPED, OPEN, SHIPPED, DELIVERED, CANCELLED</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>Para birimi</summary>
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    /// <summary>Döviz kuru</summary>
    [JsonPropertyName("conversion_rate")]
    public decimal? ConversionRate { get; set; }

    /// <summary>Toplam tutar</summary>
    [JsonPropertyName("total")]
    public decimal? Total { get; set; }

    /// <summary>Ana para biriminde toplam</summary>
    [JsonPropertyName("total_in_base_currency")]
    public decimal? TotalInBaseCurrency { get; set; }

    /// <summary>Teslimat tarihi</summary>
    [JsonPropertyName("delivery_date")]
    public DateTime? DeliveryDate { get; set; }

    /// <summary>Oluşturulma tarihi</summary>
    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }

    /// <summary>Ek bilgi</summary>
    [JsonPropertyName("additional_info")]
    public string? AdditionalInfo { get; set; }

    /// <summary>Müşteri referansı</summary>
    [JsonPropertyName("customer_ref")]
    public string? CustomerRef { get; set; }

    /// <summary>Kaynak</summary>
    [JsonPropertyName("source")]
    public string? Source { get; set; }

    /// <summary>Lokasyon ID</summary>
    [JsonPropertyName("location_id")]
    public long? LocationId { get; set; }

    /// <summary>Sipariş satırları</summary>
    [JsonPropertyName("sales_order_rows")]
    public List<KatanaSalesOrderLineWebhook>? Lines { get; set; }
}

/// <summary>
/// Katana sipariş satırı webhook payload'u
/// </summary>
public class KatanaSalesOrderLineWebhook
{
    [JsonPropertyName("id")]
    public long RowId { get; set; }

    [JsonPropertyName("variant_id")]
    public long VariantId { get; set; }

    [JsonPropertyName("product_name")]
    public string? ProductName { get; set; }

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }

    [JsonPropertyName("price_per_unit")]
    public decimal PricePerUnit { get; set; }

    [JsonPropertyName("price_per_unit_in_base_currency")]
    public decimal? PricePerUnitInBaseCurrency { get; set; }

    [JsonPropertyName("total")]
    public decimal Total { get; set; }

    [JsonPropertyName("total_in_base_currency")]
    public decimal? TotalInBaseCurrency { get; set; }

    [JsonPropertyName("tax_rate")]
    public decimal? TaxRate { get; set; }

    [JsonPropertyName("tax_rate_id")]
    public long? TaxRateId { get; set; }

    [JsonPropertyName("location_id")]
    public long? LocationId { get; set; }
}

internal static class KatanaWebhookSecurity
{
    
    public static bool SecureEquals(string? a, string? b)
    {
        if (a is null || b is null) return false;
        
        var ba = Encoding.UTF8.GetBytes(a);
        var bb = Encoding.UTF8.GetBytes(b);
        if (ba.Length != bb.Length)
        {
            
            var pad = new byte[Math.Max(ba.Length, bb.Length)];
            var aa = new byte[pad.Length];
            var bb2 = new byte[pad.Length];
            Array.Copy(ba, aa, Math.Min(ba.Length, aa.Length));
            Array.Copy(bb, bb2, Math.Min(bb.Length, bb2.Length));
            return CryptographicOperations.FixedTimeEquals(aa, bb2) && false; 
        }
        return CryptographicOperations.FixedTimeEquals(ba, bb);
    }
}
