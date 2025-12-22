using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Katana.Core.Entities;
using Katana.Core.Enums;
using Katana.Core.Helpers;
using Katana.Core.Interfaces;
using Katana.Business.Services;
using Katana.Data.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

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

    public SalesOrdersController(
        IntegrationDbContext context,
        ILucaService lucaService,
        ILoggingService loggingService,
        IAuditService auditService,
        ILogger<SalesOrdersController> logger,
        IKatanaService katanaService,
        ILocationMappingService locationMappingService)
    {
        _context = context;
        _lucaService = lucaService;
        _loggingService = loggingService;
        _auditService = auditService;
        _logger = logger;
        _katanaService = katanaService;
        _locationMappingService = locationMappingService;
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
            return BadRequest("Müşteri bilgisi eksik");

        if (order.Lines == null || order.Lines.Count == 0)
        {
            return BadRequest(new { message = "Sipariş satırları bulunamadı. Katana'dan tekrar senkronize edin." });
        }

        // Cari kodu validasyonu: "CUST_" gibi değerler Luca tarafında HTML/500'a sebep olabilir.
        var customerCode = !string.IsNullOrWhiteSpace(order.Customer.LucaCode) ? order.Customer.LucaCode : order.Customer.TaxNo;
        if (string.IsNullOrWhiteSpace(customerCode) || customerCode.StartsWith("CUST_", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Müşterinin geçerli bir Vergi No veya Luca Cari Kodu eksik/geçersiz. 'CUST_' gönderilemez." });
        }

        // Duplikasyon kontrolü - Zaten senkronize edilmiş ve hata yoksa reddet
        if (order.IsSyncedToLuca && string.IsNullOrEmpty(order.LastSyncError))
        {
            return BadRequest(new { message = "Order already synced to Luca", lucaOrderId = order.LucaOrderId });
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

            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        // Dış entegrasyon çağrısı (Luca) retry stratejisinin içine sokulmaz: transient DB retry durumunda Luca'ya duplicate gitmesin.
        var depoKodu = await _locationMappingService.GetDepoKoduByLocationIdAsync(order.LocationId?.ToString() ?? string.Empty);
        var lucaResult = await _lucaService.CreateSalesOrderInvoiceAsync(order, depoKodu);

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
    /// Admin onayı - Siparişi onayla ve Katana'ya Sales Order olarak gönder
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

            _logger.LogInformation("Approving SalesOrder and sending to Katana. OrderId={OrderId}, OrderNo={OrderNo}, LineCount={LineCount}", id, order.OrderNo, order.Lines.Count);
            _loggingService.LogInfo($"SalesOrder approve started: {order.OrderNo} (lines={order.Lines.Count})",
                User.Identity?.Name, $"SalesOrderId={id}", LogCategory.Business);

            // Katana'ya Sales Order gönder
            long? katanaOrderId = null;
            string? katanaError = null;
            
            try
            {
                // Önce müşteri ID'sini bul veya oluştur
                long? katanaCustomerId = null;
                if (order.Customer != null &&
                    !string.IsNullOrWhiteSpace(order.Customer.ReferenceId) &&
                    long.TryParse(order.Customer.ReferenceId, out var parsedKatanaCustomerId))
                {
                    katanaCustomerId = parsedKatanaCustomerId;
                }
                
                if (!katanaCustomerId.HasValue && order.Customer != null)
                {
                    // Müşteri Katana'da yoksa, müşteri adıyla ara
                    var customers = await _katanaService.GetCustomersAsync();
                    var existingCustomer = customers?.FirstOrDefault(c => 
                        c.Name?.Equals(order.Customer.Title, StringComparison.OrdinalIgnoreCase) == true ||
                        c.Email?.Equals(order.Customer.Email, StringComparison.OrdinalIgnoreCase) == true);
                    
                    if (existingCustomer != null)
                    {
                        katanaCustomerId = existingCustomer.Id;
                        // Müşteri ID'sini kaydet
                        order.Customer.ReferenceId = katanaCustomerId.Value.ToString();
                    }
                    else
                    {
                        // Yeni müşteri oluştur
                        var newCustomer = await _katanaService.CreateCustomerAsync(new KatanaCustomerDto
                        {
                            Name = order.Customer.Title ?? "Bilinmeyen Müşteri",
                            Email = order.Customer.Email,
                            Phone = order.Customer.Phone
                        });
                        
                        if (newCustomer != null)
                        {
                            katanaCustomerId = newCustomer.Id;
                            order.Customer.ReferenceId = katanaCustomerId.Value.ToString();
                        }
                    }
                }

                if (!katanaCustomerId.HasValue)
                {
                    katanaError = "Katana müşteri ID'si bulunamadı veya oluşturulamadı";
                    _logger.LogWarning("ApproveOrder: {Error}. OrderId={OrderId}", katanaError, id);
                }
                else
                {
                    // Sipariş satırlarını Katana formatına dönüştür
                    // Not: Bu senaryoda "admin onayı" = önce stoğa giriş (+), sonra satış siparişi ile rezerve et.
                    var salesOrderRows = new List<SalesOrderRowDto>();

                    foreach (var line in order.Lines)
                    {
                        if (string.IsNullOrWhiteSpace(line.SKU) || line.Quantity <= 0)
                            continue;

                        // 1) Ürünü bul/oluştur ve stoğu artır (Stock Adjustment)
                        var stockSyncSuccess = await _katanaService.SyncProductStockAsync(
                            sku: line.SKU,
                            quantity: line.Quantity,
                            locationId: line.LocationId ?? order.LocationId,
                            productName: !string.IsNullOrWhiteSpace(line.ProductName) ? line.ProductName : line.SKU,
                            salesPrice: line.PricePerUnit
                        );

                        if (!stockSyncSuccess)
                        {
                            _logger.LogError("❌ KRITIK: Stok artışı/ürün oluşturma başarısız oldu: {SKU}. Satış siparişi satırı atlanıyor!", line.SKU);
                            continue; // ⚠️ ÖNEMLI: Başarısız olursa satırı atla!
                        }

                        _logger.LogInformation("✅ Stok artışı başarılı: {SKU}, Qty={Qty}", line.SKU, line.Quantity);

                        // 2) Variant ID'yi çöz (line.VariantId 0 gelebilir)
                        long? variantId = line.VariantId > 0 ? line.VariantId : null;

                        if (!variantId.HasValue)
                        {
                            variantId = await _katanaService.FindVariantIdBySkuAsync(line.SKU);
                            if (variantId.HasValue)
                                line.VariantId = variantId.Value;
                        }

                        if (!variantId.HasValue)
                        {
                            _logger.LogError("❌ KRITIK: Variant not found for SKU {SKU}, skipping line", line.SKU);
                            continue;
                        }

                        // 3) Satış siparişi satırını ekle (rezervasyon/committed)
                        salesOrderRows.Add(new SalesOrderRowDto
                        {
                            VariantId = variantId.Value,
                            Quantity = line.Quantity,
                            PricePerUnit = line.PricePerUnit,
                            TaxRateId = line.TaxRateId,
                            LocationId = line.LocationId ?? order.LocationId
                        });
                        
                        _logger.LogInformation("✅ Satış siparişi satırı eklendi: SKU={SKU}, VariantId={VariantId}, Qty={Qty}", line.SKU, variantId, line.Quantity);
                    }

                    if (salesOrderRows.Count == 0)
                    {
                        katanaError = "Katana'ya gönderilecek geçerli sipariş satırı bulunamadı (SKU/Quantity/Variant kontrol edin).";
                        _logger.LogWarning("ApproveOrder: {Error}. OrderId={OrderId}", katanaError, id);
                    }
                    else
                    {
                        // Katana Sales Order oluştur
                        var katanaSalesOrder = new SalesOrderDto
                        {
                            OrderNo = $"SO-{order.OrderNo}",
                            CustomerId = katanaCustomerId.Value,
                            OrderCreatedDate = order.OrderCreatedDate ?? DateTime.UtcNow,
                            DeliveryDate = order.DeliveryDate,
                            Currency = order.Currency ?? "EUR",
                            Status = "NOT_SHIPPED",
                            LocationId = order.LocationId,
                            AdditionalInfo = order.AdditionalInfo ?? $"Admin onayı ile oluşturuldu - {DateTime.UtcNow:yyyy-MM-dd HH:mm}",
                            CustomerRef = order.CustomerRef,
                            SalesOrderRows = salesOrderRows
                        };

                        var createdOrder = await _katanaService.CreateSalesOrderAsync(katanaSalesOrder);
                        
                        if (createdOrder != null)
                        {
                            katanaOrderId = createdOrder.Id;
                            _logger.LogInformation("Katana Sales Order created. KatanaOrderId={KatanaOrderId}, OrderNo={OrderNo}", katanaOrderId, createdOrder.OrderNo);
                        }
                        else
                        {
                            katanaError = "Katana sipariş oluşturma başarısız (null response)";
                            _logger.LogWarning("ApproveOrder: {Error}. OrderId={OrderId}", katanaError, id);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                katanaError = $"Katana API hatası: {ex.Message}";
                _logger.LogError(ex, "ApproveOrder Katana sync failed. OrderId={OrderId}", id);
            }

            // Veritabanını güncelle
            try
            {
                var strategy = _context.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    await using var tx = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        var isSuccess = katanaOrderId.HasValue;
                        order.Status = isSuccess ? "APPROVED" : "APPROVED_WITH_ERRORS";
                        if (katanaOrderId.HasValue)
                            order.KatanaOrderId = katanaOrderId.Value;
                        order.LastSyncError = isSuccess ? null : Truncate(katanaError ?? "Bilinmeyen hata", 1900);
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

            _auditService.LogUpdate(
                "SalesOrder",
                id.ToString(),
                User.Identity?.Name ?? "System",
                null,
                katanaOrderId.HasValue 
                    ? $"Sipariş onaylandı ve Katana'ya gönderildi (KatanaOrderId={katanaOrderId})"
                    : $"Sipariş onaylandı ama Katana'ya gönderilemedi: {katanaError}");

            _logger.LogInformation("SalesOrder approval completed. OrderId={OrderId}, OrderNo={OrderNo}, KatanaOrderId={KatanaOrderId}, Status={Status}",
                id, order.OrderNo, katanaOrderId, order.Status);
            _loggingService.LogInfo($"SalesOrder approve completed: {order.OrderNo} (katanaId={katanaOrderId}, status={order.Status})",
                User.Identity?.Name, $"SalesOrderId={id}", LogCategory.Business);

            return Ok(new
            {
                success = katanaOrderId.HasValue,
                message = katanaOrderId.HasValue
                    ? $"Sipariş onaylandı ve Katana'ya gönderildi. Katana Order ID: {katanaOrderId}"
                    : $"Sipariş onaylandı ama Katana'ya gönderilemedi: {katanaError}",
                orderNo = order.OrderNo,
                orderStatus = order.Status,
                katanaOrderId = katanaOrderId,
                error = katanaError
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
