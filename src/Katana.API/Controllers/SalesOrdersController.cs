using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Katana.Core.Entities;
using Katana.Core.Enums;
using Katana.Core.Helpers;
using Katana.Core.Interfaces;
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

    public SalesOrdersController(
        IntegrationDbContext context,
        ILucaService lucaService,
        ILoggingService loggingService,
        IAuditService auditService,
        ILogger<SalesOrdersController> logger,
        IKatanaService katanaService)
    {
        _context = context;
        _lucaService = lucaService;
        _loggingService = loggingService;
        _auditService = auditService;
        _logger = logger;
        _katanaService = katanaService;
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

        // Duplikasyon kontrolü - Zaten senkronize edilmiş ve hata yoksa reddet
        if (order.IsSyncedToLuca && string.IsNullOrEmpty(order.LastSyncError))
        {
            return BadRequest(new { message = "Order already synced to Luca", lucaOrderId = order.LucaOrderId });
        }

        try
        {
            // Map to Luca request
            var lucaRequest = MappingHelper.MapToLucaSalesOrderHeader(order, order.Customer);

            // Call Luca API
            var result = await _lucaService.CreateSalesOrderHeaderAsync(lucaRequest);

            static string? TryGetLucaMessage(System.Text.Json.JsonElement el)
            {
                if (el.TryGetProperty("mesaj", out var mesaj) && mesaj.ValueKind == System.Text.Json.JsonValueKind.String)
                    return mesaj.GetString();
                if (el.TryGetProperty("message", out var msg) && msg.ValueKind == System.Text.Json.JsonValueKind.String)
                    return msg.GetString();
                if (el.TryGetProperty("error", out var err) && err.ValueKind == System.Text.Json.JsonValueKind.String)
                    return err.GetString();
                return null;
            }

            var isOk = true;
            if (result.TryGetProperty("basarili", out var basariliProp)
                && (basariliProp.ValueKind == System.Text.Json.JsonValueKind.True
                    || basariliProp.ValueKind == System.Text.Json.JsonValueKind.False))
            {
                isOk = basariliProp.GetBoolean();
            }

            // Extract LucaOrderId from response
            int? lucaOrderId = null;
            if (result.TryGetProperty("siparisId", out var siparisIdProp) && siparisIdProp.ValueKind == System.Text.Json.JsonValueKind.Number)
                lucaOrderId = siparisIdProp.GetInt32();
            else if (result.TryGetProperty("id", out var idProp) && idProp.ValueKind == System.Text.Json.JsonValueKind.Number)
                lucaOrderId = idProp.GetInt32();

            if (!isOk || !lucaOrderId.HasValue)
            {
                var lucaMsg = TryGetLucaMessage(result) ?? "Luca sipariş oluşturma başarısız (id dönmedi)";

                order.LastSyncError = lucaMsg;
                order.LastSyncAt = DateTime.UtcNow;
                order.IsSyncedToLuca = false;
                await _context.SaveChangesAsync();

                _logger.LogWarning("Luca sales order sync failed. OrderId={OrderId}, OrderNo={OrderNo}, Message={Message}", id, order.OrderNo, lucaMsg);
                _loggingService.LogWarning($"Luca sales order sync failed: {lucaMsg}", User?.Identity?.Name, $"SalesOrderId={id}", LogCategory.Business);

                return Ok(new SalesOrderSyncResultDto
                {
                    IsSuccess = false,
                    Message = "Luca senkronizasyonu başarısız",
                    ErrorDetails = lucaMsg,
                    SyncedAt = order.LastSyncAt
                });
            }

            // Update order with sync result
            order.LucaOrderId = lucaOrderId;
            order.IsSyncedToLuca = true;
            order.LastSyncAt = DateTime.UtcNow;
            order.LastSyncError = null;
            await _context.SaveChangesAsync();

            _loggingService.LogInfo($"SalesOrder {id} synced to Luca: {lucaOrderId}", 
                User?.Identity?.Name, null, LogCategory.Business);

            return Ok(new SalesOrderSyncResultDto
            {
                IsSuccess = true,
                Message = "Luca'ya başarıyla senkronize edildi",
                LucaOrderId = lucaOrderId,
                SyncedAt = order.LastSyncAt
            });
        }
        catch (Exception ex)
        {
            // Update order with error
            order.LastSyncError = ex.Message;
            order.LastSyncAt = DateTime.UtcNow;
            order.IsSyncedToLuca = false;
            await _context.SaveChangesAsync();

            _loggingService.LogError($"SalesOrder {id} Luca sync failed", ex, 
                User?.Identity?.Name, null, LogCategory.Business);

            return Ok(new SalesOrderSyncResultDto
            {
                IsSuccess = false,
                Message = "Luca senkronizasyonu başarısız",
                ErrorDetails = ex.Message,
                SyncedAt = order.LastSyncAt
            });
        }
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

        static string? TryGetLucaMessage(System.Text.Json.JsonElement el)
        {
            if (el.TryGetProperty("mesaj", out var mesaj) && mesaj.ValueKind == System.Text.Json.JsonValueKind.String)
                return mesaj.GetString();
            if (el.TryGetProperty("message", out var msg) && msg.ValueKind == System.Text.Json.JsonValueKind.String)
                return msg.GetString();
            if (el.TryGetProperty("error", out var err) && err.ValueKind == System.Text.Json.JsonValueKind.String)
                return err.GetString();
            return null;
        }

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

                var lucaRequest = MappingHelper.MapToLucaSalesOrderHeader(order, order.Customer);
                var result = await _lucaService.CreateSalesOrderHeaderAsync(lucaRequest);

                var isOk = true;
                if (result.TryGetProperty("basarili", out var basariliProp)
                    && (basariliProp.ValueKind == System.Text.Json.JsonValueKind.True
                        || basariliProp.ValueKind == System.Text.Json.JsonValueKind.False))
                {
                    isOk = basariliProp.GetBoolean();
                }

                int? lucaOrderId = null;
                if (result.TryGetProperty("siparisId", out var siparisIdProp) && siparisIdProp.ValueKind == System.Text.Json.JsonValueKind.Number)
                    lucaOrderId = siparisIdProp.GetInt32();
                else if (result.TryGetProperty("id", out var idProp) && idProp.ValueKind == System.Text.Json.JsonValueKind.Number)
                    lucaOrderId = idProp.GetInt32();

                if (!isOk || !lucaOrderId.HasValue)
                {
                    var msg = TryGetLucaMessage(result) ?? "Luca sipariş oluşturma başarısız (id dönmedi)";
                    return (OrderId: order.Id, OrderNo: order.OrderNo, Success: false, LucaOrderId: (int?)null, Error: msg);
                }

                return (OrderId: order.Id, OrderNo: order.OrderNo, Success: true, LucaOrderId: lucaOrderId, Error: (string?)null);
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
        var order = await _context.SalesOrders
            .Include(s => s.Lines)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (order == null)
        {
            return NotFound(new { success = false, message = $"Sipariş bulunamadı: {id}" });
        }

        // Zaten onaylanmış mı kontrol et
        if (order.Status == "APPROVED" || order.Status == "SHIPPED")
        {
            return BadRequest(new { success = false, message = "Bu sipariş zaten onaylanmış" });
        }

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
        int successCount = 0;
        int failCount = 0;

        // Her sipariş kalemi için Katana'ya stok ekle
        foreach (var line in order.Lines)
        {
            try
            {
                if (string.IsNullOrEmpty(line.SKU))
                {
                    _logger.LogWarning("⚠️ SKU boş, atlanıyor: LineId={LineId}", line.Id);
                    syncResults.Add(new { sku = "N/A", success = false, error = "SKU boş" });
                    failCount++;
                    continue;
                }

                // Katana'da ürün var mı kontrol et
                var existingProduct = await _katanaService.GetProductBySkuAsync(line.SKU);

                if (existingProduct != null)
                {
                    // Ürün varsa stok güncelle
                    if (!int.TryParse(existingProduct.Id, out var katanaProductId))
                    {
                        _logger.LogWarning("⚠️ Katana ürün ID sayısal değil: {Id}", existingProduct.Id);
                        syncResults.Add(new { sku = line.SKU, success = false, error = "Geçersiz Katana ID" });
                        failCount++;
                        continue;
                    }

                    var newStock = (existingProduct.InStock ?? 0) + line.Quantity;
                    var updated = await _katanaService.UpdateProductAsync(
                        katanaProductId,
                        existingProduct.Name,
                        existingProduct.SalesPrice,
                        (int)Math.Round(newStock, MidpointRounding.AwayFromZero)
                    );

                    if (updated)
                    {
                        _logger.LogInformation("✅ Katana stok güncellendi: {SKU}, Yeni Stok: {Stock}", line.SKU, newStock);
                        syncResults.Add(new { sku = line.SKU, success = true, action = "updated", newStock });
                        successCount++;
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Katana stok güncellenemedi: {SKU}", line.SKU);
                        syncResults.Add(new { sku = line.SKU, success = false, error = "Katana stok güncelleme başarısız (UpdateProductAsync=false)" });
                        failCount++;
                    }
                }
                else
                {
                    // Ürün yoksa oluştur
                    var intendedStock = (int)Math.Round(line.Quantity, MidpointRounding.AwayFromZero);
                    var newProduct = new KatanaProductDto
                    {
                        Name = line.ProductName ?? line.SKU,
                        SKU = line.SKU,
                        SalesPrice = line.PricePerUnit ?? 0,
                        Unit = "pcs",
                        IsActive = true
                    };

                    var created = await _katanaService.CreateProductAsync(newProduct);

                    if (created != null)
                    {
                        if (!int.TryParse(created.Id, out var createdProductId))
                        {
                            _logger.LogWarning("Katana product created but ID is not numeric. Sku={Sku}, Id={Id}", line.SKU, created.Id);
                            syncResults.Add(new { sku = line.SKU, success = false, action = "created", error = "Katana ürün oluşturuldu ama ID parse edilemedi" });
                            failCount++;
                        }
                        else
                        {
                            // Katana API create payload doesn't set on_hand; apply stock via update.
                            var stockUpdated = await _katanaService.UpdateProductAsync(
                                createdProductId,
                                created.Name,
                                created.SalesPrice,
                                intendedStock);

                            if (stockUpdated)
                            {
                                _logger.LogInformation("✅ Katana ürün oluşturuldu ve stok set edildi: {SKU}, Stock: {Stock}", line.SKU, intendedStock);
                                syncResults.Add(new { sku = line.SKU, success = true, action = "created", newStock = intendedStock });
                                successCount++;
                            }
                            else
                            {
                                _logger.LogWarning("Katana product created but stock update failed. Sku={Sku}, ProductId={ProductId}", line.SKU, createdProductId);
                                syncResults.Add(new { sku = line.SKU, success = false, action = "created", error = "Katana ürün oluşturuldu ama stok set edilemedi" });
                                failCount++;
                            }
                        }
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Katana ürün oluşturulamadı: {SKU}", line.SKU);
                        syncResults.Add(new { sku = line.SKU, success = false, error = "Katana ürün oluşturma başarısız (CreateProductAsync=null)" });
                        failCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Katana sync hatası: {SKU}", line.SKU);
                syncResults.Add(new { sku = line.SKU, success = false, error = ex.Message });
                failCount++;
            }
        }

        // Sipariş durumunu güncelle
        order.Status = failCount == 0 ? "APPROVED" : "APPROVED_WITH_ERRORS";
        order.LastSyncError = failCount == 0 ? null : $"Katana stock sync failed for {failCount} line(s).";
        order.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

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
