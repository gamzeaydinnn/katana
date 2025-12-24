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
        private readonly IOrderMappingRepository _orderMappingRepo;
        private readonly IStockCardPreparationService _stockCardPreparationService;

        public SalesOrdersController(
            IntegrationDbContext context,
            ILucaService lucaService,
            ILoggingService loggingService,
            IAuditService auditService,
            ILogger<SalesOrdersController> logger,
            IKatanaService katanaService,
            ILocationMappingService locationMappingService,
            IOptions<LucaApiSettings> lucaSettings,
            IOrderMappingRepository orderMappingRepo,
            IStockCardPreparationService stockCardPreparationService)
        {
            _context = context;
            _lucaService = lucaService;
            _loggingService = loggingService;
            _auditService = auditService;
            _logger = logger;
            _katanaService = katanaService;
            _locationMappingService = locationMappingService;
            _lucaSettings = lucaSettings;
            _orderMappingRepo = orderMappingRepo;
            _stockCardPreparationService = stockCardPreparationService;
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
    /// KatanaOrderId bazında gruplu satış siparişlerini getir
    /// </summary>
    [HttpGet("grouped")]
    public async Task<ActionResult<IEnumerable<GroupedSalesOrderDto>>> GetGrouped(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? status = null,
        [FromQuery] string? syncStatus = null)
    {
        var query = _context.SalesOrders
            .Include(s => s.Customer)
            .Include(s => s.Lines)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(s => s.Status == status);
        }

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

        var orders = await query.ToListAsync();

        var grouped = orders
            .GroupBy(o => o.KatanaOrderId > 0 ? o.KatanaOrderId : o.Id)
            .Select(BuildGroupedOrder)
            .OrderByDescending(g => g.OrderCreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Ok(grouped);
    }

    /// <summary>
    /// Satış siparişi detayını getir
    /// </summary>
    [HttpGet("{id:int}")]
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

                    // ✅ OrderMapping kaydı oluştur (idempotency için)
                    await _orderMappingRepo.SaveLucaInvoiceIdAsync(
                        orderId: id,
                        lucaFaturaId: lucaResult.LucaOrderId.Value,
                        orderType: "SalesOrder",
                        externalOrderId: order.OrderNo,
                        belgeSeri: order.BelgeSeri,
                        belgeNo: order.BelgeNo,
                        belgeTakipNo: order.OrderNo);

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
    /// Admin onayı - Siparişi onayla ve Katana'ya tek bir sipariş olarak gönder.
    /// Tüm sipariş satırları tek bir Katana order içinde sales_order_rows olarak gönderilir.
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
            _logger.LogInformation("ApproveOrder started. OrderId={OrderId}, User={User}", id, User.Identity?.Name);

            // 1. Load order with lines and customer
            var order = await _context.SalesOrders
                .Include(s => s.Lines)
                .Include(s => s.Customer)
                .FirstOrDefaultAsync(s => s.Id == id);

            // 2. Validate order exists
            if (order == null)
            {
                _logger.LogWarning("ApproveOrder: Order not found. OrderId={OrderId}", id);
                return NotFound(new { success = false, message = $"Sipariş bulunamadı: {id}" });
            }

            // 3. Check for duplicate approval (Requirements 6.1, 6.2, 6.3)
            // Sadece Status bazlı kontrol - APPROVED veya SHIPPED ise tekrar onaylanamaz
            if (order.Status == "APPROVED" || order.Status == "SHIPPED")
            {
                _logger.LogWarning("ApproveOrder: Order already approved. OrderId={OrderId}, Status={Status}, KatanaOrderId={KatanaOrderId}", 
                    id, order.Status, order.KatanaOrderId);
                return BadRequest(new { 
                    success = false, 
                    message = "Bu sipariş zaten onaylanmış",
                    katanaOrderId = order.KatanaOrderId
                });
            }

            // 4. Validate order has lines (Requirements 3.1)
            if (order.Lines == null || order.Lines.Count == 0)
            {
                _logger.LogWarning("ApproveOrder: Order has no lines. OrderId={OrderId}, OrderNo={OrderNo}", id, order.OrderNo);
                return BadRequest(new
                {
                    success = false,
                    message = "Sipariş satırları bulunamadı. Katana'dan tekrar senkronize edin.",
                    orderNo = order.OrderNo
                });
            }

            // 5. Validate each line has SKU and positive quantity (Requirements 3.2, 3.3)
            var validationErrors = new List<string>();
            foreach (var line in order.Lines)
            {
                if (string.IsNullOrWhiteSpace(line.SKU))
                    validationErrors.Add($"Satır {line.Id}: SKU eksik");
                if (line.Quantity <= 0)
                    validationErrors.Add($"Satır {line.Id} ({line.SKU}): Geçersiz miktar ({line.Quantity})");
                if (line.VariantId <= 0)
                    validationErrors.Add($"Satır {line.Id} ({line.SKU}): VariantId eksik");
            }

            if (validationErrors.Count > 0)
            {
                _logger.LogWarning("ApproveOrder: Validation failed. OrderId={OrderId}, Errors={Errors}", 
                    id, string.Join("; ", validationErrors));
                return BadRequest(new
                {
                    success = false,
                    message = "Sipariş doğrulama hatası",
                    errors = validationErrors,
                    orderNo = order.OrderNo
                });
            }

            _logger.LogInformation("ApproveOrder: Validation passed. OrderId={OrderId}, OrderNo={OrderNo}, LineCount={LineCount}", 
                id, order.OrderNo, order.Lines.Count);

            // 6. Katana'ya gönder - SADECE YENİ SİPARİŞ İÇİN
            // ✅ FIX: KatanaOrderId > 0 ise Katana'ya HİÇBİR ŞEY gönderme
            SalesOrderDto? katanaResult = null;
            bool isNewKatanaOrder = false;
            
            if (order.KatanaOrderId > 0)
            {
                // ✅ Sipariş zaten Katana'dan gelmiş - Katana'ya YAZMA, sadece local status güncelle
                _logger.LogInformation("ApproveOrder: Order already exists in Katana. Skipping Katana API call. OrderId={OrderId}, KatanaOrderId={KatanaOrderId}", 
                    id, order.KatanaOrderId);
                katanaResult = new SalesOrderDto { Id = order.KatanaOrderId };
            }
            else
            {
                // ✅ Yeni sipariş oluştur (normalde Katana'dan gelmemiş manuel sipariş)
                try
                {
                    _logger.LogInformation("ApproveOrder: Creating new order in Katana. OrderNo={OrderNo}", order.OrderNo);
                    isNewKatanaOrder = true;
                    
                    var katanaOrder = BuildKatanaOrderFromSalesOrder(order);
                    
                    _logger.LogInformation("ApproveOrder: Katana order built. OrderNo={OrderNo}, CustomerId={CustomerId}, RowCount={RowCount}", 
                        katanaOrder.OrderNo, katanaOrder.CustomerId, katanaOrder.SalesOrderRows?.Count ?? 0);
                    
                    katanaResult = await _katanaService.CreateSalesOrderAsync(katanaOrder);
                    
                    if (katanaResult == null || katanaResult.Id <= 0)
                    {
                        _logger.LogError("ApproveOrder: Katana returned null or invalid response. OrderId={OrderId}", id);
                        return StatusCode(500, new { 
                            success = false, 
                            message = "Katana'da sipariş oluşturulamadı (boş yanıt)" 
                        });
                    }
                    
                    _logger.LogInformation("ApproveOrder: Katana order created. OrderId={OrderId}, KatanaOrderId={KatanaOrderId}", 
                        id, katanaResult.Id);
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex, "ApproveOrder: Katana API error. OrderId={OrderId}", id);
                    return StatusCode(500, new { 
                        success = false, 
                        message = "Katana API hatası",
                        error = ex.Message
                    });
                }
            }

            // 7. Update database with transaction (Requirements 1.4, 1.5)
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _context.Database.BeginTransactionAsync();
                try
                {
                    // ✅ Sadece yeni sipariş oluşturulduysa KatanaOrderId güncelle
                    if (isNewKatanaOrder && katanaResult != null && katanaResult.Id > 0)
                    {
                        order.KatanaOrderId = katanaResult.Id;
                        
                        // Update all lines with same KatanaOrderId (Requirements 1.5)
                        foreach (var line in order.Lines)
                        {
                            line.KatanaOrderId = katanaResult.Id;
                        }
                    }
                    
                    order.Status = "APPROVED";
                    order.ApprovedDate = DateTime.UtcNow;
                    order.ApprovedBy = User.Identity?.Name;
                    order.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                    await tx.CommitAsync();
                    
                    _logger.LogInformation("ApproveOrder: Database updated. OrderId={OrderId}, KatanaOrderId={KatanaOrderId}, Status={Status}", 
                        id, order.KatanaOrderId, order.Status);
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync();
                    _logger.LogError(ex, "ApproveOrder: Database update failed. OrderId={OrderId}", id);
                    throw;
                }
            });

            // 9. Send to Luca (non-blocking) (Requirements 4.1, 4.2, 4.3, 4.4, 4.5)
            SalesOrderSyncResultDto? lucaSync = null;
            string? lucaSkippedReason = null;
            var stockCardCreationResults = new List<object>();

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
                try
                {
                    // ✅ LUCA STOK KARTI KONTROLÜ - Fatura göndermeden önce SKU'ları kontrol et
                    _logger.LogInformation("ApproveOrder: Preparing stock cards for {LineCount} lines. OrderId={OrderId}", 
                        order.Lines.Count, id);
                    
                    // Use StockCardPreparationService for stock card operations
                    var stockCardResult = await _stockCardPreparationService.PrepareStockCardsForOrderAsync(order);
                    
                    // Convert results to response format
                    foreach (var result in stockCardResult.Results)
                    {
                        stockCardCreationResults.Add(new { 
                            sku = result.SKU, 
                            action = result.Action, 
                            skartId = result.SkartId,
                            message = result.Message,
                            error = result.Error
                        });
                    }
                    
                    _logger.LogInformation("ApproveOrder: Stock card preparation complete. Total={Total}, Success={Success}, Failed={Failed}, Skipped={Skipped}", 
                        stockCardResult.TotalLines, stockCardResult.SuccessCount, stockCardResult.FailedCount, stockCardResult.SkippedCount);

                    var depoKodu = await _locationMappingService.GetDepoKoduByLocationIdAsync(order.LocationId?.ToString() ?? string.Empty);
                    _logger.LogInformation("ApproveOrder: Sending to Luca. OrderId={OrderId}, DepoKodu={DepoKodu}", id, depoKodu);
                    
                    lucaSync = await _lucaService.CreateSalesOrderInvoiceAsync(order, depoKodu);

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
                                _logger.LogInformation("ApproveOrder: Luca sync successful. OrderId={OrderId}, LucaOrderId={LucaOrderId}", 
                                    id, lucaSync.LucaOrderId);
                                
                                // ✅ OrderMapping kaydı oluştur (idempotency için)
                                await _orderMappingRepo.SaveLucaInvoiceIdAsync(
                                    orderId: id,
                                    lucaFaturaId: lucaSync.LucaOrderId.Value,
                                    orderType: "SalesOrder",
                                    externalOrderId: order.OrderNo,
                                    belgeSeri: order.BelgeSeri,
                                    belgeNo: order.BelgeNo,
                                    belgeTakipNo: order.OrderNo);
                                _logger.LogInformation("ApproveOrder: OrderMapping created. OrderId={OrderId}, LucaInvoiceId={LucaInvoiceId}", 
                                    id, lucaSync.LucaOrderId);
                            }
                            else
                            {
                                current.IsSyncedToLuca = false;
                                current.LastSyncError = Truncate(lucaSync.ErrorDetails ?? "Luca satış faturası oluşturma başarısız", 950);
                                _logger.LogWarning("ApproveOrder: Luca sync failed. OrderId={OrderId}, Error={Error}", 
                                    id, current.LastSyncError);
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
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ApproveOrder: Luca sync error (non-blocking). OrderId={OrderId}", id);
                    lucaSkippedReason = $"Luca sync hatası: {ex.Message}";
                    
                    // Update error in database but don't fail the approval
                    try
                    {
                        order.LastSyncError = Truncate(ex.Message, 950);
                        order.LastSyncAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                    }
                    catch { /* Ignore secondary errors */ }
                }
            }

            // 10. Audit logging (Requirements 7.1, 7.5)
            _auditService.LogUpdate(
                "SalesOrder",
                id.ToString(),
                User.Identity?.Name ?? "System",
                null,
                $"Sipariş onaylandı ve Katana'ya gönderildi (KatanaOrderId={order.KatanaOrderId})");

            _logger.LogInformation("ApproveOrder completed. OrderId={OrderId}, OrderNo={OrderNo}, KatanaOrderId={KatanaOrderId}, Status={Status}",
                id, order.OrderNo, order.KatanaOrderId, order.Status);
            _loggingService.LogInfo($"SalesOrder approved: {order.OrderNo} (KatanaOrderId={order.KatanaOrderId})",
                User.Identity?.Name, $"SalesOrderId={id}", LogCategory.Business);

            // Build response
            object lucaSyncPayload = lucaSync != null
                ? new { attempted = true, isSuccess = lucaSync.IsSuccess, lucaOrderId = lucaSync.LucaOrderId, message = lucaSync.Message, errorDetails = lucaSync.ErrorDetails, stockCardResults = stockCardCreationResults }
                : new { attempted = false, reason = lucaSkippedReason, stockCardResults = stockCardCreationResults };

            return Ok(new
            {
                success = true,
                message = "Sipariş onaylandı ve Katana'ya gönderildi",
                orderNo = order.OrderNo,
                orderStatus = order.Status,
                katanaOrderId = order.KatanaOrderId,
                lineCount = order.Lines.Count,
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

    /// <summary>
    /// Build Katana SalesOrderDto from local SalesOrder entity
    /// </summary>
    private SalesOrderDto BuildKatanaOrderFromSalesOrder(SalesOrder order)
    {
        // Katana'ya gönderirken, siparişin orijinal Katana customer_id'sini kullanmalıyız
        // Customer.ReferenceId Katana'dan gelen customer ID'yi içerir
        long katanaCustomerId = 0;
        if (order.Customer?.ReferenceId != null && long.TryParse(order.Customer.ReferenceId, out var parsedId))
        {
            katanaCustomerId = parsedId;
        }

        return new SalesOrderDto
        {
            OrderNo = order.OrderNo ?? $"SO-{order.Id}",
            CustomerId = katanaCustomerId,
            LocationId = order.LocationId,
            DeliveryDate = order.DeliveryDate,
            Currency = order.Currency ?? "TRY",
            Status = "NOT_SHIPPED",
            AdditionalInfo = order.AdditionalInfo,
            CustomerRef = order.CustomerRef,

            // Map all lines to sales_order_rows (Requirements 2.1, 2.2, 2.3, 2.4)
            SalesOrderRows = order.Lines.Select(line => new SalesOrderRowDto
            {
                VariantId = line.VariantId,
                Quantity = line.Quantity,
                PricePerUnit = line.PricePerUnit,
                TaxRateId = line.TaxRateId,
                LocationId = line.LocationId ?? order.LocationId,
                Attributes = new List<SalesOrderRowAttributeDto>()
            }).ToList(),

            Addresses = new List<SalesOrderAddressDto>()
        };
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

    private static GroupedSalesOrderDto BuildGroupedOrder(IGrouping<long, SalesOrder> group)
    {
        var orders = group.ToList();
        var header = orders.FirstOrDefault(o => o.Customer != null)
            ?? orders.OrderBy(o => o.OrderCreatedDate ?? o.CreatedAt).First();

        var lines = orders
            .SelectMany(o => o.Lines ?? new List<SalesOrderLine>())
            .GroupBy(l => l.KatanaRowId != 0 ? $"K:{l.KatanaRowId}" : $"L:{l.Id}")
            .Select(g => g.First())
            .OrderBy(l => l.KatanaRowId)
            .ThenBy(l => l.Id)
            .Select(MapLineDto)
            .ToList();

        return new GroupedSalesOrderDto
        {
            GroupKatanaOrderId = group.Key,
            OrderNo = header.OrderNo,
            OrderNos = orders.Select(o => o.OrderNo).Where(o => !string.IsNullOrWhiteSpace(o)).Distinct().ToList(),
            CustomerId = header.CustomerId,
            CustomerName = header.Customer?.Title,
            OrderCreatedDate = header.OrderCreatedDate,
            DeliveryDate = header.DeliveryDate,
            Currency = header.Currency,
            Status = header.Status,
            Total = orders.Sum(o => o.Total ?? 0m),
            TotalInBaseCurrency = orders.Sum(o => o.TotalInBaseCurrency ?? 0m),
            IsSyncedToLuca = orders.Any(o => o.IsSyncedToLuca),
            LastSyncError = orders.Select(o => o.LastSyncError).FirstOrDefault(e => !string.IsNullOrWhiteSpace(e)),
            LastSyncAt = orders.Max(o => o.LastSyncAt),
            Lines = lines
        };
    }

    private static LocalSalesOrderLineDto MapLineDto(SalesOrderLine line)
    {
        return new LocalSalesOrderLineDto
        {
            Id = line.Id,
            SalesOrderId = line.SalesOrderId,
            KatanaRowId = line.KatanaRowId,
            VariantId = line.VariantId,
            SKU = line.SKU,
            ProductName = line.ProductName,
            Quantity = line.Quantity,
            PricePerUnit = line.PricePerUnit,
            PricePerUnitInBaseCurrency = line.PricePerUnitInBaseCurrency,
            Total = line.Total,
            TotalInBaseCurrency = line.TotalInBaseCurrency,
            TaxRate = line.TaxRate,
            TaxRateId = line.TaxRateId,
            LocationId = line.LocationId,
            ProductAvailability = line.ProductAvailability,
            ProductExpectedDate = line.ProductExpectedDate,
            LucaDetayId = line.LucaDetayId,
            LucaStokId = line.LucaStokId,
            LucaDepoId = line.LucaDepoId
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

    /// <summary>
    /// Sipariş detayını varyant gruplarıyla birlikte getir
    /// Multi-variant siparişler için ürün bazlı gruplama ve alt toplamlar
    /// </summary>
    [HttpGet("{id}/grouped-summary")]
    public async Task<ActionResult<OrderGroupedSummaryDto>> GetGroupedSummary(int id)
    {
        var order = await _context.SalesOrders
            .Include(s => s.Customer)
            .Include(s => s.Lines)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (order == null)
            return NotFound($"Sipariş bulunamadı: {id}");

        // Satırları ürün bazında grupla
        var productGroups = order.Lines
            .GroupBy(l => GetProductBaseCode(l.SKU))
            .Select(g => new OrderProductGroupDto
            {
                ProductBaseCode = g.Key,
                ProductName = g.First().ProductName?.Split('-').FirstOrDefault()?.Trim() ?? g.Key,
                VariantCount = g.Count(),
                Lines = g.Select(l => new LocalSalesOrderLineDto
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
                }).ToList(),
                SubtotalQuantity = g.Sum(l => l.Quantity),
                SubtotalAmount = g.Sum(l => l.Total ?? 0),
                SubtotalAmountInBaseCurrency = g.Sum(l => l.TotalInBaseCurrency ?? 0)
            })
            .OrderBy(g => g.ProductBaseCode)
            .ToList();

        var summary = new OrderGroupedSummaryDto
        {
            OrderId = order.Id,
            OrderNo = order.OrderNo,
            CustomerName = order.Customer?.Title,
            OrderDate = order.OrderCreatedDate,
            Status = order.Status,
            Currency = order.Currency,
            TotalLineCount = order.Lines.Count,
            UniqueProductCount = productGroups.Count,
            ProductGroups = productGroups,
            GrandTotal = order.Total ?? 0,
            GrandTotalInBaseCurrency = order.TotalInBaseCurrency ?? 0
        };

        return Ok(summary);
    }

    /// <summary>
    /// SKU'dan ana ürün kodunu çıkarır (PRODUCT-VARIANT-ATTR formatından PRODUCT kısmını alır)
    /// </summary>
    private static string GetProductBaseCode(string? sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
            return "UNKNOWN";

        var parts = sku.Split('-');
        return parts.Length > 0 ? parts[0] : sku;
    }
}
