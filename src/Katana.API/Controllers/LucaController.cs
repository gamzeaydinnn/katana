using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Katana.Data.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Katana.API.Controllers;

/// <summary>
/// Luca entegrasyonu için batch işlem API'leri.
/// Toplu ürün gönderimi, durum takibi ve yönetim endpoint'leri.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,StokYonetici")]
public class LucaController : ControllerBase
{
    private readonly IBatchJobService _batchJobService;
    private readonly ILucaService _lucaService;
    private readonly IntegrationDbContext _context;
    private readonly ILogger<LucaController> _logger;
    private readonly IAuditService _auditService;

    public LucaController(
        IBatchJobService batchJobService,
        ILucaService lucaService,
        IntegrationDbContext context,
        ILogger<LucaController> logger,
        IAuditService auditService)
    {
        _batchJobService = batchJobService;
        _lucaService = lucaService;
        _context = context;
        _logger = logger;
        _auditService = auditService;
    }

    /// <summary>
    /// Toplu ürün gönderimi başlatır.
    /// Ürünler batch'ler halinde arka planda Luca'ya gönderilir.
    /// </summary>
    /// <param name="request">Batch gönderim parametreleri</param>
    /// <returns>Job bilgisi ve durum takip URL'i</returns>
    [HttpPost("push-products-batch")]
    [ProducesResponseType(typeof(BatchJobCreatedResponse), 202)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> PushProductsBatch([FromBody] BatchPushRequest? request = null)
    {
        try
        {
            request ??= new BatchPushRequest();
            
            // Varsayılan değerler
            if (request.BatchSize <= 0) request.BatchSize = 100;
            if (request.DelayBetweenBatchesMs < 0) request.DelayBetweenBatchesMs = 1000;

            // Ürün ID'lerini belirle
            List<int> productIds;

            if (request.ProductIds != null && request.ProductIds.Count > 0)
            {
                // Belirtilen ID'leri kullan
                productIds = request.ProductIds;
            }
            else if (request.OnlyUpdated)
            {
                // Son X saat içinde güncellenen ürünler
                var since = DateTime.UtcNow.AddHours(-request.UpdatedWithinHours);
                productIds = await _context.Products
                    .Where(p => p.UpdatedAt >= since)
                    .OrderByDescending(p => p.UpdatedAt)
                    .Select(p => p.Id)
                    .ToListAsync();
            }
            else
            {
                // Tüm ürünler
                productIds = await _context.Products
                    .OrderBy(p => p.Id)
                    .Select(p => p.Id)
                    .ToListAsync();
            }

            if (productIds.Count == 0)
            {
                return BadRequest(new { error = "Gönderilecek ürün bulunamadı" });
            }

            // Request'e ID'leri ekle
            request.ProductIds = productIds;

            // Kullanıcı adını al
            var userName = User.Identity?.Name ?? "anonymous";

            // Batch job oluştur
            var response = await _batchJobService.CreateBatchJobAsync(request, userName);

            // Audit log
            _auditService.LogSync("BatchPushStarted", userName,
                $"Batch push başlatıldı: {productIds.Count} ürün, JobId: {response.JobId}");

            _logger.LogInformation(
                "Batch push job oluşturuldu: {JobId}, {Count} ürün, {Batches} batch",
                response.JobId, productIds.Count, response.TotalBatches);

            // 202 Accepted döndür (işlem arka planda devam edecek)
            return AcceptedAtAction(nameof(GetBatchStatus), new { jobId = response.JobId }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Batch push job oluşturulurken hata");
            return StatusCode(500, new { error = "Batch job oluşturulamadı", detail = ex.Message });
        }
    }

    /// <summary>
    /// Batch job durumunu sorgular.
    /// </summary>
    /// <param name="jobId">Job ID</param>
    /// <returns>Job durumu ve ilerleme bilgisi</returns>
    [HttpGet("batch-status/{jobId}")]
    [AllowAnonymous] // UI'dan kolayca sorgulanabilsin
    [ProducesResponseType(typeof(BatchJobStatusDto), 200)]
    [ProducesResponseType(404)]
    public IActionResult GetBatchStatus(string jobId)
    {
        var status = _batchJobService.GetJobStatus(jobId);

        if (status == null)
        {
            return NotFound(new { error = "Job bulunamadı", jobId });
        }

        return Ok(status);
    }

    /// <summary>
    /// Tüm aktif batch job'ları listeler.
    /// </summary>
    /// <returns>Aktif job'ların listesi</returns>
    [HttpGet("batch-jobs")]
    [ProducesResponseType(typeof(ActiveBatchJobsResponse), 200)]
    public IActionResult GetActiveBatchJobs()
    {
        var jobs = _batchJobService.GetActiveJobs();
        return Ok(jobs);
    }

    /// <summary>
    /// Çalışan bir batch job'u iptal eder.
    /// </summary>
    /// <param name="jobId">İptal edilecek job ID</param>
    /// <param name="request">İptal sebebi (opsiyonel)</param>
    /// <returns>İptal durumu</returns>
    [HttpPost("batch-cancel/{jobId}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public IActionResult CancelBatchJob(string jobId, [FromBody] CancelBatchJobRequest? request = null)
    {
        var userName = User.Identity?.Name ?? "anonymous";
        var reason = request?.Reason ?? "Kullanıcı tarafından iptal edildi";

        var success = _batchJobService.CancelJob(jobId, userName, reason);

        if (!success)
        {
            var status = _batchJobService.GetJobStatus(jobId);
            if (status == null)
            {
                return NotFound(new { error = "Job bulunamadı", jobId });
            }
            return BadRequest(new { error = "Job iptal edilemedi", currentStatus = status.Status.ToString() });
        }

        _auditService.LogSync("BatchPushCancelled", userName,
            $"Batch push iptal edildi: {jobId}, Sebep: {reason}");

        _logger.LogInformation("Batch job iptal edildi: {JobId}, Sebep: {Reason}", jobId, reason);

        return Ok(new { message = "Job başarıyla iptal edildi", jobId });
    }

    /// <summary>
    /// Luca bağlantısını test eder.
    /// </summary>
    /// <returns>Bağlantı durumu</returns>
    [HttpGet("test-connection")]
    [AllowAnonymous]
    [ProducesResponseType(200)]
    public async Task<IActionResult> TestConnection()
    {
        try
        {
            var isConnected = await _lucaService.TestConnectionAsync();
            return Ok(new 
            { 
                connected = isConnected, 
                timestamp = DateTime.UtcNow,
                message = isConnected ? "Luca bağlantısı başarılı" : "Luca bağlantısı kurulamadı"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Luca bağlantı testi başarısız");
            return Ok(new 
            { 
                connected = false, 
                timestamp = DateTime.UtcNow,
                message = $"Bağlantı hatası: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Gönderilecek ürün sayısını döndürür (önizleme).
    /// </summary>
    /// <param name="onlyUpdated">Sadece güncellenmiş ürünler</param>
    /// <param name="updatedWithinHours">Son X saat içinde güncellenenler</param>
    /// <returns>Ürün sayısı</returns>
    [HttpGet("preview-push")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> PreviewPush(
        [FromQuery] bool onlyUpdated = false,
        [FromQuery] int updatedWithinHours = 24)
    {
        try
        {
            IQueryable<Katana.Core.Entities.Product> query = _context.Products;

            if (onlyUpdated)
            {
                var since = DateTime.UtcNow.AddHours(-updatedWithinHours);
                query = query.Where(p => p.UpdatedAt >= since);
            }

            var count = await query.CountAsync();
            var estimatedBatches = (int)Math.Ceiling((double)count / 100);
            var estimatedTimeMinutes = estimatedBatches * 1.5; // Tahmini süre (batch başına ~1.5 dakika)

            return Ok(new
            {
                totalProducts = count,
                estimatedBatches,
                estimatedTimeMinutes = Math.Round(estimatedTimeMinutes, 1),
                batchSize = 100,
                onlyUpdated,
                updatedWithinHours = onlyUpdated ? updatedWithinHours : (int?)null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Push önizleme hatası");
            return StatusCode(500, new { error = "Önizleme alınamadı", detail = ex.Message });
        }
    }

    /// <summary>
    /// Tek bir ürünü Luca'ya gönderir (test amaçlı).
    /// </summary>
    /// <param name="id">Ürün ID</param>
    /// <returns>Gönderim sonucu</returns>
    [HttpPost("push-product/{id:int}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> PushSingleProduct(int id)
    {
        try
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound(new { error = "Ürün bulunamadı", id });
            }

            var stockCard = Katana.Core.Helpers.MappingHelper.MapToLucaStockCard(product);
            var result = await _lucaService.SendStockCardsAsync(new List<LucaCreateStokKartiRequest> { stockCard });

            return Ok(new
            {
                success = result.SuccessfulRecords > 0,
                productId = id,
                productCode = product.SKU,
                productName = product.Name,
                result = new
                {
                    processed = result.ProcessedRecords,
                    successful = result.SuccessfulRecords,
                    failed = result.FailedRecords,
                    message = result.Message,
                    errors = result.Errors
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tek ürün gönderimi başarısız: {Id}", id);
            return StatusCode(500, new { error = "Ürün gönderilemedi", detail = ex.Message });
        }
    }
}
