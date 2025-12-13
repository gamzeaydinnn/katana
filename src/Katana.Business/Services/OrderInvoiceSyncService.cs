using Katana.Business.Interfaces;
using Katana.Core.Interfaces;
using Katana.Core.DTOs;
using Katana.Core.Entities;
using Katana.Core.Enums;
using Katana.Core.Events;
using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Wrap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Katana.Business.Services;

/// <summary>
/// Katana Sales Order & Purchase Order'ları Luca'ya Fatura olarak aktaran,
/// gerektiğinde ödemesini kapatan ve silen tam entegrasyon servisi.
/// 
/// Akış:
/// 1. Katana Order → LucaCreateInvoiceHeaderRequest mapping
/// 2. Luca API'ye fatura gönderimi
/// 3. Fatura ID kaydetme
/// 4. Ödeme kapama (opsiyonel)
/// 5. Silme (iptal durumunda)
/// </summary>
public class OrderInvoiceSyncService : IOrderInvoiceSyncService
{
    private readonly IntegrationDbContext _context;
    private readonly ILucaService _lucaService;
    private readonly IOrderMappingRepository _mappingRepo;
    private readonly ILogger<OrderInvoiceSyncService> _logger;
    private readonly IAuditService _auditService;
    private readonly IEventPublisher _eventPublisher;

    // Circuit Breaker - Luca API down olduğunda cascade failure'ı önler
    // 5 ardışık hata sonrası 2 dakika devre kesilir
    private static readonly AsyncCircuitBreakerPolicy _lucaCircuitBreaker = Policy
        .Handle<HttpRequestException>()
        .Or<TimeoutException>()
        .Or<TaskCanceledException>()
        .CircuitBreakerAsync(
            exceptionsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromMinutes(2),
            onBreak: (ex, duration) => Console.WriteLine($"[CircuitBreaker] Luca OPEN for {duration}"),
            onReset: () => Console.WriteLine("[CircuitBreaker] Luca CLOSED - recovered"),
            onHalfOpen: () => Console.WriteLine("[CircuitBreaker] Luca HALF-OPEN - testing..."));

    // Retry Policy - Luca API çağrıları için
    private static readonly AsyncRetryPolicy _lucaSyncRetryPolicy = Policy
        .Handle<HttpRequestException>()
        .Or<TimeoutException>()
        .Or<Exception>(ex => ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase))
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
            onRetry: (exception, delay, attempt, context) =>
            {
                if (context.TryGetValue("logger", out var loggerObj) && loggerObj is ILogger logger)
                {
                    logger.LogWarning(exception,
                        "Luca sync retry attempt {Attempt}/3 after {Delay}s",
                        attempt, delay.TotalSeconds);
                }
            });

    // Combined Policy: Circuit Breaker wraps Retry
    private static readonly AsyncPolicyWrap _lucaResiliencePolicy = 
        _lucaCircuitBreaker.WrapAsync(_lucaSyncRetryPolicy);

    /// <summary>
    /// Circuit Breaker durumunu kontrol et
    /// </summary>
    public static CircuitState LucaCircuitState => _lucaCircuitBreaker.CircuitState;

    // Luca Belge Türleri
    private const int LUCA_SATIS_FATURASI = 18;      // Satış Faturası
    private const int LUCA_ALIM_FATURASI = 16;       // Alım Faturası
    private const int MUSTERI = 1;                   // Müşteri
    private const int TEDARIKCI = 2;                 // Tedarikçi
    private const int MAL_HIZMET = 1;                // Mal/Hizmet faturası
    private const int STOK_KARTI = 1;                // Stok kartı türü

    public OrderInvoiceSyncService(
        IntegrationDbContext context,
        ILucaService lucaService,
        IOrderMappingRepository mappingRepo,
        ILogger<OrderInvoiceSyncService> logger,
        IAuditService auditService,
        IEventPublisher eventPublisher)
    {
        _context = context;
        _lucaService = lucaService;
        _mappingRepo = mappingRepo;
        _logger = logger;
        _auditService = auditService;
        _eventPublisher = eventPublisher;
    }

    #region Sales Order → Luca Satış Faturası

    /// <summary>
    /// Katana Sales Order'ı Luca'ya Satış Faturası olarak gönderir.
    /// </summary>
    public async Task<OrderSyncResultDto> SyncSalesOrderToLucaAsync(int orderId)
    {
        var result = new OrderSyncResultDto { OrderId = orderId, OrderType = "SalesOrder" };

        try
        {
            // 1. Order'ı getir
            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                result.Success = false;
                result.Message = $"Order bulunamadı: {orderId}";
                return result;
            }

            // 2. Daha önce gönderilmiş mi kontrol et
            var existingLucaId = await _mappingRepo.GetLucaInvoiceIdByOrderIdAsync(orderId, "SalesOrder");
            if (existingLucaId.HasValue)
            {
                result.Success = true;
                result.LucaFaturaId = existingLucaId.Value;
                result.Message = $"Order zaten Luca'ya gönderilmiş. Fatura ID: {existingLucaId.Value}";
                return result;
            }

            // 3. Luca request'i oluştur
            var lucaRequest = await BuildSalesInvoiceRequestAsync(order);
            if (lucaRequest == null)
            {
                result.Success = false;
                result.Message = "Luca fatura request'i oluşturulamadı - mapping eksik olabilir";
                return result;
            }

            // 4. Circuit Breaker kontrolü - API down ise hızlı fail
            if (_lucaCircuitBreaker.CircuitState == CircuitState.Open)
            {
                result.Success = false;
                result.Message = "Luca API şu anda erişilemez durumda (Circuit Open). Lütfen birkaç dakika sonra tekrar deneyin.";
                _logger.LogWarning("Luca sync skipped - Circuit Breaker is OPEN for Order {OrderId}", orderId);
                return result;
            }

            // 5. Luca'ya gönder (Circuit Breaker + Retry ile)
            var context = new Context();
            context["logger"] = _logger;
            
            var lucaResponse = await _lucaResiliencePolicy.ExecuteAsync(
                async (ctx) => await _lucaService.CreateInvoiceRawAsync(lucaRequest),
                context
            );

            // 5. Luca'dan dönen ID'yi parse et
            long? lucaFaturaId = null;
            bool isSuccess = false;

            if (lucaResponse.TryGetProperty("basarili", out var basariliProp) && basariliProp.GetBoolean())
            {
                isSuccess = true;
                // Luca fatura ID'sini parse et
                if (lucaResponse.TryGetProperty("ssFaturaBaslikId", out var faturaIdProp))
                {
                    lucaFaturaId = faturaIdProp.GetInt64();
                }
                else if (lucaResponse.TryGetProperty("id", out var idProp))
                {
                    lucaFaturaId = idProp.GetInt64();
                }
            }

            if (isSuccess && lucaFaturaId.HasValue)
            {
                // Transaction başlat - Atomik operasyon için
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // 6. Luca ID'yi mapping tablosuna kaydet
                        await _mappingRepo.SaveLucaInvoiceIdAsync(
                            orderId,
                            lucaFaturaId.Value,
                            "SalesOrder",
                            externalOrderId: order.OrderNo,
                            belgeSeri: lucaRequest.BelgeSeri,
                            belgeNo: lucaRequest.BelgeNo?.ToString(),
                            belgeTakipNo: lucaRequest.BelgeTakipNo ?? order.OrderNo);
                    
                    // 7. Order'ı synced olarak işaretle
                    order.IsSynced = true;
                    order.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    // Transaction commit
                    await transaction.CommitAsync();

                    result.Success = true;
                    result.LucaFaturaId = lucaFaturaId.Value;
                    result.Message = $"Satış faturası Luca'ya başarıyla gönderildi. Fatura ID: {lucaFaturaId.Value}";

                    _logger.LogInformation(
                        "Sales Order {OrderNo} successfully synced to Luca. Fatura ID: {FaturaId}",
                        order.OrderNo, lucaFaturaId.Value
                    );
                    
                    // 8. Audit log ekle
                    _auditService.LogSync(
                        "OrderInvoiceSync",
                        "system",
                        $"Order {orderId} synced to Luca as Invoice {lucaFaturaId.Value}"
                    );

                    // 9. InvoiceSyncedEvent publish et (bildirim için)
                    try
                    {
                        // Invoice entity oluştur veya mevcut olanı bul
                        var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.Id == orderId)
                            ?? new Invoice 
                            { 
                                InvoiceNo = order.OrderNo, 
                                CustomerId = order.CustomerId,
                                Amount = order.TotalAmount,
                                IsSynced = true
                            };
                        
                        var syncEvent = new InvoiceSyncedEvent(invoice, "OrderInvoiceSync");
                        
                        // Event publisher ile publish et
                        await _eventPublisher.PublishAsync(syncEvent);
                        
                        _logger.LogInformation("InvoiceSyncedEvent published: {Event}", syncEvent.ToString());
                    }
                    catch (Exception eventEx)
                    {
                        _logger.LogWarning(eventEx, "Failed to publish InvoiceSyncedEvent for Order {OrderId}", orderId);
                        // Event hatası ana işlemi etkilememeli
                    }
                }
                catch (Exception txEx)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(txEx, "Transaction failed for Order {OrderId}", orderId);
                    throw;
                }
            }
            else
            {
                result.Success = false;
                var errorMsg = lucaResponse.TryGetProperty("mesaj", out var mesajProp) 
                    ? mesajProp.GetString() 
                    : "Bilinmeyen Luca hatası";
                result.Message = $"Luca API hatası: {errorMsg}";
                _logger.LogWarning(
                    "Failed to sync Sales Order {OrderNo} to Luca: {Message}", 
                    order.OrderNo, errorMsg
                );
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing Sales Order {OrderId} to Luca", orderId);
            result.Success = false;
            result.Message = $"Hata: {ex.Message}";
            return result;
        }
    }

    /// <summary>
    /// Sales Order'ı Luca fatura formatına çevirir
    /// </summary>
    private async Task<LucaCreateInvoiceHeaderRequest?> BuildSalesInvoiceRequestAsync(Order order)
    {
        var validationErrors = new List<string>();
        const string entityType = "SalesOrder";

        // 1) mapping info çek
        var map = await _mappingRepo.GetMappingInfoAsync(order.Id, entityType);

        // 2) Belge alanlarını mapping'den al; yoksa deterministik üret
        var belgeSeri = !string.IsNullOrWhiteSpace(map?.BelgeSeri) ? map!.BelgeSeri : "A";

        var belgeNo = !string.IsNullOrWhiteSpace(map?.BelgeNo)
            ? map!.BelgeNo!
            : TryExtractDigitsLast9(order.OrderNo) ?? (1_000_000 + order.Id).ToString();

        var belgeTakipNo = !string.IsNullOrWhiteSpace(map?.BelgeTakipNo)
            ? map!.BelgeTakipNo!
            : Left50(order.OrderNo ?? $"{entityType}-{order.Id}");

        // 3) mapping eksikse (veya alanlar boşsa) DB'ye yaz ki retry'da aynı belge kullansın
        if (map is null || string.IsNullOrWhiteSpace(map.BelgeSeri) || string.IsNullOrWhiteSpace(map.BelgeNo) || string.IsNullOrWhiteSpace(map.BelgeTakipNo))
        {
            await _mappingRepo.UpsertMappingInfoAsync(
                orderId: order.Id,
                entityType: entityType,
                externalOrderId: order.OrderNo,
                belgeSeri: belgeSeri,
                belgeNo: belgeNo,
                belgeTakipNo: belgeTakipNo,
                ct: default
            );
        }

        var cariKodu = await _mappingRepo.GetLucaCariKoduByCustomerIdAsync(order.CustomerId.ToString());

        var cariValidation = Validators.LucaDataValidator.ValidateCariKodu(cariKodu, "Müşteri Kodu");
        if (!cariValidation.IsValid)
        {
            _logger.LogWarning("Customer {CustomerId} validation failed: {Error}", order.CustomerId, cariValidation.ErrorMessage);
            cariKodu = $"MUS-{order.CustomerId:D5}";
            _logger.LogWarning("Fallback cari kodu kullanılıyor: {CariKodu}", cariKodu);
        }

        var currencyValidation = Validators.LucaDataValidator.ValidateCurrency(order.Currency);
        if (!currencyValidation.IsValid)
        {
            _logger.LogWarning("Order {OrderNo} currency validation failed: {Error}. Using fallback.", order.OrderNo, currencyValidation.ErrorMessage);
            order.Currency = "TRY";
        }

        var docNoValidation = Validators.LucaDataValidator.ValidateDocumentNo(order.OrderNo, "Sipariş No");
        if (!docNoValidation.IsValid)
        {
            _logger.LogWarning("Order {OrderNo} document no validation failed: {Error}. Using fallback.", order.OrderNo, docNoValidation.ErrorMessage);
            order.OrderNo = $"ORD-{order.Id:D8}";
        }

        var dateValidation = Validators.LucaDataValidator.ValidateDate(order.OrderDate, "Sipariş Tarihi", allowFuture: false);
        if (!dateValidation.IsValid)
        {
            _logger.LogWarning("Order {OrderNo} date validation failed: {Error}. Using fallback.", order.OrderNo, dateValidation.ErrorMessage);
            order.OrderDate = DateTime.UtcNow;
        }

        var belgeTurDetayId = await _mappingRepo.GetBelgeTurDetayIdAsync(isSalesOrder: true);

        // Luca API artık string belgeNo kabul ediyor
        var belgeNoStr = string.IsNullOrWhiteSpace(belgeNo) ? order.Id.ToString() : belgeNo.Trim();

        var request = new LucaCreateInvoiceHeaderRequest
        {
            BelgeSeri = belgeSeri,
            BelgeNo = belgeNoStr,
            BelgeTarihi = order.OrderDate.ToString("dd/MM/yyyy"),
            VadeTarihi = order.OrderDate.AddDays(30).ToString("dd/MM/yyyy"),
            BelgeAciklama = $"Katana Sales Order #{order.OrderNo}",
            BelgeTurDetayId = belgeTurDetayId.ToString(),
            BelgeTakipNo = belgeTakipNo,
            FaturaTur = MAL_HIZMET.ToString(),
            ParaBirimKod = order.Currency ?? "TRY",
            KdvFlag = false,
            MusteriTedarikci = MUSTERI.ToString(),
            CariKodu = cariKodu,
            CariTanim = order.Customer?.Title,
            VergiNo = order.Customer?.TaxNo,
            SiparisNo = order.OrderNo,
            SiparisTarihi = order.OrderDate,
            DetayList = new List<LucaCreateInvoiceDetailRequest>()
        };

        // Satırları dönüştür
        foreach (var item in order.Items)
        {
            var kartKodu = await _mappingRepo.GetLucaStokKoduByProductIdAsync(item.ProductId);
            
            // Validation: Stok kodu
            var stokValidation = Validators.LucaDataValidator.ValidateStokKodu(kartKodu, item.Product?.Name);
            if (!stokValidation.IsValid)
            {
                _logger.LogWarning("Product {ProductId} stok kodu validation failed: {Error}", 
                    item.ProductId, stokValidation.ErrorMessage);
                
                // Fallback: Product SKU kullan
                kartKodu = item.Product?.SKU ?? $"PRD-{item.ProductId:D5}";
                _logger.LogWarning("Fallback stok kodu kullanılıyor: {KartKodu}", kartKodu);
            }

            // Validation: Miktar
            var qtyValidation = Validators.LucaDataValidator.ValidateQuantity(item.Quantity, "Miktar");
            if (!qtyValidation.IsValid)
            {
                _logger.LogWarning("Product {ProductName} quantity validation failed: {Error}. Using fallback.", 
                    item.Product?.Name, qtyValidation.ErrorMessage);
                
                // Fallback: Minimum 1 quantity
                item.Quantity = Math.Max(1, item.Quantity);
            }

            // Validation: Birim fiyat
            var priceValidation = Validators.LucaDataValidator.ValidateDecimalPrecision(item.UnitPrice, "Birim Fiyat");
            if (!priceValidation.IsValid)
            {
                _logger.LogWarning("Product {ProductName} price validation failed: {Error}. Using fallback.", 
                    item.Product?.Name, priceValidation.ErrorMessage);
                
                // Fallback: Round to 2 decimals
                item.UnitPrice = Math.Round(item.UnitPrice, 2);
            }

            var taxRate = await _mappingRepo.GetTaxRateByIdAsync(null); // Default KDV

            // Validation: KDV oranı
            var taxValidation = Validators.LucaDataValidator.ValidateTaxRate((decimal)taxRate);
            if (!taxValidation.IsValid)
            {
                _logger.LogWarning("Tax rate validation failed: {Error}. Using fallback.", 
                    taxValidation.ErrorMessage);
                
                // Fallback: Default 20% KDV
                taxRate = 20.0;
            }

            request.DetayList.Add(new LucaCreateInvoiceDetailRequest
            {
                KartTuru = STOK_KARTI,
                KartKodu = kartKodu,
                KartAdi = item.Product?.Name,
                Miktar = item.Quantity,
                BirimFiyat = (double)item.UnitPrice,
                KdvOran = taxRate,
                Aciklama = item.Product?.Name
            });
        }

        // Eğer kritik validation hataları varsa null dön
        // Not: Artık çoğu validation fallback kullanıyor, sadece kritik hatalar engeller
        if (validationErrors.Any())
        {
            _logger.LogError("Luca critical validation failed for Order {OrderNo}. Errors: {Errors}", 
                order.OrderNo, string.Join("; ", validationErrors));
            
            // Hataları audit log'a kaydet
            _auditService.LogAction(
                "OrderInvoiceSync",
                "Order",
                order.Id.ToString(),
                "System",
                $"Luca validation errors (critical): {string.Join("; ", validationErrors)}"
            );
            
            return null;
        }
        
        // Fallback kullanımını log'la
            _logger.LogInformation("Order {OrderNo} converted to Luca invoice with fallback values where needed", 
                order.OrderNo);

            return request;
        }

    #endregion

    #region Purchase Order → Luca Alım Faturası

    /// <summary>
    /// Katana Purchase Order'ı Luca'ya Alım Faturası olarak gönderir.
    /// Not: PurchaseOrder entity'si projede yoksa, Order entity'si supplier_id ile kullanılabilir
    /// veya ayrı bir PurchaseOrder entity'si oluşturulmalıdır.
    /// </summary>
    public async Task<OrderSyncResultDto> SyncPurchaseOrderToLucaAsync(int purchaseOrderId)
    {
        var result = new OrderSyncResultDto { OrderId = purchaseOrderId, OrderType = "PurchaseOrder" };

        try
        {
            // PurchaseOrder entity'si varsa kullan, yoksa Order entity'si supplier modunda
            // Bu örnekte genel bir yaklaşım gösteriyoruz

            var existingLucaId = await _mappingRepo.GetLucaInvoiceIdByOrderIdAsync(purchaseOrderId, "PurchaseOrder");
            if (existingLucaId.HasValue)
            {
                result.Success = true;
                result.LucaFaturaId = existingLucaId.Value;
                result.Message = $"Purchase Order zaten Luca'ya gönderilmiş. Fatura ID: {existingLucaId.Value}";
                return result;
            }

            // TODO: Gerçek PurchaseOrder entity'si eklendiğinde bu method güncellenmeli
            _logger.LogWarning("PurchaseOrder sync not fully implemented yet. PO ID: {POId}", purchaseOrderId);
            
            result.Success = false;
            result.Message = "PurchaseOrder entity henüz implemente edilmedi";
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing Purchase Order {POId} to Luca", purchaseOrderId);
            result.Success = false;
            result.Message = $"Hata: {ex.Message}";
            return result;
        }
    }

    #endregion

    #region Fatura Kapama (Ödeme/Tahsilat)

    /// <summary>
    /// Luca'daki faturayı kapatır (ödeme/tahsilat işler)
    /// Sales Order için: Tahsilat
    /// Purchase Order için: Tediye
    /// </summary>
    public async Task<OrderSyncResultDto> CloseInvoiceAsync(int orderId, string orderType, decimal amount)
    {
        var result = new OrderSyncResultDto { OrderId = orderId, OrderType = orderType };

        try
        {
            var isSalesOrder = orderType.Equals("SalesOrder", StringComparison.OrdinalIgnoreCase);
            
            // Luca fatura ID'sini bul
            var lucaFaturaId = await _mappingRepo.GetLucaInvoiceIdByOrderIdAsync(orderId, orderType);
            if (!lucaFaturaId.HasValue)
            {
                result.Success = false;
                result.Message = $"Order için Luca fatura ID bulunamadı: {orderId}";
                return result;
            }

            var belgeTurDetayId = await _mappingRepo.GetPaymentBelgeTurDetayIdAsync(isSalesOrder);
            var kasaKodu = await _mappingRepo.GetDefaultCashAccountCodeAsync();

            var closeRequest = new LucaCloseInvoiceRequest
            {
                FaturaId = lucaFaturaId.Value,
                Tutar = (double)amount,
                BelgeTurDetayId = belgeTurDetayId,
                CariKod = kasaKodu
            };

            var response = await _lucaService.CloseInvoiceAsync(closeRequest);

            // Response kontrol
            if (response.TryGetProperty("basarili", out var basariliProp) && basariliProp.GetBoolean())
            {
                result.Success = true;
                result.LucaFaturaId = lucaFaturaId.Value;
                result.Message = $"Fatura başarıyla kapatıldı. Fatura ID: {lucaFaturaId.Value}";
                _logger.LogInformation("Invoice {FaturaId} closed successfully for Order {OrderId}", lucaFaturaId.Value, orderId);
            }
            else
            {
                var mesaj = response.TryGetProperty("mesaj", out var mesajProp) ? mesajProp.GetString() : "Bilinmeyen hata";
                result.Success = false;
                result.Message = $"Fatura kapama hatası: {mesaj}";
                _logger.LogWarning("Failed to close invoice {FaturaId}: {Message}", lucaFaturaId.Value, mesaj);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing invoice for Order {OrderId}", orderId);
            result.Success = false;
            result.Message = $"Hata: {ex.Message}";
            return result;
        }
    }

    #endregion

    #region Fatura Silme

    /// <summary>
    /// Luca'daki faturayı siler (iptal durumunda)
    /// </summary>
    public async Task<OrderSyncResultDto> DeleteInvoiceAsync(int orderId, string orderType)
    {
        var result = new OrderSyncResultDto { OrderId = orderId, OrderType = orderType };

        try
        {
            var lucaFaturaId = await _mappingRepo.GetLucaInvoiceIdByOrderIdAsync(orderId, orderType);
            if (!lucaFaturaId.HasValue)
            {
                result.Success = false;
                result.Message = $"Order için Luca fatura ID bulunamadı: {orderId}";
                return result;
            }

            var deleteRequest = new LucaDeleteInvoiceRequest
            {
                SsFaturaBaslikId = lucaFaturaId.Value
            };

            var response = await _lucaService.DeleteInvoiceAsync(deleteRequest);

            if (response.TryGetProperty("basarili", out var basariliProp) && basariliProp.GetBoolean())
            {
                result.Success = true;
                result.LucaFaturaId = lucaFaturaId.Value;
                result.Message = $"Fatura başarıyla silindi. Fatura ID: {lucaFaturaId.Value}";
                _logger.LogInformation("Invoice {FaturaId} deleted successfully for Order {OrderId}", lucaFaturaId.Value, orderId);
            }
            else
            {
                var mesaj = response.TryGetProperty("mesaj", out var mesajProp) ? mesajProp.GetString() : "Bilinmeyen hata";
                result.Success = false;
                result.Message = $"Fatura silme hatası: {mesaj}";
                _logger.LogWarning("Failed to delete invoice {FaturaId}: {Message}", lucaFaturaId.Value, mesaj);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting invoice for Order {OrderId}", orderId);
            result.Success = false;
            result.Message = $"Hata: {ex.Message}";
            return result;
        }
    }

    #endregion

    #region Batch Sync

    /// <summary>
    /// Bekleyen (synced olmayan) tüm Sales Order'ları Luca'ya gönderir
    /// </summary>
    public async Task<OrderBatchSyncResultDto> SyncPendingSalesOrdersAsync()
    {
        var result = new OrderBatchSyncResultDto();
        
        // ✅ Circuit Breaker kontrolü - API down ise batch'i başlatma
        if (_lucaCircuitBreaker.CircuitState == CircuitState.Open)
        {
            result.Message = "Luca API şu anda erişilemez (Circuit Open). Batch sync atlandı.";
            _logger.LogWarning("Batch sync skipped - Luca Circuit Breaker is OPEN");
            return result;
        }
        
        // ✅ Performance metrics tracking
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var pendingOrders = await _context.Orders
                .Where(o => !o.IsSynced && o.Status == OrderStatus.Delivered)
                .OrderBy(o => o.OrderDate)
                .Take(50) // Batch limit
                .ToListAsync();

            result.TotalCount = pendingOrders.Count;

            int successCount = 0;
            int failCount = 0;

            // ✅ Parallel batch processing (5 concurrent requests)
            await Parallel.ForEachAsync(pendingOrders,
                new ParallelOptions { MaxDegreeOfParallelism = 5 },
                async (order, ct) =>
                {
                    var syncResult = await SyncSalesOrderToLucaAsync(order.Id);
                    if (syncResult.Success)
                    {
                        Interlocked.Increment(ref successCount);
                    }
                    else
                    {
                        Interlocked.Increment(ref failCount);
                        lock (result.FailedOrderIds)
                        {
                            result.FailedOrderIds.Add(order.Id);
                        }
                    }
                });

            result.SuccessCount = successCount;
            result.FailCount = failCount;
            
            sw.Stop();
            
            // ✅ Performance metrics
            var rate = successCount > 0 ? successCount * 60000.0 / sw.ElapsedMilliseconds : 0;
            result.Message = $"Batch sync tamamlandı. Başarılı: {result.SuccessCount}, Başarısız: {result.FailCount}, " +
                           $"Süre: {sw.ElapsedMilliseconds}ms, Hız: {rate:F2} sipariş/dk";
            
            _logger.LogInformation(
                "Batch sales order sync completed. Success: {Success}/{Total}, Failed: {Failed}, " +
                "Duration: {Duration}ms, Rate: {Rate:F2} orders/min",
                result.SuccessCount, result.TotalCount, result.FailCount, sw.ElapsedMilliseconds, rate);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Error in batch sales order sync after {Duration}ms", sw.ElapsedMilliseconds);
            result.Message = $"Batch sync hatası: {ex.Message}";
        }

        return result;
    }

    #endregion

    #region Helpers

    private static string Left50(string s) => s.Length <= 50 ? s : s.Substring(0, 50);

    private static string? TryExtractDigitsLast9(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        var digits = new string(input.Where(char.IsDigit).ToArray());
        if (digits.Length == 0) return null;

        // INT taşmasın diye son 9 haneyi al
        if (digits.Length > 9) digits = digits.Substring(digits.Length - 9);

        return digits;
    }

    private int? TryParseOrderNo(string? orderNo)
    {
        // "SO-1001" gibi formatları handle et
        if (string.IsNullOrEmpty(orderNo))
            return null;

        // Sadece rakamları al
        var digits = new string(orderNo.Where(char.IsDigit).ToArray());
        if (int.TryParse(digits, out int result))
            return result;

        return null; // Luca kendi numara verebilir
    }

    #endregion
}

#region DTOs

/// <summary>
/// Tekil order sync sonucu
/// </summary>
public class OrderSyncResultDto
{
    public int OrderId { get; set; }
    public string OrderType { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public long? LucaFaturaId { get; set; }
}

/// <summary>
/// Toplu order sync sonucu
/// </summary>
public class OrderBatchSyncResultDto
{
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailCount { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<int> FailedOrderIds { get; set; } = new();
}

#endregion
