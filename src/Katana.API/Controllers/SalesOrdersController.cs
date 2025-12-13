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
    public async Task<ActionResult<SalesOrderSyncResultDto>> SyncToLuca(int id)
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
    /// Admin onayı - Siparişi onayla ve Katana'ya stok olarak ekle
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

            _logger.LogInformation("Approving SalesOrder. OrderId={OrderId}, OrderNo={OrderNo}, LineCount={LineCount}", id, order.OrderNo, order.Lines.Count);
            _loggingService.LogInfo($"SalesOrder approve started: {order.OrderNo} (lines={order.Lines.Count})",
                User.Identity?.Name, $"SalesOrderId={id}", LogCategory.Business);

            var syncResults = new List<object>();
            var failureSummaries = new List<string>();
            var successCount = 0;
            var failCount = 0;

            foreach (var line in order.Lines)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(line.SKU))
                    {
                        var msg = $"LineId={line.Id}: SKU boş";
                        _logger.LogWarning("SalesOrder approve skipped line: {Message}", msg);
                        syncResults.Add(new { sku = "N/A", success = false, error = "SKU boş" });
                        failureSummaries.Add(msg);
                        failCount++;
                        continue;
                    }

                    if (line.Quantity <= 0)
                    {
                        var msg = $"SKU={line.SKU}: geçersiz miktar ({line.Quantity})";
                        _logger.LogWarning("SalesOrder approve skipped line: {Message}", msg);
                        syncResults.Add(new { sku = line.SKU, success = false, error = "Miktar 0 veya negatif" });
                        failureSummaries.Add(msg);
                        failCount++;
                        continue;
                    }

                    var ok = await _katanaService.SyncProductStockAsync(
                        sku: line.SKU,
                        quantity: line.Quantity,
                        locationId: order.LocationId,
                        productName: line.ProductName,
                        salesPrice: line.PricePerUnit);

                    if (ok)
                    {
                        syncResults.Add(new { sku = line.SKU, success = true, action = "synced" });
                        successCount++;
                    }
                    else
                    {
                        var msg = $"SKU={line.SKU}: Katana stok senkronu başarısız";
                        _logger.LogWarning("SalesOrder approve failed: {Message}", msg);
                        syncResults.Add(new { sku = line.SKU, success = false, error = "Katana stok senkronu başarısız" });
                        failureSummaries.Add(msg);
                        failCount++;
                    }
                }
                catch (Exception ex)
                {
                    var msg = $"SKU={line.SKU}: {ex.Message}";
                    _logger.LogError(ex, "SalesOrder approve Katana sync failed. OrderId={OrderId}, Sku={Sku}", id, line.SKU);
                    syncResults.Add(new { sku = line.SKU, success = false, error = ex.Message });
                    failureSummaries.Add(msg);
                    failCount++;
                }
            }

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                order.Status = failCount == 0 ? "APPROVED" : "APPROVED_WITH_ERRORS";
                order.LastSyncError = failCount == 0
                    ? null
                    : Truncate(string.Join(" | ", failureSummaries.Distinct().Take(25)), 1900);
                order.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "SalesOrder approve failed while persisting result. OrderId={OrderId}", id);
                _loggingService.LogError($"SalesOrder approve DB update failed: {id}", ex, User.Identity?.Name, null, LogCategory.Business);
                return StatusCode(500, new { success = false, message = "Onay sonucu kaydedilirken hata oluştu." });
            }

            _auditService.LogUpdate(
                "SalesOrder",
                id.ToString(),
                User.Identity?.Name ?? "System",
                null,
                $"Sipariş onaylandı ve Katana'ya {successCount} ürün eklendi/güncellendi");

            _logger.LogInformation("SalesOrder approval completed. OrderId={OrderId}, OrderNo={OrderNo}, Success={Success}, Failed={Failed}, Status={Status}",
                id, order.OrderNo, successCount, failCount, order.Status);
            _loggingService.LogInfo($"SalesOrder approve completed: {order.OrderNo} (ok={successCount}, fail={failCount}, status={order.Status})",
                User.Identity?.Name, $"SalesOrderId={id}", LogCategory.Business);

            return Ok(new
            {
                success = failCount == 0,
                message = failCount == 0
                    ? $"Sipariş onaylandı. {successCount} ürün Katana'ya eklendi/güncellendi."
                    : $"Sipariş onaylandı ama Katana senkronunda hata var. Başarılı: {successCount}, Hatalı: {failCount}.",
                orderNo = order.OrderNo,
                orderStatus = order.Status,
                successCount,
                failCount,
                syncResults
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
}
