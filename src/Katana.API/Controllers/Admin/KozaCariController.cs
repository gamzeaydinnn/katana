using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Katana.Business.Interfaces;
using Katana.Core.DTOs.Koza;
using Katana.Data.Context;
using Katana.Core.Entities;
using Katana.Core.DTOs;
using System.Text.Json;
using Katana.Business.Extensions;
using System.IO;

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
    /// Koza'daki tedarikçi carilerini listele (mapping + Katana Suppliers JOIN)
    /// GET /api/admin/koza/cari/suppliers
    /// </summary>
    [HttpGet("suppliers")]
    [ProducesResponseType(typeof(IReadOnlyList<KozaSupplierListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ListSuppliers(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Listing Koza supplier caris from mapping table with Katana supplier details");
            
            // Mapping tablosu ile Suppliers tablosunu JOIN et
            var query = from mapping in _dbContext.SupplierKozaCariMappings
                        join supplier in _dbContext.Suppliers on mapping.KatanaSupplierId equals supplier.KatanaId into supplierGroup
                        from supplier in supplierGroup.DefaultIfEmpty()
                        where mapping.SyncStatus == "SUCCESS"
                        orderby mapping.UpdatedAt descending
                        select new
                        {
                            Mapping = mapping,
                            Supplier = supplier
                        };
            
            var results = await query.ToListAsync(ct);
            
            // DTO'ya dönüştür
            var suppliers = results.Select(r => new KozaSupplierListItemDto
            {
                FinansalNesneId = r.Mapping.KozaFinansalNesneId,
                Kod = r.Mapping.KozaCariKodu ?? $"TED-{r.Mapping.KatanaSupplierId}",
                Tanim = r.Mapping.KozaCariTanim ?? r.Mapping.KatanaSupplierName ?? "",
                VergiNo = r.Supplier?.TaxNo,
                Telefon = r.Supplier?.Phone,
                Email = r.Supplier?.Email
            }).ToList();
            
            _logger.LogInformation("Retrieved {Count} suppliers from mapping table", suppliers.Count);
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
                    KatanaSupplierId = katanaSupplier.Id.ToString(),
                    SupplierName = katanaSupplier.Name
                };
                var statusLabel = "Unknown";

                try
                {
                    // Mapping tablosunda var mı kontrol et
                    var existingMapping = await _dbContext.SupplierKozaCariMappings
                        .FirstOrDefaultAsync(m => m.KatanaSupplierId == katanaSupplier.Id.ToString(), ct);

                    if (existingMapping != null)
                    {
                        var now = DateTime.UtcNow;
                        existingMapping.KozaCariKodu ??= $"TED-{katanaSupplier.Id}";
                        existingMapping.KozaCariTanim ??= katanaSupplier.Name;
                        existingMapping.KatanaSupplierName = katanaSupplier.Name;
                        existingMapping.SyncStatus = "SUCCESS";
                        existingMapping.LastSyncAt = now;
                        existingMapping.LastSyncError = null;
                        existingMapping.UpdatedAt = now;
                        existingMapping.UpdateHash();
                        await _dbContext.SaveChangesAsync(ct);

                        // Zaten senkronize edilmiş
                        item.KozaCariKodu = existingMapping.KozaCariKodu;
                        item.KozaFinansalNesneId = existingMapping.KozaFinansalNesneId;
                        item.Success = true;
                        item.Message = "Tedarikçi cari zaten Luca'da mevcut";
                        result.SkippedCount++;
                        statusLabel = "Skipped";
                        
                        _logger.LogInformation(
                            "Supplier sync skipped: KatanaSupplierId={Id}, KatanaName={Name}, KozaCariKodu={Kod}, KozaFinansalNesneId={FinId}",
                            katanaSupplier.Id,
                            katanaSupplier.Name,
                            existingMapping.KozaCariKodu,
                            existingMapping.KozaFinansalNesneId);
                    }
                    else
                    {
                        // Koza'da oluştur
                        var supplierDto = new KatanaSupplierToCariDto
                        {
                            KatanaSupplierId = katanaSupplier.Id.ToString(),
                            Name = katanaSupplier.Name,
                            Email = katanaSupplier.Email,
                            Phone = katanaSupplier.Phone,
                            TaxNo = katanaSupplier.TaxNo
                        };

                        // Retry logic: 3 deneme, her denemede exponential backoff
                        KozaResult? kozaResult = null;
                        var maxRetries = 3;
                        var retryDelay = 500; // 500ms başlangıç

                        for (int retry = 0; retry < maxRetries; retry++)
                        {
                            try
                            {
                                kozaResult = await _lucaService.EnsureSupplierCariAsync(supplierDto, ct);
                                
                                // Başarılı ise döngüden çık
                                if (kozaResult.Success)
                                {
                                    break;
                                }
                                
                                // Başarısız ama retry yapılabilir mi kontrol et
                                if (retry < maxRetries - 1 && 
                                    (kozaResult.Message?.Contains("Connection reset") == true ||
                                     kozaResult.Message?.Contains("transport connection") == true))
                                {
                                    _logger.LogWarning("Retry {Retry}/{Max} for supplier {Id} after {Delay}ms", 
                                        retry + 1, maxRetries, katanaSupplier.Id, retryDelay);
                                    await Task.Delay(retryDelay, ct);
                                    retryDelay *= 2; // Exponential backoff
                                }
                                else
                                {
                                    break; // Retry yapılamaz hata, çık
                                }
                            }
                            catch (HttpRequestException httpEx) when (
                                httpEx.InnerException is IOException ioEx && 
                                ioEx.Message.Contains("Connection reset"))
                            {
                                if (retry < maxRetries - 1)
                                {
                                    _logger.LogWarning("Connection reset, retry {Retry}/{Max} for supplier {Id} after {Delay}ms", 
                                        retry + 1, maxRetries, katanaSupplier.Id, retryDelay);
                                    await Task.Delay(retryDelay, ct);
                                    retryDelay *= 2;
                                }
                                else
                                {
                                    kozaResult = new KozaResult { Success = false, Message = httpEx.Message };
                                }
                            }
                        }

                        // Throttling: Her istek arasında 200ms bekle
                        await Task.Delay(200, ct);

                        if (kozaResult?.Success == true)
                        {
                            // Mapping kaydet
                            var now = DateTime.UtcNow;
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
                                KatanaSupplierId = katanaSupplier.Id.ToString(),
                                KozaCariKodu = cariKodu,
                                KozaFinansalNesneId = finansalNesneId,
                                KatanaSupplierName = katanaSupplier.Name,
                                KozaCariTanim = supplierDto.Name ?? katanaSupplier.Name,
                                SyncStatus = "SUCCESS",
                                LastSyncAt = now,
                                LastSyncError = null,
                                CreatedAt = now,
                                UpdatedAt = now
                            };
                            mapping.UpdateHash();

                            _dbContext.SupplierKozaCariMappings.Add(mapping);
                            await _dbContext.SaveChangesAsync(ct);

                            item.KozaCariKodu = cariKodu;
                            item.KozaFinansalNesneId = finansalNesneId;
                            item.Success = true;
                            item.Message = kozaResult.Message ?? "Tedarikçi cari başarıyla oluşturuldu";
                            result.SuccessCount++;
                            statusLabel = "Success";

                            _logger.LogInformation(
                                "Supplier sync ok: KatanaSupplierId={Id}, KatanaName={Name}, KozaCariKodu={Kod}, KozaFinansalNesneId={FinId}",
                                katanaSupplier.Id,
                                katanaSupplier.Name,
                                cariKodu,
                                finansalNesneId);
                        }
                        else
                        {
                            item.Success = false;
                            item.Message = kozaResult?.Message ?? "Tedarikçi cari oluşturulamadı";
                            result.ErrorCount++;
                            statusLabel = "Error";
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
                    statusLabel = "Error";
                }

                _logger.LogInformation("Supplier sync result: KatanaSupplierId={Id}, Status={Status}, Message={Message}",
                    item.KatanaSupplierId,
                    statusLabel,
                    item.Message ?? "-");

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

    #region Müşteri İşlemleri

    /// <summary>
    /// Koza'daki müşteri carilerini listele (Luca Koza API'den)
    /// GET /api/admin/koza/cari/customers
    /// </summary>
    [HttpGet("customers")]
    [ProducesResponseType(typeof(IReadOnlyList<KozaCustomerListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ListCustomers(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Listing Koza customer caris from Luca API");
            
            // Luca Koza API'den müşteri listesini çek
            var customers = await _lucaService.ListMusteriCustomerItemsAsync(ct);
            
            _logger.LogInformation("Retrieved {Count} customers from Koza", customers.Count);
            return Ok(customers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list Koza customers");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Katana Customer → Koza Müşteri Cari toplu senkronizasyonu
    /// POST /api/admin/koza/cari/customers/sync
    /// </summary>
    [HttpPost("customers/sync")]
    [ProducesResponseType(typeof(CustomerSyncResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SyncCustomers(CancellationToken ct)
    {
        var result = new CustomerSyncResult();
        
        try
        {
            _logger.LogInformation("Starting Katana → Koza customer sync");

            // 1. Katana'dan customer'ları çek
            var katanaCustomers = await _katanaService.GetCustomersAsync();
            result.TotalCount = katanaCustomers.Count;

            _logger.LogInformation("Found {Count} customers in Katana", katanaCustomers.Count);

            if (katanaCustomers.Count == 0)
            {
                result.ErrorMessage = "Katana'da müşteri bulunamadı";
                return Ok(result);
            }

            // 2. Her customer için Koza'da cari oluştur/kontrol et
            foreach (var katanaCustomer in katanaCustomers)
            {
                var item = new CustomerSyncItem
                {
                    KatanaCustomerId = katanaCustomer.Id.ToString(),
                    CustomerName = katanaCustomer.Name
                };
                var statusLabel = "Unknown";

                try
                {
                    // Mapping tablosunda var mı kontrol et
                    var customerIdStr = katanaCustomer.Id.ToString();
                    var existingMapping = await _dbContext.CustomerKozaCariMappings
                        .FirstOrDefaultAsync(m => m.KatanaCustomerId == customerIdStr, ct);

                    if (existingMapping != null)
                    {
                        var now = DateTime.UtcNow;
                        existingMapping.KozaCariKodu ??= $"MUS-{katanaCustomer.Id}";
                        existingMapping.KozaCariTanim ??= katanaCustomer.Name;
                        existingMapping.KatanaCustomerName = katanaCustomer.Name;
                        existingMapping.SyncStatus = "SUCCESS";
                        existingMapping.LastSyncAt = now;
                        existingMapping.LastSyncError = null;
                        existingMapping.UpdatedAt = now;
                        existingMapping.UpdateHash();
                        await _dbContext.SaveChangesAsync(ct);

                        // Zaten senkronize edilmiş
                        item.KozaCariKodu = existingMapping.KozaCariKodu;
                        item.KozaFinansalNesneId = existingMapping.KozaFinansalNesneId;
                        item.Success = true;
                        item.Message = "Müşteri cari zaten Luca'da mevcut";
                        result.SkippedCount++;
                        statusLabel = "Skipped";
                        
                        _logger.LogInformation(
                            "Customer sync skipped: KatanaCustomerId={Id}, KatanaName={Name}, KozaCariKodu={Kod}, KozaFinansalNesneId={FinId}",
                            katanaCustomer.Id,
                            katanaCustomer.Name,
                            existingMapping.KozaCariKodu,
                            existingMapping.KozaFinansalNesneId);
                    }
                    else
                    {
                        // Koza'da oluştur
                        var customerDto = new KatanaCustomerToCariDto
                        {
                            KatanaCustomerId = customerIdStr,
                            Code = $"MUS-{katanaCustomer.Id}",
                            Name = katanaCustomer.Name,
                            Email = katanaCustomer.Email,
                            Phone = katanaCustomer.Phone,
                            TaxNo = null // KatanaCustomerDto doesn't have TaxNo field
                        };

                        // Retry logic: connection reset durumları için sabit gecikmeler
                        KozaResult? kozaResult = null;
                        var connectionResetDelays = new[] { 500, 1000, 2000, 4000 };

                        for (int attempt = 0; attempt <= connectionResetDelays.Length; attempt++)
                        {
                            try
                            {
                                kozaResult = await _lucaService.EnsureCustomerCariAsync(customerDto, ct);
                                
                                // Başarılı ise döngüden çık
                                if (kozaResult.Success)
                                {
                                    break;
                                }
                                
                                // Başarısız ama retry yapılabilir mi kontrol et
                                if (attempt < connectionResetDelays.Length && 
                                    (kozaResult.Message?.Contains("Connection reset") == true ||
                                     kozaResult.Message?.Contains("transport connection") == true))
                                {
                                    var delayMs = connectionResetDelays[attempt];
                                    _logger.LogWarning("Retry {Retry}/{Max} for customer {Code} after {Delay}ms", 
                                        attempt + 1, connectionResetDelays.Length + 1, customerDto.Code, delayMs);
                                    await Task.Delay(delayMs, ct);
                                }
                                else
                                {
                                    break; // Retry yapılamaz hata, çık
                                }
                            }
                            catch (HttpRequestException httpEx) when (
                                httpEx.InnerException is IOException ioEx && 
                                ioEx.Message.Contains("Connection reset"))
                            {
                                if (attempt < connectionResetDelays.Length)
                                {
                                    var delayMs = connectionResetDelays[attempt];
                                    _logger.LogWarning("Connection reset, retry {Retry}/{Max} for customer {Code} after {Delay}ms", 
                                        attempt + 1, connectionResetDelays.Length + 1, customerDto.Code, delayMs);
                                    await Task.Delay(delayMs, ct);
                                }
                                else
                                {
                                    kozaResult = new KozaResult { Success = false, Message = httpEx.Message };
                                }
                            }
                        }

                        // Throttling: Her istek arasında 350-1000ms arası jitter'lı bekleme
                        await Task.Delay(Random.Shared.Next(350, 1001), ct);

                        if (kozaResult?.Success == true)
                        {
                            // Mapping kaydet
                            var now = DateTime.UtcNow;
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

                            cariKodu ??= $"MUS-{katanaCustomer.Id}";

                            var mapping = new CustomerKozaCariMapping
                            {
                                KatanaCustomerId = customerIdStr,
                                KozaCariKodu = cariKodu,
                                KozaFinansalNesneId = finansalNesneId,
                                KatanaCustomerName = katanaCustomer.Name,
                                KozaCariTanim = customerDto.Name ?? katanaCustomer.Name,
                                KatanaCustomerTaxNo = null, // KatanaCustomerDto doesn't have TaxNo field
                                SyncStatus = "SUCCESS",
                                LastSyncAt = now,
                                LastSyncError = null,
                                CreatedAt = now,
                                UpdatedAt = now
                            };
                            mapping.UpdateHash();

                            _dbContext.CustomerKozaCariMappings.Add(mapping);
                            await _dbContext.SaveChangesAsync(ct);

                            item.KozaCariKodu = cariKodu;
                            item.KozaFinansalNesneId = finansalNesneId;
                            item.Success = true;
                            item.Message = kozaResult.Message ?? "Müşteri cari başarıyla oluşturuldu";
                            result.SuccessCount++;
                            statusLabel = "Success";

                            _logger.LogInformation(
                                "Customer sync ok: KatanaCustomerId={Id}, KatanaName={Name}, KozaCariKodu={Kod}, KozaFinansalNesneId={FinId}",
                                katanaCustomer.Id,
                                katanaCustomer.Name,
                                cariKodu,
                                finansalNesneId);
                        }
                        else
                        {
                            item.Success = false;
                            item.Message = kozaResult?.Message ?? "Müşteri cari oluşturulamadı";
                            result.ErrorCount++;
                            statusLabel = "Error";
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to sync customer {Id}: {Name}", 
                        katanaCustomer.Id, katanaCustomer.Name);
                    item.Success = false;
                    item.Message = ex.Message;
                    result.ErrorCount++;
                    statusLabel = "Error";
                }

                _logger.LogInformation("Customer sync result: KatanaCustomerId={Id}, Status={Status}, Message={Message}",
                    item.KatanaCustomerId,
                    statusLabel,
                    item.Message ?? "-");

                result.Items.Add(item);
            }

            _logger.LogInformation(
                "Customer sync completed: Total={Total}, Success={Success}, Skipped={Skipped}, Error={Error}",
                result.TotalCount, result.SuccessCount, result.SkippedCount, result.ErrorCount);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Customer sync failed");
            result.ErrorMessage = ex.Message;
            return StatusCode(500, result);
        }
    }

    /// <summary>
    /// Customer mapping'leri listele
    /// GET /api/admin/koza/cari/customers/mappings
    /// </summary>
    [HttpGet("customers/mappings")]
    [ProducesResponseType(typeof(List<CustomerKozaCariMapping>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCustomerMappings(CancellationToken ct)
    {
        try
        {
            var mappings = await _dbContext.CustomerKozaCariMappings
                .OrderByDescending(m => m.UpdatedAt)
                .ToListAsync(ct);
            
            return Ok(mappings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get customer mappings");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    #endregion
}
