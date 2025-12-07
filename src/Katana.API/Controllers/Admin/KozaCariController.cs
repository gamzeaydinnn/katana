using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Katana.Business.Interfaces;
using Katana.Core.DTOs.Koza;
using Katana.Data.Context;
using Katana.Core.Entities;
using Katana.Core.DTOs;
using System.Text.Json;

namespace Katana.API.Controllers.Admin;

/// <summary>
/// Koza Cari (Müşteri/Tedarikçi) yönetimi endpoint'leri
/// Frontend bu endpoint'ler üzerinden Koza cari işlemlerini yapar
/// </summary>
[ApiController]
[Route("api/admin/koza/cari")]
[AllowAnonymous]  // TODO: Restore [Authorize(Roles = "Admin")] after CORS testing
public sealed class KozaCariController : ControllerBase
{
    private readonly ILucaService _lucaService;
    private readonly IKatanaService _katanaService;
    private readonly IntegrationDbContext _dbContext;
    private readonly ILogger<KozaCariController> _logger;

    public KozaCariController(
        ILucaService lucaService,
        IKatanaService katanaService,
        IntegrationDbContext dbContext,
        ILogger<KozaCariController> logger)
    {
        _lucaService = lucaService;
        _katanaService = katanaService;
        _dbContext = dbContext;
        _logger = logger;
    }

    #region Cari Yardımcı Endpoint'ler

    /// <summary>
    /// Cari adres listesi
    /// GET /api/admin/koza/cari/{finansalNesneId}/addresses
    /// </summary>
    [HttpGet("{finansalNesneId:long}/addresses")]
    [ProducesResponseType(typeof(JsonElement), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAddresses(long finansalNesneId, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Getting addresses for finansalNesneId: {Id}", finansalNesneId);
            var result = await _lucaService.ListCariAddressesAsync(finansalNesneId, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get addresses for finansalNesneId: {Id}", finansalNesneId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Cari çalışma koşulları
    /// GET /api/admin/koza/cari/work-conditions/{calismaKosulId}
    /// </summary>
    [HttpGet("work-conditions/{calismaKosulId:long}")]
    [ProducesResponseType(typeof(JsonElement), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetWorkConditions(long calismaKosulId, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Getting work conditions for calismaKosulId: {Id}", calismaKosulId);
            var result = await _lucaService.GetCariCalismaKosulAsync(calismaKosulId, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get work conditions for calismaKosulId: {Id}", calismaKosulId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Cari yetkili kişiler listesi
    /// GET /api/admin/koza/cari/{finansalNesneId}/authorized
    /// </summary>
    [HttpGet("{finansalNesneId:long}/authorized")]
    [ProducesResponseType(typeof(JsonElement), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAuthorizedPersons(long finansalNesneId, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Getting authorized persons for finansalNesneId: {Id}", finansalNesneId);
            var result = await _lucaService.ListCariYetkililerAsync(finansalNesneId, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get authorized persons for finansalNesneId: {Id}", finansalNesneId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Cari hareket ekleme
    /// POST /api/admin/koza/cari/movements
    /// </summary>
    [HttpPost("movements")]
    [ProducesResponseType(typeof(KozaResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateMovement(
        [FromBody] KozaCariHareketRequest request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.CariKodu))
            {
                return BadRequest(new { error = "Cari kodu zorunludur" });
            }

            if (request.CariTuru != 1 && request.CariTuru != 2)
            {
                return BadRequest(new { error = "Cari türü 1 (Müşteri) veya 2 (Tedarikçi) olmalıdır" });
            }

            _logger.LogInformation("Creating cari movement: CariKodu={CariKodu}, Tür={Tur}", 
                request.CariKodu, request.CariTuru == 1 ? "Müşteri" : "Tedarikçi");

            var result = await _lucaService.CreateCariHareketAsync(request, ct);
            
            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create cari movement");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    #endregion

    #region Tedarikçi İşlemleri

    /// <summary>
    /// Koza'daki tedarikçi carilerini listele
    /// GET /api/admin/koza/cari/suppliers
    /// </summary>
    [HttpGet("suppliers")]
    [ProducesResponseType(typeof(IReadOnlyList<KozaCariDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ListSuppliers(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Listing Koza supplier caris");
            var suppliers = await _lucaService.ListTedarikciCarilerAsync(ct);
            
            _logger.LogInformation("Retrieved {Count} suppliers from Koza", suppliers.Count);
            return Ok(suppliers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list Koza suppliers");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Katana Supplier → Koza Tedarikçi Cari toplu senkronizasyonu
    /// POST /api/admin/koza/cari/suppliers/sync
    /// </summary>
    [HttpPost("suppliers/sync")]
    [ProducesResponseType(typeof(SupplierSyncResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SyncSuppliers(CancellationToken ct)
    {
        var result = new SupplierSyncResult();
        
        try
        {
            _logger.LogInformation("Starting Katana → Koza supplier sync");

            // 1. Katana'dan supplier'ları çek
            var katanaSuppliers = await _katanaService.GetSuppliersAsync();
            result.TotalCount = katanaSuppliers.Count;

            _logger.LogInformation("Found {Count} suppliers in Katana", katanaSuppliers.Count);

            if (katanaSuppliers.Count == 0)
            {
                result.ErrorMessage = "Katana'da tedarikçi bulunamadı";
                return Ok(result);
            }

            // 2. Her supplier için Koza'da cari oluştur/kontrol et
            foreach (var katanaSupplier in katanaSuppliers)
            {
                var item = new SupplierSyncItem
                {
                    KatanaSupplierId = katanaSupplier.Id,
                    SupplierName = katanaSupplier.Name
                };

                try
                {
                    // Mapping tablosunda var mı kontrol et
                    var existingMapping = await _dbContext.SupplierKozaCariMappings
                        .FirstOrDefaultAsync(m => m.KatanaSupplierId == katanaSupplier.Id, ct);

                    if (existingMapping != null)
                    {
                        // Zaten senkronize edilmiş
                        item.KozaCariKodu = existingMapping.KozaCariKodu;
                        item.KozaFinansalNesneId = existingMapping.KozaFinansalNesneId;
                        item.Success = true;
                        item.Message = "Zaten senkronize edilmiş";
                        result.SkippedCount++;
                    }
                    else
                    {
                        // Koza'da oluştur
                        var supplierDto = new KatanaSupplierToCariDto
                        {
                            KatanaSupplierId = katanaSupplier.Id,
                            Name = katanaSupplier.Name,
                            Email = katanaSupplier.Email,
                            Phone = katanaSupplier.Phone,
                            TaxNo = katanaSupplier.TaxNo
                        };

                        var kozaResult = await _lucaService.EnsureSupplierCariAsync(supplierDto, ct);

                        if (kozaResult.Success)
                        {
                            // Mapping kaydet
                            string? cariKodu = null;
                            long? finansalNesneId = null;

                            if (kozaResult.Data != null)
                            {
                                var dataJson = JsonSerializer.Serialize(kozaResult.Data);
                                var dataObj = JsonSerializer.Deserialize<JsonElement>(dataJson);
                                
                                if (dataObj.TryGetProperty("CariKodu", out var ck))
                                    cariKodu = ck.GetString();
                                if (dataObj.TryGetProperty("FinansalNesneId", out var fn) && fn.ValueKind == JsonValueKind.Number)
                                    finansalNesneId = fn.GetInt64();
                            }

                            cariKodu ??= $"TED-{katanaSupplier.Id}";

                            var mapping = new SupplierKozaCariMapping
                            {
                                KatanaSupplierId = katanaSupplier.Id,
                                KozaCariKodu = cariKodu,
                                KozaFinansalNesneId = finansalNesneId,
                                KatanaSupplierName = katanaSupplier.Name,
                                KozaCariTanim = katanaSupplier.Name,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };

                            _dbContext.SupplierKozaCariMappings.Add(mapping);
                            await _dbContext.SaveChangesAsync(ct);

                            item.KozaCariKodu = cariKodu;
                            item.KozaFinansalNesneId = finansalNesneId;
                            item.Success = true;
                            item.Message = kozaResult.Message;
                            result.SuccessCount++;
                        }
                        else
                        {
                            item.Success = false;
                            item.Message = kozaResult.Message;
                            result.ErrorCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to sync supplier {Id}: {Name}", 
                        katanaSupplier.Id, katanaSupplier.Name);
                    item.Success = false;
                    item.Message = ex.Message;
                    result.ErrorCount++;
                }

                result.Items.Add(item);
            }

            _logger.LogInformation(
                "Supplier sync completed: Total={Total}, Success={Success}, Skipped={Skipped}, Error={Error}",
                result.TotalCount, result.SuccessCount, result.SkippedCount, result.ErrorCount);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Supplier sync failed");
            result.ErrorMessage = ex.Message;
            return StatusCode(500, result);
        }
    }

    /// <summary>
    /// Supplier mapping'leri listele
    /// GET /api/admin/koza/cari/suppliers/mappings
    /// </summary>
    [HttpGet("suppliers/mappings")]
    [ProducesResponseType(typeof(List<SupplierKozaCariMapping>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSupplierMappings(CancellationToken ct)
    {
        try
        {
            var mappings = await _dbContext.SupplierKozaCariMappings
                .OrderByDescending(m => m.UpdatedAt)
                .ToListAsync(ct);
            
            return Ok(mappings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get supplier mappings");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    #endregion
}
