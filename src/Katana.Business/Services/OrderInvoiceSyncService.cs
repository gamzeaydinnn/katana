using Katana.Business.Interfaces;
using Katana.Core.Interfaces;
using Katana.Core.DTOs;
using Katana.Core.Entities;
using Katana.Core.Enums;
using Katana.Core.Events;
using Katana.Core.Helpers;
using Katana.Data.Context;
using Katana.Data.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    private readonly IVariantMappingService _variantMappingService;
    private readonly ILogger<OrderInvoiceSyncService> _logger;
    private readonly IAuditService _auditService;
    private readonly IEventPublisher _eventPublisher;
    private readonly LucaApiSettings _lucaSettings;

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
        IVariantMappingService variantMappingService,
        ILogger<OrderInvoiceSyncService> logger,
        IAuditService auditService,
        IEventPublisher eventPublisher,
        IOptions<LucaApiSettings> lucaSettings)
    {
        _context = context;
        _lucaService = lucaService;
        _lucaSettings = lucaSettings.Value;
        _mappingRepo = mappingRepo;
        _variantMappingService = variantMappingService;
        _logger = logger;
        _auditService = auditService;
        _eventPublisher = eventPublisher;
    }

    #region Sales Order → Luca Satış Faturası

    /// <summary>
    /// Katana Sales Order'ı Luca'ya Satış Faturası olarak gönderir.
    /// Tekrarlı fatura oluşumunu engeller ve sipariş onay durumunu kontrol eder.
    /// </summary>
    public async Task<OrderSyncResultDto> SyncSalesOrderToLucaAsync(int orderId)
    {
        var result = new OrderSyncResultDto { OrderId = orderId, OrderType = "SalesOrder" };

        try
        {
            // 1. SalesOrder'ı getir
            var order = await _context.SalesOrders
                .Include(o => o.Customer)
                .Include(o => o.Lines)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                result.Success = false;
                result.Message = $"Sales Order bulunamadı: {orderId}";
                return result;
            }

            // 2. ✅ Sipariş onaylanmış mı kontrol et
            if (!string.IsNullOrEmpty(order.Status) && order.Status != "APPROVED" && order.Status != "Approved")
            {
                result.Success = false;
                result.Message = $"Sipariş henüz onaylanmadı. Mevcut durum: {order.Status}";
                _logger.LogWarning("Sipariş {OrderId} ({OrderNo}) onaysız olduğu için Luca'ya gönderilemedi. Status: {Status}", 
                    orderId, order.OrderNo, order.Status);
                return result;
            }

            // 3. ✅ IsSyncedToLuca flag kontrolü - zaten senkronize edilmişse tekrar gönderme
            if (order.IsSyncedToLuca)
            {
                // Mapping'den LucaInvoiceId'yi bul
                var existingInvoiceId = await _context.OrderMappings
                    .AsNoTracking()
                    .Where(m => m.EntityType == "SalesOrder" && m.OrderId == orderId && m.LucaInvoiceId > 0)
                    .Select(m => m.LucaInvoiceId)
                    .FirstOrDefaultAsync();

                if (existingInvoiceId > 0)
                {
                    result.Success = true;
                    result.LucaFaturaId = existingInvoiceId;
                    result.Message = $"Sipariş zaten Luca'ya senkronize edilmiş. Fatura ID: {existingInvoiceId}";
                    _logger.LogInformation("Sipariş {OrderId} ({OrderNo}) zaten Luca'da mevcut. InvoiceId: {InvoiceId}", 
                        orderId, order.OrderNo, existingInvoiceId);
                    return result;
                }
            }

            var group = await LoadSalesOrderGroupAsync(order);
            var groupOrderIds = group.Orders.Select(o => o.Id).ToList();

            // 4. ✅ Daha önce gönderilmiş mi kontrol et (grup içinden herhangi biri SYNCED ise)
            var existingMapping = await _context.OrderMappings
                .AsNoTracking()
                .Where(m => m.EntityType == "SalesOrder" && groupOrderIds.Contains(m.OrderId) && m.LucaInvoiceId > 0)
                .OrderBy(m => m.OrderId)
                .FirstOrDefaultAsync();

            if (existingMapping != null && existingMapping.LucaInvoiceId > 0)
            {
                await MarkSalesOrdersSyncedAsync(group.Orders, existingMapping.LucaInvoiceId, null);
                result.Success = true;
                result.LucaFaturaId = existingMapping.LucaInvoiceId;
                result.Message = $"Order zaten Luca'ya gönderilmiş. Fatura ID: {existingMapping.LucaInvoiceId}";
                _logger.LogInformation("Sipariş grubu {OrderId} için zaten Luca faturası mevcut: {InvoiceId}", 
                    orderId, existingMapping.LucaInvoiceId);
                return result;
            }

            // 5. Luca request'i oluştur
            var lucaRequest = await BuildSalesInvoiceRequestFromSalesOrderAsync(group.HeaderOrder, group.Lines);
            if (lucaRequest == null)
            {
                result.Success = false;
                result.Message = "Luca fatura request'i oluşturulamadı - mapping eksik olabilir";
                return result;
            }
            _logger.LogInformation("BUILD_INVOICE BelgeTakipNo={BelgeNo} OrderId={OrderId} LineCount={LineCount}",
                order.OrderNo ?? orderId.ToString(), orderId, group.Lines.Count);

            // 6. Circuit Breaker kontrolü - API down ise hızlı fail
            if (_lucaCircuitBreaker.CircuitState == CircuitState.Open)
            {
                result.Success = false;
                result.Message = "Luca API şu anda erişilemez durumda (Circuit Open). Lütfen birkaç dakika sonra tekrar deneyin.";
                _logger.LogWarning("Luca sync skipped - Circuit Breaker is OPEN for Order {OrderId}", orderId);
                return result;
            }

            // 7. Luca'ya gönder (Circuit Breaker + Retry ile)
            var context = new Context();
            context["logger"] = _logger;
            
            var lucaResponse = await _lucaResiliencePolicy.ExecuteAsync(
                async (ctx) => await _lucaService.CreateInvoiceRawAsync(lucaRequest),
                context
            );

            // 8. Luca'dan dönen ID'yi parse et
            long? lucaFaturaId = null;
            bool isSuccess = false;

            // 🔥 Önce hata kodunu kontrol et (code: 1001, 1002 = Login gerekli)
            if (lucaResponse.TryGetProperty("code", out var codeProp) && codeProp.ValueKind == JsonValueKind.Number)
            {
                var code = codeProp.GetInt32();
                if (code == 1001 || code == 1002)
                {
                    var msg = lucaResponse.TryGetProperty("message", out var msgProp) ? msgProp.GetString() : "Login olunmalı";
                    result.Success = false;
                    result.Message = $"Luca oturum hatası: {msg}. Lütfen tekrar deneyin.";
                    _logger.LogError("Luca returned login required error for Order {OrderNo}. Code: {Code}", order.OrderNo, code);
                    return result;
                }
            }

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
                var strategy = _context.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    await using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        // 9. Luca ID'yi tüm grup siparişlerine kaydet
                        await MarkSalesOrdersSyncedAsync(group.Orders, lucaFaturaId.Value, lucaRequest);

                        await transaction.CommitAsync();
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                });

                result.Success = true;
                result.LucaFaturaId = lucaFaturaId.Value;
                result.Message = $"Satış faturası Luca'ya başarıyla gönderildi. Fatura ID: {lucaFaturaId.Value}";

                _logger.LogInformation("CREATE_INVOICE_OK BelgeTakipNo={BelgeNo} OrderId={OrderId} LucaInvoiceId={LucaInvoiceId} DetayCount={DetayCount}",
                    order.OrderNo ?? orderId.ToString(), orderId, lucaFaturaId.Value, lucaRequest.DetayList?.Count ?? 0);

                _logger.LogInformation(
                    "Sales Order {OrderNo} successfully synced to Luca. Fatura ID: {FaturaId}",
                    order.OrderNo, lucaFaturaId.Value
                );
                
                // 10. Audit log ekle
                _auditService.LogSync(
                    "OrderInvoiceSync",
                    "system",
                    $"Order {orderId} synced to Luca as Invoice {lucaFaturaId.Value} (KatanaOrderId={order.KatanaOrderId})"
                );

                // 11. InvoiceSyncedEvent publish et (bildirim için)
                try
                {
                    // Invoice entity oluştur veya mevcut olanı bul
                    var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.Id == orderId)
                        ?? new Invoice 
                        { 
                            InvoiceNo = order.OrderNo, 
                            CustomerId = order.CustomerId,
                            Amount = group.Lines.Sum(l => l.Total ?? 0m),
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
                _logger.LogError("CREATE_INVOICE_FAIL BelgeTakipNo={BelgeNo} OrderId={OrderId} Error={Error}",
                    order.OrderNo ?? orderId.ToString(), orderId, errorMsg);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing Sales Order {OrderId} to Luca", orderId);
            _logger.LogError("CREATE_INVOICE_FAIL OrderId={OrderId} Error={Error}", orderId, ex.Message);
            result.Success = false;
            result.Message = $"Hata: {ex.Message}";
            return result;
        }
    }

    private sealed record SalesOrderGroup(SalesOrder HeaderOrder, List<SalesOrder> Orders, List<SalesOrderLine> Lines);

    private async Task<SalesOrderGroup> LoadSalesOrderGroupAsync(SalesOrder order)
    {
        if (order.KatanaOrderId <= 0)
        {
            var fallbackLines = order.Lines?.ToList() ?? new List<SalesOrderLine>();
            return new SalesOrderGroup(order, new List<SalesOrder> { order }, fallbackLines);
        }

        var orders = await _context.SalesOrders
            .Include(o => o.Customer)
            .Include(o => o.Lines)
            .Where(o => o.KatanaOrderId == order.KatanaOrderId)
            .OrderBy(o => o.OrderCreatedDate ?? o.CreatedAt)
            .ThenBy(o => o.Id)
            .ToListAsync();

        if (orders.Count == 0)
        {
            var fallbackLines = order.Lines?.ToList() ?? new List<SalesOrderLine>();
            return new SalesOrderGroup(order, new List<SalesOrder> { order }, fallbackLines);
        }

        var header = orders.FirstOrDefault(o => o.Customer != null) ?? orders.First();
        var mergedLines = MergeSalesOrderLines(orders);

        return new SalesOrderGroup(header, orders, mergedLines);
    }

    private static List<SalesOrderLine> MergeSalesOrderLines(IEnumerable<SalesOrder> orders)
    {
        var lines = orders
            .SelectMany(o => o.Lines ?? Enumerable.Empty<SalesOrderLine>())
            .ToList();

        return lines
            .GroupBy(l => l.KatanaRowId != 0 ? $"K:{l.KatanaRowId}" : $"L:{l.Id}")
            .Select(g => g.First())
            .OrderBy(l => l.KatanaRowId)
            .ThenBy(l => l.Id)
            .ToList();
    }

    private async Task MarkSalesOrdersSyncedAsync(
        List<SalesOrder> orders,
        long lucaInvoiceId,
        LucaCreateInvoiceHeaderRequest? request)
    {
        var now = DateTime.UtcNow;

        foreach (var groupOrder in orders)
        {
            await _mappingRepo.SaveLucaInvoiceIdAsync(
                groupOrder.Id,
                lucaInvoiceId,
                "SalesOrder",
                externalOrderId: groupOrder.OrderNo,
                belgeSeri: request?.BelgeSeri ?? groupOrder.BelgeSeri,
                belgeNo: request?.BelgeNo?.ToString() ?? groupOrder.BelgeNo,
                belgeTakipNo: request?.BelgeTakipNo ?? groupOrder.OrderNo);

            groupOrder.IsSyncedToLuca = true;
            groupOrder.LastSyncAt = now;
            groupOrder.LastSyncError = null;
            groupOrder.UpdatedAt = now;
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// SalesOrder'ı Luca fatura formatına çevirir
    /// </summary>
    private async Task<LucaCreateInvoiceHeaderRequest?> BuildSalesInvoiceRequestFromSalesOrderAsync(
        SalesOrder order,
        IReadOnlyList<SalesOrderLine> lines)
    {
        var validationErrors = new List<string>();
        const string entityType = "SalesOrder";

        // 1) mapping info çek
        var map = await _mappingRepo.GetMappingInfoAsync(order.Id, entityType);

        // 2) Belge alanlarını mapping'den al; yoksa appsettings'den al
        var belgeSeri = !string.IsNullOrWhiteSpace(map?.BelgeSeri) ? map!.BelgeSeri : 
                        !string.IsNullOrWhiteSpace(order.BelgeSeri) ? order.BelgeSeri :
                        _lucaSettings.DefaultBelgeSeri;

        var belgeNo = !string.IsNullOrWhiteSpace(map?.BelgeNo) ? map!.BelgeNo! :
                      !string.IsNullOrWhiteSpace(order.BelgeNo) ? order.BelgeNo :
                      TryExtractDigitsLast9(order.OrderNo) ?? (1_000_000 + order.Id).ToString();

        var belgeTakipNo = !string.IsNullOrWhiteSpace(map?.BelgeTakipNo) ? map!.BelgeTakipNo! :
                          Left50(order.OrderNo ?? $"{entityType}-{order.Id}");

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

        var currency = order.Currency ?? "TRY";
        var currencyValidation = Validators.LucaDataValidator.ValidateCurrency(currency);
        if (!currencyValidation.IsValid)
        {
            _logger.LogWarning("Order {OrderNo} currency validation failed: {Error}. Using fallback.", order.OrderNo, currencyValidation.ErrorMessage);
            currency = "TRY";
        }

        var orderNo = order.OrderNo;
        var docNoValidation = Validators.LucaDataValidator.ValidateDocumentNo(orderNo, "Sipariş No");
        if (!docNoValidation.IsValid)
        {
            _logger.LogWarning("Order {OrderNo} document no validation failed: {Error}. Using fallback.", orderNo, docNoValidation.ErrorMessage);
            orderNo = $"SO-{order.Id:D8}";
        }

        var orderDate = order.OrderCreatedDate ?? order.CreatedAt;
        var dateValidation = Validators.LucaDataValidator.ValidateDate(orderDate, "Sipariş Tarihi", allowFuture: false);
        if (!dateValidation.IsValid)
        {
            _logger.LogWarning("Order {OrderNo} date validation failed: {Error}. Using fallback.", orderNo, dateValidation.ErrorMessage);
            orderDate = DateTime.UtcNow;
        }

        var belgeTurDetayId = await _mappingRepo.GetBelgeTurDetayIdAsync(isSalesOrder: true);

        // Luca API artık string belgeNo kabul ediyor
        var belgeNoStr = string.IsNullOrWhiteSpace(belgeNo) ? order.Id.ToString() : belgeNo.Trim();

        // CariAd ve CariSoyad: ContactPerson veya Title'dan üret
        // Öncelik: ContactPerson > Title
        var rawNameSource = !string.IsNullOrWhiteSpace(order.Customer?.ContactPerson) 
            ? order.Customer.ContactPerson 
            : order.Customer?.Title;
        
        string cariAd;
        string cariSoyad;
        
        if (!string.IsNullOrWhiteSpace(rawNameSource))
        {
            // Normalize: Trim + çoklu boşluğu teke indir
            var normalizedName = System.Text.RegularExpressions.Regex.Replace(rawNameSource.Trim(), @"\s+", " ");
            var nameParts = normalizedName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            if (nameParts.Length >= 2)
            {
                // 2+ kelime: soyad = son kelime, ad = kalan kısım
                cariSoyad = nameParts[^1]; // Son kelime
                cariAd = string.Join(" ", nameParts[..^1]); // Son kelime hariç tümü
            }
            else
            {
                // Tek kelime: ad = o kelime, soyad = UNKNOWN
                cariAd = nameParts.Length > 0 ? nameParts[0] : normalizedName;
                cariSoyad = "UNKNOWN";
                _logger.LogWarning("CariSoyad could not be extracted from single word name '{Name}' for Order {OrderId}, using UNKNOWN", 
                    normalizedName, order.Id);
            }
        }
        else
        {
            cariAd = $"Unknown Customer (Katana ID: {order.CustomerId})";
            cariSoyad = "UNKNOWN";
            _logger.LogWarning("CariAd/CariSoyad fallback used for order {OrderId}, Customer {CustomerId} has no Title or ContactPerson", 
                order.Id, order.CustomerId);
        }
        
        // Hard-guard: CariSoyad boşsa UNKNOWN ata
        if (string.IsNullOrWhiteSpace(cariSoyad))
        {
            cariSoyad = "UNKNOWN";
            _logger.LogWarning("Invoice CariSoyad was empty, fallback applied: {CariSoyad} for Order {OrderId}", cariSoyad, order.Id);
        }

        // CariTip hesaplama: VergiNo'dan sadece rakamları al
        // 11 hane = TCKN (şahıs) → CariTip=2
        // 10 hane = VKN (firma) → CariTip=1
        // Diğer durumlar → CariTip=1 (firma varsayılan)
        var vergiNoRaw = order.Customer?.TaxNo ?? "";
        var vergiNoDigits = System.Text.RegularExpressions.Regex.Replace(vergiNoRaw, @"[^\d]", "");
        int cariTip;
        if (vergiNoDigits.Length == 11)
        {
            cariTip = 2; // Şahıs (TCKN)
        }
        else if (vergiNoDigits.Length == 10)
        {
            cariTip = 1; // Firma (VKN)
        }
        else
        {
            cariTip = 1; // Fallback: Firma varsay
        }
        _logger.LogDebug("Invoice CariTip={CariTip} computed from VergiNo={VergiNo} (digits={VergiNoDigits}, len={Len}) for Order {OrderId}", 
            cariTip, vergiNoRaw, vergiNoDigits, vergiNoDigits.Length, order.Id);

        // VergiNo ZORUNLU fallback: Luca API "[vergiNo] alanı zorunludur" hatası veriyor
        // 1) VergiNo doluysa: sadece rakamları al
        // 2) Boşsa: cariKodu'ndan rakamları türet
        // 3) Hâlâ boşsa: "11111111111" dummy kullan (Luca accepted)
        string vergiNo;
        bool vergiNoFallbackUsed = false;
        
        if (!string.IsNullOrWhiteSpace(vergiNoDigits) && vergiNoDigits.Length >= 10)
        {
            // VergiNo geçerli, direkt kullan
            vergiNo = vergiNoDigits;
        }
        else
        {
            // Fallback 1: cariKodu'ndan rakamları türet
            var cariKoduDigits = System.Text.RegularExpressions.Regex.Replace(cariKodu ?? "", @"[^\d]", "");
            if (!string.IsNullOrWhiteSpace(cariKoduDigits) && cariKoduDigits.Length >= 10)
            {
                vergiNo = cariKoduDigits.Length > 11 ? cariKoduDigits.Substring(0, 11) : cariKoduDigits;
                vergiNoFallbackUsed = true;
            }
            else
            {
                // Fallback 2: Luca accepted dummy VKN
                vergiNo = "11111111111";
                vergiNoFallbackUsed = true;
            }
        }
        
        if (vergiNoFallbackUsed)
        {
            _logger.LogWarning("Invoice VergiNo was empty, fallback applied: {VergiNo} for Order {OrderId}, Customer {CustomerId}", 
                vergiNo, order.Id, order.CustomerId);
        }

        var request = new LucaCreateInvoiceHeaderRequest
        {
            BelgeSeri = belgeSeri,
            BelgeNo = belgeNoStr,
            BelgeTarihi = orderDate.ToString("dd/MM/yyyy"),
            VadeTarihi = orderDate.AddDays(30).ToString("dd/MM/yyyy"),
            BelgeAciklama = $"Katana Sales Order #{orderNo}",
            BelgeTurDetayId = belgeTurDetayId.ToString(),
            BelgeTakipNo = belgeTakipNo,
            FaturaTur = MAL_HIZMET.ToString(),
            ParaBirimKod = currency,
            // KurBedeli: Dövizli faturalar için zorunlu - TRY için 1, diğerleri için ConversionRate kullan
            KurBedeli = currency.ToUpperInvariant() == "TRY" ? 1.0 : (double)(order.ConversionRate ?? 1m),
            KdvFlag = false,
            MusteriTedarikci = MUSTERI.ToString(),
            CariKodu = cariKodu,
            CariAd = cariAd,
            CariSoyad = cariSoyad, // Koza API zorunlu alan
            CariKisaAd = Left50($"{cariAd} {cariSoyad}".Trim()),
            CariYasalUnvan = order.Customer?.Title ?? $"{cariAd} {cariSoyad}".Trim(),
            CariTanim = order.Customer?.Title,
            CariTip = cariTip, // Hesaplanan CariTip
            VergiNo = vergiNo, // ZORUNLU alan - fallback ile her zaman dolu
            TcKimlikNo = cariTip == 2 ? vergiNoDigits : null, // TCKN sadece şahıs için
            Il = order.Customer?.City ?? "ISTANBUL",
            Ilce = order.Customer?.District ?? "MERKEZ",
            GonderimTipi = "ELEKTRONIK",
            OdemeTipi = "DIGER",
            EfaturaTuru = 1,
            SiparisNo = orderNo,
            SiparisTarihi = orderDate,
            DetayList = new List<LucaCreateInvoiceDetailRequest>()
        };
        
        // Dövizli fatura için kur bilgisini logla
        if (currency.ToUpperInvariant() != "TRY")
        {
            _logger.LogInformation("Dövizli fatura oluşturuluyor: Order {OrderNo}, Currency={Currency}, KurBedeli={KurBedeli}", 
                orderNo, currency, request.KurBedeli);
        }

        // DepoKodu: LocationId'den mapping bul, yoksa default "001"
        var depoKodu = "001";
        if (order.LocationId.HasValue)
        {
            var warehouseMapping = await _context.MappingTables
                .Where(m => m.MappingType == "LOCATION_WAREHOUSE"
                    && m.SourceValue == order.LocationId.Value.ToString()
                    && m.IsActive)
                .Select(m => m.TargetValue)
                .FirstOrDefaultAsync();

            if (!string.IsNullOrWhiteSpace(warehouseMapping))
            {
                depoKodu = warehouseMapping;
            }
            else
            {
                _logger.LogWarning("LocationId {LocationId} için LOCATION_WAREHOUSE mapping bulunamadı, default '001' kullanılıyor",
                    order.LocationId.Value);
            }
        }

        // Satırları dönüştür - Varyant bilgilerini de dahil et
        foreach (var line in lines)
        {
            // Varyant mapping'den ürün bilgilerini çek
            var variantMapping = await _variantMappingService.GetMappingAsync(line.VariantId);
            Product? product = null;
            ProductVariant? productVariant = null;
            
            if (variantMapping != null)
            {
                product = await _context.Products.FirstOrDefaultAsync(p => p.Id == variantMapping.ProductId);
                if (variantMapping.ProductVariantId.HasValue)
                {
                    productVariant = await _context.ProductVariants.FirstOrDefaultAsync(pv => pv.Id == variantMapping.ProductVariantId.Value);
                }
            }
            
            // KartKodu'nu resolve et (varyant bilgilerini kullanarak)
            var kartKodu = LucaVariantMappingHelper.ResolveKartKodu(line, variantMapping, product, productVariant);
            
            // Fallback: Eğer hala boşsa ResolveParentStockCodeAsync kullan
            if (string.IsNullOrWhiteSpace(kartKodu))
            {
                kartKodu = await ResolveParentStockCodeAsync(line);
            }

            // Validation: Stok kodu
            var stokValidation = Validators.LucaDataValidator.ValidateStokKodu(kartKodu, line.ProductName);
            if (!stokValidation.IsValid)
            {
                _logger.LogWarning("Stock code validation failed for VariantId {VariantId}: {Error}",
                    line.VariantId, stokValidation.ErrorMessage);

                // Fallback: SKU kullan
                kartKodu = line.SKU ?? $"PRD-{line.VariantId:D5}";
                _logger.LogWarning("Fallback stok kodu kullanılıyor: {KartKodu}", kartKodu);
            }

            // Validation: Miktar
            var quantity = (int)line.Quantity;
            var qtyValidation = Validators.LucaDataValidator.ValidateQuantity(quantity, "Miktar");
            if (!qtyValidation.IsValid)
            {
                _logger.LogWarning("Product {ProductName} quantity validation failed: {Error}. Using fallback.", 
                    line.ProductName, qtyValidation.ErrorMessage);
                
                // Fallback: Minimum 1 quantity
                quantity = Math.Max(1, quantity);
            }

            // Validation: Birim fiyat
            var unitPrice = (decimal)line.PricePerUnit;
            var priceValidation = Validators.LucaDataValidator.ValidateDecimalPrecision(unitPrice, "Birim Fiyat");
            if (!priceValidation.IsValid)
            {
                _logger.LogWarning("Product {ProductName} price validation failed: {Error}. Using fallback.", 
                    line.ProductName, priceValidation.ErrorMessage);
                
                // Fallback: Round to 2 decimals
                unitPrice = Math.Round(unitPrice, 2);
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

            // Varyant bilgilerini içeren açıklama oluştur
            var aciklama = MappingHelper.BuildInvoiceLineDescriptionWithVariant(
                product?.Name ?? line.ProductName,
                productVariant?.SKU ?? line.SKU,
                productVariant?.Attributes);
            
            // KartAdi: Varyant varsa varyant bilgilerini ekle
            var kartAdi = line.ProductName;
            if (productVariant != null && !string.IsNullOrWhiteSpace(productVariant.Attributes))
            {
                kartAdi = $"{product?.Name ?? line.ProductName} ({productVariant.Attributes})";
            }

            request.DetayList.Add(new LucaCreateInvoiceDetailRequest
            {
                KartTuru = STOK_KARTI,
                KartKodu = kartKodu,
                KartAdi = kartAdi,
                Miktar = quantity,
                BirimFiyat = (double)unitPrice,
                KdvOran = taxRate,
                DepoKodu = depoKodu,
                Aciklama = aciklama,
                Barkod = productVariant?.Barcode ?? product?.Barcode
            });
        }

        // Son güvenlik kontrolü: Kritik alanları garanti altına al
        if (string.IsNullOrWhiteSpace(request.CariAd))
            validationErrors.Add("CariAd boş olamaz");
        if (string.IsNullOrWhiteSpace(request.CariKodu))
            validationErrors.Add("CariKodu boş olamaz");
        if (request.DetayList == null || !request.DetayList.Any())
            validationErrors.Add("DetayList boş olamaz");

        // Eğer kritik validation hataları varsa null dön
        if (validationErrors.Any())
        {
            _logger.LogError("Luca critical validation failed for Order {OrderNo}. Errors: {Errors}",
                orderNo, string.Join("; ", validationErrors));

            // Hataları audit log'a kaydet
            _auditService.LogAction(
                "OrderInvoiceSync",
                "SalesOrder",
                order.Id.ToString(),
                "System",
                $"Luca validation errors (critical): {string.Join("; ", validationErrors)}"
            );

            return null;
        }

        _logger.LogInformation("SalesOrder {OrderNo} converted to Luca invoice with fallback values where needed",
            orderNo);

        return request;
    }

    private async Task<string?> ResolveParentStockCodeAsync(SalesOrderLine line)
    {
        var mapping = await _variantMappingService.GetMappingAsync(line.VariantId);
        if (mapping != null)
        {
            var mappedSku = await _mappingRepo.GetLucaStokKoduByProductIdAsync(mapping.ProductId);
            if (!string.IsNullOrWhiteSpace(mappedSku))
            {
                return mappedSku;
            }
        }

        var baseSku = GetSkuBaseCode(line.SKU);
        if (!string.IsNullOrWhiteSpace(baseSku))
        {
            var productSku = await _context.Products
                .Where(p => p.SKU == baseSku)
                .Select(p => p.SKU)
                .FirstOrDefaultAsync();

            if (!string.IsNullOrWhiteSpace(productSku))
            {
                return productSku;
            }
        }

        return line.SKU;
    }

    private static string? GetSkuBaseCode(string? sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            return null;
        }

        var parts = sku.Split('-', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[0] : sku;
    }

    /// <summary>
    /// Sales Order'ı Luca fatura formatına çevirir (DEPRECATED - Order entity için)
    /// </summary>
    private async Task<LucaCreateInvoiceHeaderRequest?> BuildSalesInvoiceRequestAsync(Order order)
    {
        var validationErrors = new List<string>();
        const string entityType = "SalesOrder";

        // 1) mapping info çek
        var map = await _mappingRepo.GetMappingInfoAsync(order.Id, entityType);

        // 2) Belge alanlarını mapping'den al; yoksa appsettings'den al
        var belgeSeri = !string.IsNullOrWhiteSpace(map?.BelgeSeri) ? map!.BelgeSeri : _lucaSettings.DefaultBelgeSeri;

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

        // CariAd ve CariSoyad: ContactPerson veya Title'dan üret (DEPRECATED method)
        var rawNameSourceDep = !string.IsNullOrWhiteSpace(order.Customer?.ContactPerson) 
            ? order.Customer.ContactPerson 
            : order.Customer?.Title;
        
        string cariAdDep;
        string cariSoyadDep;
        
        if (!string.IsNullOrWhiteSpace(rawNameSourceDep))
        {
            var normalizedNameDep = System.Text.RegularExpressions.Regex.Replace(rawNameSourceDep.Trim(), @"\s+", " ");
            var namePartsDep = normalizedNameDep.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            if (namePartsDep.Length >= 2)
            {
                cariSoyadDep = namePartsDep[^1];
                cariAdDep = string.Join(" ", namePartsDep[..^1]);
            }
            else
            {
                cariAdDep = namePartsDep.Length > 0 ? namePartsDep[0] : normalizedNameDep;
                cariSoyadDep = "UNKNOWN";
            }
        }
        else
        {
            cariAdDep = $"Unknown Customer (Katana ID: {order.CustomerId})";
            cariSoyadDep = "UNKNOWN";
        }
        
        if (string.IsNullOrWhiteSpace(cariSoyadDep))
        {
            cariSoyadDep = "UNKNOWN";
            _logger.LogWarning("Invoice CariSoyad was empty (DEPRECATED), fallback applied: {CariSoyad}", cariSoyadDep);
        }

        // CariTip hesaplama (DEPRECATED method)
        var taxNoRawDep = order.Customer?.TaxNo ?? "";
        var taxNoDigitsDep = System.Text.RegularExpressions.Regex.Replace(taxNoRawDep, @"[^\d]", "");
        int cariTipDep;
        if (taxNoDigitsDep.Length == 11)
        {
            cariTipDep = 2; // Şahıs (TCKN)
        }
        else if (taxNoDigitsDep.Length == 10)
        {
            cariTipDep = 1; // Firma (VKN)
        }
        else
        {
            cariTipDep = 1; // Fallback: Firma varsay
        }
        _logger.LogInformation("Invoice CariTip computed as {CariTip} from TaxNo='{TaxNoRaw}' digitsLen={Len} (DEPRECATED)", 
            cariTipDep, taxNoRawDep, taxNoDigitsDep.Length);

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
            CariAd = cariAdDep, // Koza API zorunlu alan
            CariSoyad = cariSoyadDep, // Koza API zorunlu alan
            CariTip = cariTipDep, // Hesaplanan CariTip
            VergiNo = cariTipDep == 1 ? taxNoDigitsDep : null, // VKN sadece firma için
            TcKimlikNo = cariTipDep == 2 ? taxNoDigitsDep : null, // TCKN sadece şahıs için
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
            if (existingLucaId.HasValue && existingLucaId.Value > 0)
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
            var pendingOrders = await _context.SalesOrders
                .Where(o => !o.IsSyncedToLuca && (o.Status == "DELIVERED" || o.Status == "APPROVED"))
                .OrderBy(o => o.OrderCreatedDate ?? o.CreatedAt)
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
