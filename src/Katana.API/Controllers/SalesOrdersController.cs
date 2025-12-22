using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Katana.Core.Entities;
using Katana.Core.Enums;
using Katana.Core.Helpers;
using Katana.Core.Interfaces;
using Katana.Business.Services;
using Katana.Data.Context;
using Katana.Data.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace Katana.API.Controllers;

[Authorize]
[ApiController]
[Route("api/sales-orders")]
public class SalesOrdersController : ControllerBase
    {
        private readonly IntegrationDbContext _context;
        private readonly ILucaService _lucaService;
        private readonly ILoggingService _loggingService;
        private readonly IAuditService _auditService;
        private readonly ILogger<SalesOrdersController> _logger;
        private readonly IKatanaService _katanaService;
        private readonly ILocationMappingService _locationMappingService;
        private readonly IOptions<LucaApiSettings> _lucaSettings;

        public SalesOrdersController(
            IntegrationDbContext context,
            ILucaService lucaService,
            ILoggingService loggingService,
            IAuditService auditService,
            ILogger<SalesOrdersController> logger,
            IKatanaService katanaService,
            ILocationMappingService locationMappingService,
            IOptions<LucaApiSettings> lucaSettings)
        {
            _context = context;
            _lucaService = lucaService;
            _loggingService = loggingService;
            _auditService = auditService;
            _logger = logger;
            _katanaService = katanaService;
            _locationMappingService = locationMappingService;
            _lucaSettings = lucaSettings;
        }

    /// <summary>
    /// Tüm satış siparişlerini listele
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SalesOrderSummaryDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? status = null,
        [FromQuery] string? syncStatus = null)
    {
        var query = _context.SalesOrders
            .Include(s => s.Customer)
            .AsQueryable();

        // Filter by status
        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(s => s.Status == status);
        }

        // Filter by sync status
        if (!string.IsNullOrEmpty(syncStatus))
        {
            query = syncStatus switch
            {
                "synced" => query.Where(s => s.IsSyncedToLuca && string.IsNullOrEmpty(s.LastSyncError)),
                "error" => query.Where(s => !string.IsNullOrEmpty(s.LastSyncError)),
                "not_synced" => query.Where(s => !s.IsSyncedToLuca && string.IsNullOrEmpty(s.LastSyncError)),
                _ => query
            };
        }

        var orders = await query
            .OrderByDescending(s => s.OrderCreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SalesOrderSummaryDto
            {
                Id = s.Id,
                OrderNo = s.OrderNo,
                CustomerName = s.Customer != null ? s.Customer.Title : null,
                OrderCreatedDate = s.OrderCreatedDate,
                Status = s.Status,
                Currency = s.Currency,
                Total = s.Total,
                LucaOrderId = s.LucaOrderId,
                IsSyncedToLuca = s.IsSyncedToLuca,
                LastSyncError = s.LastSyncError,
                LastSyncAt = s.LastSyncAt
            })
            .ToListAsync();

        return Ok(orders);
    }

    /// <summary>
    /// Satış siparişi detayını getir
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<LocalSalesOrderDto>> GetById(int id)
    {
        var order = await _context.SalesOrders
            .Include(s => s.Customer)
            .Include(s => s.Lines)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (order == null)
            return NotFound($"Sipariş bulunamadı: {id}");

        var dto = MapToDto(order);
        return Ok(dto);
    }

    /// <summary>
    /// Luca alanlarını güncelle (BelgeSeri, DuzenlemeSaati, OnayFlag vb.)
    /// </summary>
    [HttpPatch("{id}/luca-fields")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<LocalSalesOrderDto>> UpdateLucaFields(int id, [FromBody] UpdateSalesOrderLucaFieldsDto dto)
    {
        var order = await _context.SalesOrders
            .Include(s => s.Customer)
            .Include(s => s.Lines)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (order == null)
            return NotFound($"Sipariş bulunamadı: {id}");

        // Update Luca fields
        if (dto.BelgeSeri != null) order.BelgeSeri = dto.BelgeSeri;
        if (dto.BelgeNo != null) order.BelgeNo = dto.BelgeNo;
        if (dto.DuzenlemeSaati != null) order.DuzenlemeSaati = dto.DuzenlemeSaati;
        if (dto.BelgeTurDetayId.HasValue) order.BelgeTurDetayId = dto.BelgeTurDetayId;
        if (dto.NakliyeBedeliTuru.HasValue) order.NakliyeBedeliTuru = dto.NakliyeBedeliTuru;
        if (dto.TeklifSiparisTur.HasValue) order.TeklifSiparisTur = dto.TeklifSiparisTur;
        if (dto.OnayFlag.HasValue) order.OnayFlag = dto.OnayFlag.Value;

        order.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _auditService.LogUpdate("SalesOrder", id.ToString(), User?.Identity?.Name ?? "system", null,
            "Luca fields updated");
        _loggingService.LogInfo($"SalesOrder {id} Luca fields updated", User?.Identity?.Name, null, LogCategory.UserAction);

        return Ok(MapToDto(order));
    }

    /// <summary>
    /// Siparişi Luca'ya manuel senkronize et
    /// </summary>
    [HttpPost("{id}/sync")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SalesOrderSyncResultDto>> SyncToLuca(
        int id,
        [FromBody] UpdateSalesOrderLucaFieldsDto? lucaFields = null)
    {
        var order = await _context.SalesOrders
            .Include(s => s.Customer)
            .Include(s => s.Lines)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (order == null)
            return NotFound($"Sipariş bulunamadı: {id}");

        if (order.Customer == null)
            return BadRequest(new { message = "Müşteri bilgisi eksik. Siparişe müşteri atanmamış." });

        if (order.Lines == null || order.Lines.Count == 0)
        {
            return BadRequest(new { message = "Sipariş satırları bulunamadı. Katana'dan tekrar senkronize edin." });
        }

        // ===== MÜŞTERİ VERİSİ VALİDASYONU =====
        var customer = order.Customer;
        var validationErrors = new List<string>();

        // Cari kodu / vergi no kontrolü (DB'yi değiştirmeden, Luca için sanitize et)
        string? customerCode = null;
        string? sanitizedTaxNo = null;

        if (!string.IsNullOrWhiteSpace(customer.LucaCode))
        {
            customerCode = customer.LucaCode.Trim();
        }

        var taxNoDigits = string.IsNullOrWhiteSpace(customer.TaxNo)
            ? string.Empty
            : new string(customer.TaxNo.Where(char.IsDigit).ToArray());

        if (taxNoDigits.Length == 10 || taxNoDigits.Length == 11)
        {
            sanitizedTaxNo = taxNoDigits;
            if (string.IsNullOrWhiteSpace(customerCode))
                customerCode = taxNoDigits;
        }

        if (string.IsNullOrWhiteSpace(customerCode))
        {
            // Benzersiz, geçici bir cari kod üret (DB’ye yazmayacağız): TEMP-{CustomerId}
            customerCode = $"TEMP-{customer.Id}";
            sanitizedTaxNo ??= $"999{customer.Id:D8}";
            _logger.LogWarning("SyncToLuca: Müşteri vergi no/cari kod eksik. CustomerId={CustomerId}, RawTax='{Raw}', GeneratedCode={GeneratedCode}, GeneratedTaxNo={GeneratedTaxNo}",
                customer.Id, customer.TaxNo, customerCode, sanitizedTaxNo);
        }
        else if (customerCode.StartsWith("CUST", StringComparison.OrdinalIgnoreCase))
        {
            validationErrors.Add($"Geçersiz cari kodu: '{customerCode}'. Müşteriye geçerli bir Vergi No veya Luca Cari Kodu atayın");
        }

        // Müşteri adı kontrolü
        if (string.IsNullOrWhiteSpace(customer.Title))
        {
            validationErrors.Add("Müşteri adı/unvanı eksik");
        }

        if (validationErrors.Count > 0)
        {
            return BadRequest(new { 
                message = "Müşteri verisi eksik veya geçersiz", 
                errorDetails = string.Join("; ", validationErrors),
                customerId = customer.Id,
                customerTitle = customer.Title
            });
        }

        // ===== DEPO KODU VALİDASYONU =====
        var depoKodu = await _locationMappingService.GetDepoKoduByLocationIdAsync(order.LocationId?.ToString() ?? string.Empty);
        
        // Depo kodu uyarısı logla (default kullanılıyorsa)
        if (string.IsNullOrWhiteSpace(order.LocationId?.ToString()))
        {
            _logger.LogWarning("Sipariş {OrderNo} için LocationId boş, varsayılan depo kullanılıyor: {DepoKodu}", 
                order.OrderNo, depoKodu);
        }

        // Duplikasyon kontrolü - Zaten senkronize edilmiş ve hata yoksa reddet
        if (order.IsSyncedToLuca && string.IsNullOrEmpty(order.LastSyncError))
        {
            return BadRequest(new { message = "Sipariş zaten Koza'ya senkronize edilmiş", lucaOrderId = order.LucaOrderId });
        }

        // ===== DÖVİZ KURU VALİDASYONU =====
        var currency = string.IsNullOrWhiteSpace(order.Currency) ? "TRY" : order.Currency.ToUpperInvariant();
        if (currency != "TRY" && (!order.ConversionRate.HasValue || order.ConversionRate <= 0))
        {
            _logger.LogWarning("Dövizli sipariş {OrderNo} için kur bilgisi eksik. Currency={Currency}, ConversionRate={Rate}", 
                order.OrderNo, currency, order.ConversionRate);
            // Uyarı ver ama devam et - Koza tarafında hata alınırsa kullanıcı görecek
        }

        // If UI sends Luca fields together with the sync request, apply them first so the outgoing payload matches UI.
        if (lucaFields != null)
        {
            if (!string.IsNullOrWhiteSpace(lucaFields.BelgeSeri))
            {
                order.BelgeSeri = lucaFields.BelgeSeri.Trim();
            }
            if (lucaFields.BelgeNo != null)
            {
                order.BelgeNo = string.IsNullOrWhiteSpace(lucaFields.BelgeNo) ? null : lucaFields.BelgeNo.Trim();
            }
            if (lucaFields.DuzenlemeSaati != null)
            {
                order.DuzenlemeSaati = string.IsNullOrWhiteSpace(lucaFields.DuzenlemeSaati) ? null : lucaFields.DuzenlemeSaati.Trim();
            }
            if (lucaFields.BelgeTurDetayId.HasValue) order.BelgeTurDetayId = lucaFields.BelgeTurDetayId;
            if (lucaFields.NakliyeBedeliTuru.HasValue) order.NakliyeBedeliTuru = lucaFields.NakliyeBedeliTuru;
            if (lucaFields.TeklifSiparisTur.HasValue) order.TeklifSiparisTur = lucaFields.TeklifSiparisTur;
            if (lucaFields.OnayFlag.HasValue) order.OnayFlag = lucaFields.OnayFlag.Value;
            if (lucaFields.BelgeAciklama != null) order.AdditionalInfo = lucaFields.BelgeAciklama;
        }

        // Luca’ya giderken kullanılacak belgeSeri/null normalize et (DB’yi güncellemeden)
        var effectiveBelgeSeri = !string.IsNullOrWhiteSpace(order.BelgeSeri)
            ? order.BelgeSeri.Trim()
            : (_lucaSettings.Value.DefaultBelgeSeri ?? "EFA2025");

        // Luca'ya gönderirken müşteri/sipariş bilgilerini sanitize edip kopyala (DB state'ini etkilemeden)
        var safeCustomer = new Customer
        {
            Id = customer.Id,
            Title = customer.Title,
            LucaCode = customerCode,
            TaxNo = sanitizedTaxNo ?? customer.TaxNo,
            TaxOffice = customer.TaxOffice,
            Address = customer.Address,
            City = customer.City,
            District = customer.District,
            Country = customer.Country,
            Phone = customer.Phone,
            Email = customer.Email,
            Currency = customer.Currency,
            IsActive = customer.IsActive
        };

        var safeOrder = new SalesOrder
        {
            Id = order.Id,
            OrderNo = order.OrderNo,
            Customer = safeCustomer,
            CustomerId = order.CustomerId,
            OrderCreatedDate = order.OrderCreatedDate,
            DeliveryDate = order.DeliveryDate,
            Currency = order.Currency,
            ConversionRate = order.ConversionRate,
            Status = order.Status,
            Total = order.Total,
            TotalInBaseCurrency = order.TotalInBaseCurrency,
            AdditionalInfo = order.AdditionalInfo,
            CustomerRef = order.CustomerRef,
            Source = order.Source,
            LocationId = order.LocationId,
            BelgeSeri = effectiveBelgeSeri,
            BelgeNo = order.BelgeNo,
            DuzenlemeSaati = order.DuzenlemeSaati,
            BelgeTurDetayId = order.BelgeTurDetayId,
            NakliyeBedeliTuru = order.NakliyeBedeliTuru,
            TeklifSiparisTur = order.TeklifSiparisTur,
            OnayFlag = order.OnayFlag,
            Lines = order.Lines.Select(l => new SalesOrderLine
            {
                Id = l.Id,
                SalesOrderId = l.SalesOrderId,
                KatanaRowId = l.KatanaRowId,
                VariantId = l.VariantId,
                SKU = l.SKU,
                ProductName = l.ProductName,
                Quantity = l.Quantity,
                PricePerUnit = l.PricePerUnit,
                PricePerUnitInBaseCurrency = l.PricePerUnitInBaseCurrency,
                Total = l.Total,
                TotalInBaseCurrency = l.TotalInBaseCurrency,
                TaxRate = l.TaxRate,
                TaxRateId = l.TaxRateId,
                LocationId = l.LocationId,
                ProductAvailability = l.ProductAvailability,
                ProductExpectedDate = l.ProductExpectedDate,
                LucaDetayId = l.LucaDetayId,
                LucaStokId = l.LucaStokId,
                LucaDepoId = l.LucaDepoId,
                CreatedAt = l.CreatedAt,
                UpdatedAt = l.UpdatedAt
            }).ToList()
        };

        // Dış entegrasyon çağrısı (Luca) retry stratejisinin içine sokulmaz: transient DB retry durumunda Luca'ya duplicate gitmesin.
        var lucaResult = await _lucaService.CreateSalesOrderInvoiceAsync(safeOrder, depoKodu);

        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var current = await _context.SalesOrders.FirstOrDefaultAsync(s => s.Id == id);
                if (current == null)
                    return NotFound($"Sipariş bulunamadı: {id}");

                current.LastSyncAt = DateTime.UtcNow;
                current.UpdatedAt = DateTime.UtcNow;

                if (lucaResult.IsSuccess && lucaResult.LucaOrderId.HasValue)
                {
                    current.IsSyncedToLuca = true;
                    current.LucaOrderId = lucaResult.LucaOrderId;
                    current.LastSyncError = null;
                    await _context.SaveChangesAsync();
                    await tx.CommitAsync();

                    _logger.LogInformation("SalesOrder synced to Luca. OrderId={OrderId}, OrderNo={OrderNo}, LucaId={LucaId}", id, order.OrderNo, lucaResult.LucaOrderId);
                    _loggingService.LogInfo($"SalesOrder {id} synced to Luca (invoice): {lucaResult.LucaOrderId}",
                        User?.Identity?.Name, null, LogCategory.Business);

                    return Ok(new SalesOrderSyncResultDto
                    {
                        IsSuccess = true,
                        Message = "Luca'ya başarıyla senkronize edildi",
                        LucaOrderId = lucaResult.LucaOrderId,
                        SyncedAt = current.LastSyncAt
                    });
                }

                current.IsSyncedToLuca = false;
                current.LastSyncError = lucaResult.ErrorDetails ?? "Luca satış faturası oluşturma başarısız";
                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                _logger.LogError("Luca sync failed. OrderId={OrderId}, OrderNo={OrderNo}, Error={Error}", id, order.OrderNo, current.LastSyncError);
                _loggingService.LogWarning($"Luca sales order sync failed: {current.LastSyncError}",
                    User?.Identity?.Name, $"SalesOrderId={id}", LogCategory.Business);

                return BadRequest(new SalesOrderSyncResultDto
                {
                    IsSuccess = false,
                    Message = "Luca entegrasyon hatası",
                    ErrorDetails = current.LastSyncError,
                    SyncedAt = current.LastSyncAt
                });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "SyncToLuca Critical Error OrderId: {OrderId}", id);
                return StatusCode(500, new SalesOrderSyncResultDto
                {
                    IsSuccess = false,
                    Message = "Entegrasyon sırasında kritik hata.",
                    ErrorDetails = ex.Message
                });
            }
        });
    }

    /// <summary>
    /// Senkronizasyon durumunu getir
    /// </summary>
    [HttpGet("{id}/sync-status")]
    public async Task<ActionResult<SalesOrderSyncStatusDto>> GetSyncStatus(int id)
    {
        var order = await _context.SalesOrders
            .AsNoTracking()
            .Select(s => new SalesOrderSyncStatusDto
            {
                SalesOrderId = s.Id,
                LucaOrderId = s.LucaOrderId,
                IsSyncedToLuca = s.IsSyncedToLuca,
                LastSyncAt = s.LastSyncAt,
                LastSyncError = s.LastSyncError,
                Status = s.IsSyncedToLuca && string.IsNullOrEmpty(s.LastSyncError)
                    ? "synced"
                    : (!string.IsNullOrEmpty(s.LastSyncError) ? "error" : "not_synced")
            })
            .FirstOrDefaultAsync(s => s.SalesOrderId == id);

        if (order == null)
            return NotFound($"Sipariş bulunamadı: {id}");

        return Ok(order);
    }

    /// <summary>
    /// Toplu senkronizasyon (senkronize edilmemiş siparişleri Luca'ya gönder)
    /// Paralel batch processing ile performans optimizasyonu
    /// </summary>
    [HttpPost("sync-all")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<object>> SyncAllPending([FromQuery] int maxCount = 50)
    {
        // ✅ Performance metrics tracking
        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        var pendingOrders = await _context.SalesOrders
            .Include(s => s.Customer)
            .Include(s => s.Lines)
            .Where(s => !s.IsSyncedToLuca && string.IsNullOrEmpty(s.LastSyncError))
            .Take(maxCount)
            .ToListAsync();

        int successCount = 0;
        int failCount = 0;
        var errors = new ConcurrentBag<object>();

        var locationToDepo = await _locationMappingService.GetLocationToDepoKoduMapAsync();
        const string defaultDepoKodu = "001";

        // DbContext thread-safe değil: paralelde sadece HTTP çağrısı yap, EF entity güncellemesini tek thread'de yap.
        var semaphore = new SemaphoreSlim(5);
        var tasks = pendingOrders.Select(async order =>
        {
            await semaphore.WaitAsync();
            try
            {
                if (order.Customer == null)
                {
                    return (OrderId: order.Id, OrderNo: order.OrderNo, Success: false, LucaOrderId: (int?)null, Error: "Customer is null");
                }

                if (order.Lines == null || order.Lines.Count == 0)
                {
                    return (OrderId: order.Id, OrderNo: order.OrderNo, Success: false, LucaOrderId: (int?)null, Error: "Order has no lines");
                }

                var depoKey = order.LocationId?.ToString() ?? string.Empty;
                var depoKodu = (!string.IsNullOrWhiteSpace(depoKey) && locationToDepo.TryGetValue(depoKey, out var mappedDepo) && !string.IsNullOrWhiteSpace(mappedDepo))
                    ? mappedDepo
                    : defaultDepoKodu;

                var lucaResult = await _lucaService.CreateSalesOrderInvoiceAsync(order, depoKodu);
                if (!lucaResult.IsSuccess || !lucaResult.LucaOrderId.HasValue)
                {
                    var msg = lucaResult.ErrorDetails ?? "Luca satış faturası oluşturma başarısız (id dönmedi)";
                    return (OrderId: order.Id, OrderNo: order.OrderNo, Success: false, LucaOrderId: (int?)null, Error: msg);
                }

                return (OrderId: order.Id, OrderNo: order.OrderNo, Success: true, LucaOrderId: lucaResult.LucaOrderId, Error: (string?)null);
            }
            catch (Exception ex)
            {
                return (OrderId: order.Id, OrderNo: order.OrderNo, Success: false, LucaOrderId: (int?)null, Error: ex.Message);
            }
            finally
            {
                semaphore.Release();
            }
        }).ToList();

        var results = await Task.WhenAll(tasks);

        var now = DateTime.UtcNow;
        foreach (var r in results)
        {
            var order = pendingOrders.First(o => o.Id == r.OrderId);
            order.LastSyncAt = now;

            if (r.Success)
            {
                order.LucaOrderId = r.LucaOrderId;
                order.IsSyncedToLuca = true;
                order.LastSyncError = null;
                successCount++;
            }
            else
            {
                order.IsSyncedToLuca = false;
                order.LastSyncError = r.Error;
                failCount++;
                errors.Add(new { OrderId = r.OrderId, OrderNo = r.OrderNo, Error = r.Error });
            }
        }

        await _context.SaveChangesAsync();
        
        sw.Stop();

        // ✅ Performance metrics logging
        var rate = successCount > 0 ? successCount * 60000.0 / sw.ElapsedMilliseconds : 0;
        _logger.LogInformation(
            "Batch sync completed: {Success}/{Total} orders, Duration: {Duration}ms, Rate: {Rate:F2} orders/min, Parallelism: 5x",
            successCount, pendingOrders.Count, sw.ElapsedMilliseconds, rate);

        _loggingService.LogInfo($"Bulk sync completed: {successCount} success, {failCount} failed, {sw.ElapsedMilliseconds}ms", 
            User?.Identity?.Name, null, LogCategory.Business);

        return Ok(new
        {
            TotalProcessed = pendingOrders.Count,
            SuccessCount = successCount,
            FailCount = failCount,
            DurationMs = sw.ElapsedMilliseconds,
            RateOrdersPerMinute = rate,
            Errors = errors
        });
    }

    /// <summary>
    /// Sipariş istatistikleri
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<object>> GetStats()
    {
        var stats = await _context.SalesOrders
            .GroupBy(s => 1)
            .Select(g => new
            {
                TotalOrders = g.Count(),
                SyncedOrders = g.Count(s => s.IsSyncedToLuca && string.IsNullOrEmpty(s.LastSyncError)),
                ErrorOrders = g.Count(s => !string.IsNullOrEmpty(s.LastSyncError)),
                PendingOrders = g.Count(s => !s.IsSyncedToLuca && string.IsNullOrEmpty(s.LastSyncError)),
                TotalValue = g.Sum(s => s.Total ?? 0)
            })
            .FirstOrDefaultAsync();

        return Ok(stats ?? new
        {
            TotalOrders = 0,
            SyncedOrders = 0,
            ErrorOrders = 0,
            PendingOrders = 0,
            TotalValue = 0m
        });
    }

    /// <summary>
    /// Admin onayı - Siparişi onayla ve Katana'da stok artırımı yap (Stock Adjustment / ürün oluşturma).
    /// Not: Siparişler Katana'dan geldiği için Katana'da yeni Sales Order oluşturulmaz.
    /// </summary>
    [HttpPost("{id}/approve")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult> ApproveOrder(int id)
    {
        static string Truncate(string value, int maxLen)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLen ? value : value.Substring(0, maxLen);
        }

        try
        {
            var order = await _context.SalesOrders
                .Include(s => s.Lines)
                .Include(s => s.Customer)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (order == null)
                return NotFound(new { success = false, message = $"Sipariş bulunamadı: {id}" });

            if (order.Status == "APPROVED" || order.Status == "SHIPPED")
                return BadRequest(new { success = false, message = "Bu sipariş zaten onaylanmış" });

            if (order.Lines == null || order.Lines.Count == 0)
            {
                _logger.LogWarning("SalesOrder approve requested but order has no lines. OrderId={OrderId}, OrderNo={OrderNo}", id, order.OrderNo);
                return BadRequest(new
                {
                    success = false,
                    message = "Sipariş satırları bulunamadı. Katana'dan tekrar senkronize edin (Siparişler > Katana'dan Çek).",
                    orderNo = order.OrderNo
                });
            }

            var linesToSync = order.Lines
                .Where(l => !string.IsNullOrWhiteSpace(l.SKU) && l.Quantity > 0)
                .ToList();
            if (linesToSync.Count == 0)
            {
                _logger.LogWarning("SalesOrder approve requested but order has no valid lines (SKU/Quantity). OrderId={OrderId}, OrderNo={OrderNo}", id, order.OrderNo);
                return BadRequest(new
                {
                    success = false,
                    message = "Katana'ya stok güncellemesi için geçerli sipariş satırı bulunamadı (SKU/Quantity kontrol edin).",
                    orderNo = order.OrderNo
                });
            }

            _logger.LogInformation("Approving SalesOrder: Katana stock adjustment. OrderId={OrderId}, OrderNo={OrderNo}, LineCount={LineCount}", id, order.OrderNo, order.Lines.Count);
            _loggingService.LogInfo($"SalesOrder approve started (Katana stock adjustment): {order.OrderNo} (lines={order.Lines.Count})",
                User.Identity?.Name, $"SalesOrderId={id}", LogCategory.Business);

            // Katana'da stok artırımı yap (her satır için). Bu işlem idempotent değildir; aynı sipariş tekrar onaylanmamalıdır.
            var successCount = 0;
            var failCount = 0;
            var lineResults = new List<(string Sku, decimal Quantity, bool Success, string? Error)>();

            try
            {
                foreach (var line in linesToSync)
                {
                    try
                    {
                        var ok = await _katanaService.SyncProductStockAsync(
                            sku: line.SKU,
                            quantity: line.Quantity,
                            locationId: line.LocationId ?? order.LocationId,
                            productName: !string.IsNullOrWhiteSpace(line.ProductName) ? line.ProductName : line.SKU,
                            salesPrice: line.PricePerUnit
                        );

                        if (ok)
                        {
                            successCount++;
                            lineResults.Add((line.SKU, line.Quantity, true, null));
                        }
                        else
                        {
                            failCount++;
                            var err = $"Stok artışı/ürün oluşturma başarısız: {line.SKU}";
                            lineResults.Add((line.SKU, line.Quantity, false, err));
                            _logger.LogWarning("ApproveOrder: {Error}. OrderId={OrderId}, OrderNo={OrderNo}", err, id, order.OrderNo);
                        }
                    }
                    catch (Exception exLine)
                    {
                        failCount++;
                        lineResults.Add((line.SKU, line.Quantity, false, exLine.Message));
                        _logger.LogWarning(exLine, "ApproveOrder: Stock sync failed. OrderId={OrderId}, OrderNo={OrderNo}, SKU={SKU}", id, order.OrderNo, line.SKU);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ApproveOrder Katana sync failed. OrderId={OrderId}", id);
            }

            // Veritabanını güncelle (onay durumu). Luca senkronizasyon sonucu ayrı güncellenir.
            try
            {
                var strategy = _context.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    await using var tx = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        var isSuccess = failCount == 0 && successCount > 0;
                        order.Status = isSuccess ? "APPROVED" : "APPROVED_WITH_ERRORS";

                        order.UpdatedAt = DateTime.UtcNow;

                        await _context.SaveChangesAsync();
                        await tx.CommitAsync();
                    }
                    catch
                    {
                        await tx.RollbackAsync();
                        throw;
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SalesOrder approve failed while persisting result. OrderId={OrderId}", id);
                _loggingService.LogError($"SalesOrder approve DB update failed: {id}", ex, User.Identity?.Name, null, LogCategory.Business);
                return StatusCode(500, new { success = false, message = "Onay sonucu kaydedilirken hata oluştu." });
            }

            // ✅ Onay sonrası Luca'ya senkronizasyon (fatura)
            // Not: Luca çağrısı DB retry stratejisinin dışında kalmalı; aksi halde transient DB retry sırasında duplicate fatura oluşabilir.
            SalesOrderSyncResultDto? lucaSync = null;
            var lucaSkippedReason = (string?)null;

            if (failCount == 0 && successCount > 0)
            {
                if (order.IsSyncedToLuca && string.IsNullOrEmpty(order.LastSyncError))
                {
                    lucaSkippedReason = $"Sipariş zaten Koza'ya senkronize edilmiş (LucaId={order.LucaOrderId})";
                }
                else if (order.Customer == null)
                {
                    lucaSkippedReason = "Müşteri bilgisi eksik";
                }
                else
                {
                    var validationErrors = new List<string>();
                    var customer = order.Customer;
                    var customerCode = !string.IsNullOrWhiteSpace(customer.LucaCode) ? customer.LucaCode : customer.TaxNo;

                    if (string.IsNullOrWhiteSpace(customerCode))
                    {
                        validationErrors.Add("Müşterinin Vergi No veya Luca Cari Kodu eksik");
                    }
                    else if (customerCode.StartsWith("CUST", StringComparison.OrdinalIgnoreCase))
                    {
                        validationErrors.Add($"Geçersiz cari kodu: '{customerCode}'. Müşteriye geçerli bir Vergi No veya Luca Cari Kodu atayın");
                    }

                    if (!string.IsNullOrWhiteSpace(customer.TaxNo))
                    {
                        var taxNoDigits = new string(customer.TaxNo.Where(char.IsDigit).ToArray());
                        if (taxNoDigits.Length != 10 && taxNoDigits.Length != 11)
                        {
                            validationErrors.Add($"Geçersiz Vergi No/TC Kimlik No formatı: '{customer.TaxNo}' (10 veya 11 haneli olmalı)");
                        }
                    }

                    if (string.IsNullOrWhiteSpace(customer.Title))
                    {
                        validationErrors.Add("Müşteri adı/unvanı eksik");
                    }

                    if (validationErrors.Count > 0)
                    {
                        lucaSkippedReason = string.Join("; ", validationErrors);
                    }
                    else
                    {
                        var depoKodu = await _locationMappingService.GetDepoKoduByLocationIdAsync(order.LocationId?.ToString() ?? string.Empty);
                        lucaSync = await _lucaService.CreateSalesOrderInvoiceAsync(order, depoKodu);

                        var strategy = _context.Database.CreateExecutionStrategy();
                        await strategy.ExecuteAsync(async () =>
                        {
                            await using var tx = await _context.Database.BeginTransactionAsync();
                            try
                            {
                                var current = await _context.SalesOrders.FirstOrDefaultAsync(s => s.Id == id);
                                if (current == null) return;

                                current.LastSyncAt = DateTime.UtcNow;
                                current.UpdatedAt = DateTime.UtcNow;

                                if (lucaSync.IsSuccess && lucaSync.LucaOrderId.HasValue)
                                {
                                    current.IsSyncedToLuca = true;
                                    current.LucaOrderId = lucaSync.LucaOrderId;
                                    current.LastSyncError = null;
                                }
                                else
                                {
                                    current.IsSyncedToLuca = false;
                                    current.LastSyncError = Truncate(lucaSync.ErrorDetails ?? "Luca satış faturası oluşturma başarısız", 950);
                                }

                                await _context.SaveChangesAsync();
                                await tx.CommitAsync();
                            }
                            catch
                            {
                                await tx.RollbackAsync();
                                throw;
                            }
                        });
                    }
                }
            }
            else
            {
                lucaSkippedReason = "Katana stok güncellemesi kısmi başarısız olduğu için Luca senkronu atlandı";
            }

            _auditService.LogUpdate(
                "SalesOrder",
                id.ToString(),
                User.Identity?.Name ?? "System",
                null,
                failCount == 0 && successCount > 0
                    ? $"Sipariş onaylandı ve Katana stok güncellendi (KatanaOrderId={order.KatanaOrderId})"
                    : $"Sipariş onaylandı ama bazı stok güncellemeleri başarısız oldu (KatanaOrderId={order.KatanaOrderId})");

            _logger.LogInformation("SalesOrder approval completed. OrderId={OrderId}, OrderNo={OrderNo}, KatanaOrderId={KatanaOrderId}, Status={Status}, Success={SuccessCount}, Fail={FailCount}",
                id, order.OrderNo, order.KatanaOrderId, order.Status, successCount, failCount);
            _loggingService.LogInfo($"SalesOrder approve completed: {order.OrderNo} (katanaOrderId={order.KatanaOrderId}, status={order.Status}, success={successCount}, fail={failCount})",
                User.Identity?.Name, $"SalesOrderId={id}", LogCategory.Business);

            object lucaSyncPayload = lucaSync != null
                ? new { attempted = true, isSuccess = lucaSync.IsSuccess, lucaOrderId = lucaSync.LucaOrderId, message = lucaSync.Message, errorDetails = lucaSync.ErrorDetails }
                : new { attempted = false, reason = lucaSkippedReason };

            return Ok(new
            {
                success = failCount == 0 && successCount > 0,
                message = (failCount == 0 && successCount > 0)
                    ? "Sipariş onaylandı"
                    : $"Sipariş onaylandı ancak Katana stok güncellemesi kısmi başarısız oldu (başarılı={successCount}, hatalı={failCount}).",
                orderNo = order.OrderNo,
                orderStatus = order.Status,
                katanaOrderId = order.KatanaOrderId,
                successCount,
                failCount,
                syncResults = lineResults.Select(r => new { sku = r.Sku, quantity = r.Quantity, success = r.Success, error = r.Error }).ToList(),
                lucaSync = lucaSyncPayload
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ApproveOrder critical error. OrderId={OrderId}", id);
            _loggingService.LogError($"ApproveOrder critical error: {id}", ex, User.Identity?.Name, null, LogCategory.Business);
            return StatusCode(500, new { success = false, message = "Sunucu hatası oluştu.", error = ex.Message });
        }
    }

    private static LocalSalesOrderDto MapToDto(SalesOrder order)
    {
        return new LocalSalesOrderDto
        {
            Id = order.Id,
            KatanaOrderId = order.KatanaOrderId,
            OrderNo = order.OrderNo,
            CustomerId = order.CustomerId,
            CustomerName = order.Customer?.Title,
            OrderCreatedDate = order.OrderCreatedDate,
            DeliveryDate = order.DeliveryDate,
            Currency = order.Currency,
            Status = order.Status,
            Total = order.Total,
            TotalInBaseCurrency = order.TotalInBaseCurrency,
            AdditionalInfo = order.AdditionalInfo,
            CustomerRef = order.CustomerRef,
            Source = order.Source,
            LocationId = order.LocationId,
            LucaOrderId = order.LucaOrderId,
            BelgeSeri = order.BelgeSeri,
            BelgeNo = order.BelgeNo,
            DuzenlemeSaati = order.DuzenlemeSaati,
            BelgeTurDetayId = order.BelgeTurDetayId,
            NakliyeBedeliTuru = order.NakliyeBedeliTuru,
            TeklifSiparisTur = order.TeklifSiparisTur,
            OnayFlag = order.OnayFlag,
            LastSyncAt = order.LastSyncAt,
            LastSyncError = order.LastSyncError,
            IsSyncedToLuca = order.IsSyncedToLuca,
            Lines = order.Lines.Select(l => new LocalSalesOrderLineDto
            {
                Id = l.Id,
                SalesOrderId = l.SalesOrderId,
                KatanaRowId = l.KatanaRowId,
                VariantId = l.VariantId,
                SKU = l.SKU,
                ProductName = l.ProductName,
                Quantity = l.Quantity,
                PricePerUnit = l.PricePerUnit,
                PricePerUnitInBaseCurrency = l.PricePerUnitInBaseCurrency,
                Total = l.Total,
                TotalInBaseCurrency = l.TotalInBaseCurrency,
                TaxRate = l.TaxRate,
                TaxRateId = l.TaxRateId,
                LocationId = l.LocationId,
                ProductAvailability = l.ProductAvailability,
                ProductExpectedDate = l.ProductExpectedDate,
                LucaDetayId = l.LucaDetayId,
                LucaStokId = l.LucaStokId,
                LucaDepoId = l.LucaDepoId
            }).ToList()
        };
    }

    /// <summary>
    /// APPROVED_WITH_ERRORS durumundaki siparişlerin durumunu temizle
    /// Charset sorunu düzeltildikten sonra eski hataları temizlemek için kullanılır
    /// </summary>
    [HttpPost("clear-errors")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ClearApprovedErrors()
    {
        try
        {
            _logger.LogInformation("ClearApprovedErrors: Clearing APPROVED_WITH_ERRORS status");

            // APPROVED_WITH_ERRORS durumundaki siparişleri bul
            var failedOrders = await _context.SalesOrders
                .Where(o => o.Status == "APPROVED_WITH_ERRORS")
                .ToListAsync();

            if (!failedOrders.Any())
            {
                return Ok(new { success = true, message = "Temizlenecek sipariş bulunamadı.", clearedCount = 0 });
            }

            _logger.LogInformation("Found {Count} orders with APPROVED_WITH_ERRORS status", failedOrders.Count);

            // Tüm siparişlerin durumunu APPROVED olarak güncelle ve hata mesajını temizle
            foreach (var order in failedOrders)
            {
                order.Status = "APPROVED";
                order.LastSyncError = null;
                order.UpdatedAt = DateTime.UtcNow;
                _logger.LogInformation("Cleared error status for order: {OrderNo} (ID: {OrderId})", order.OrderNo, order.Id);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("ClearApprovedErrors completed. Cleared {Count} orders", failedOrders.Count);

            return Ok(new
            {
                success = true,
                message = $"{failedOrders.Count} siparişin hata durumu temizlendi.",
                clearedCount = failedOrders.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ClearApprovedErrors: Unexpected error");
            return StatusCode(500, new { success = false, message = "Hata durumu temizlenirken hata oluştu.", error = ex.Message });
        }
    }
}
