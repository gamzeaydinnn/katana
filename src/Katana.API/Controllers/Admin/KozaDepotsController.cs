using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Katana.Business.Interfaces;
using Katana.Core.DTOs.Koza;
using Katana.Data.Context;
using Katana.Core.Entities;

namespace Katana.API.Controllers.Admin;

/// <summary>
/// Koza Depo yönetimi endpoint'leri
/// Frontend bu endpoint'ler üzerinden Koza depo işlemlerini yapar
/// Lazy load + batch sync ile performans optimizasyonu
/// </summary>
[ApiController]
[Route("api/admin/koza/depots")]
[AllowAnonymous]  // TODO: Restore [Authorize(Roles = "Admin")] after CORS testing
public sealed class KozaDepotsController : ControllerBase
{
    private readonly ILucaService _lucaService;
    private readonly IntegrationDbContext _context;
    private readonly ILogger<KozaDepotsController> _logger;

    public KozaDepotsController(
        ILucaService lucaService,
        IntegrationDbContext context,
        ILogger<KozaDepotsController> logger)
    {
        _lucaService = lucaService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Yerel cache'den depoları listele (pagination ile) - Koza API'ye GİTMEZ
    /// GET /api/admin/koza/depots?page=1&pageSize=100
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Listing depots from LOCAL DB - Page: {Page}, PageSize: {PageSize}", page, pageSize);
            
            var total = await _context.KozaDepots.AsNoTracking().CountAsync(ct);
            
            if (total == 0)
            {
                _logger.LogWarning("No depots found in local DB. Sync required.");
                return Ok(new
                {
                    data = Array.Empty<object>(),
                    message = "Önce Sync yapmanız gerekiyor. Sync butonuna tıklayın.",
                    pagination = new
                    {
                        currentPage = page,
                        pageSize,
                        totalItems = 0,
                        totalPages = 0
                    }
                });
            }
            
            var depots = await _context.KozaDepots
                .AsNoTracking()
                .OrderBy(d => d.Kod)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new
                {
                    d.Id,
                    d.DepoId,
                    d.Kod,
                    d.Tanim,
                    d.KategoriKod,
                    d.Ulke,
                    d.Il,
                    d.Ilce,
                    d.AdresSerbest,
                    d.CreatedAt,
                    d.UpdatedAt
                })
                .ToListAsync(ct);
            
            _logger.LogInformation("Retrieved {Count}/{Total} depots from LOCAL DB", depots.Count, total);
            
            return Ok(new
            {
                data = depots,
                pagination = new
                {
                    currentPage = page,
                    pageSize,
                    totalItems = total,
                    totalPages = (int)Math.Ceiling(total / (double)pageSize)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list depots from LOCAL DB");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Koza API'den depoları çek ve yerel cache'e senkronize et (batch processing - OPTIMIZED)
    /// POST /api/admin/koza/depots/sync?batchSize=50
    /// </summary>
    [HttpPost("sync")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SyncDepots(
        [FromQuery] int batchSize = 50,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Starting depot sync from Koza API with batch size {BatchSize}", batchSize);
            
            // Koza API'den tüm depoları çek
            var kozaDepots = await _lucaService.ListDepotsAsync(ct);
            _logger.LogInformation("Retrieved {Count} depots from Koza API", kozaDepots.Count);
            
            int added = 0;
            int updated = 0;
            int batches = (int)Math.Ceiling(kozaDepots.Count / (double)batchSize);
            
            for (int i = 0; i < batches; i++)
            {
                var batch = kozaDepots
                    .Skip(i * batchSize)
                    .Take(batchSize)
                    .ToList();
                
                _logger.LogInformation("Processing batch {BatchNum}/{TotalBatches} ({Count} items)", 
                    i + 1, batches, batch.Count);
                
                // Batch içindeki tüm kodları topla
                var batchKodlar = batch
                    .Where(d => !string.IsNullOrWhiteSpace(d.Kod))
                    .Select(d => d.Kod!)
                    .ToList();
                
                // Mevcut kayıtları toplu çek (tek sorguda)
                var existingDepots = await _context.KozaDepots
                    .Where(d => batchKodlar.Contains(d.Kod))
                    .ToDictionaryAsync(d => d.Kod, ct);
                
                foreach (var kozaDepot in batch)
                {
                    // Kod boşsa atla
                    if (string.IsNullOrWhiteSpace(kozaDepot.Kod))
                    {
                        _logger.LogWarning("Skipping depot with empty Kod");
                        continue;
                    }
                    
                    if (!existingDepots.TryGetValue(kozaDepot.Kod, out var existing))
                    {
                        // Yeni kayıt ekle
                        var newDepot = new KozaDepot
                        {
                            DepoId = kozaDepot.DepoId,
                            Kod = kozaDepot.Kod,
                            Tanim = kozaDepot.Tanim ?? "",
                            // Kategori kodu normalizasyonu: "MERKEZ" → "01"
                            KategoriKod = NormalizeKategoriKod(kozaDepot.KategoriKod),
                            Ulke = kozaDepot.Ulke,
                            Il = kozaDepot.Il,
                            Ilce = kozaDepot.Ilce,
                            AdresSerbest = kozaDepot.AdresSerbest,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        
                        await _context.KozaDepots.AddAsync(newDepot, ct);
                        added++;
                    }
                    else
                    {
                        // Mevcut kaydı güncelle
                        existing.DepoId = kozaDepot.DepoId;
                        existing.Tanim = kozaDepot.Tanim ?? existing.Tanim;
                        existing.KategoriKod = NormalizeKategoriKod(kozaDepot.KategoriKod) ?? existing.KategoriKod;
                        existing.Ulke = kozaDepot.Ulke ?? existing.Ulke;
                        existing.Il = kozaDepot.Il ?? existing.Il;
                        existing.Ilce = kozaDepot.Ilce ?? existing.Ilce;
                        existing.AdresSerbest = kozaDepot.AdresSerbest ?? existing.AdresSerbest;
                        existing.UpdatedAt = DateTime.UtcNow;
                        
                        updated++;
                    }
                }
                
                // Batch'i kaydet
                await _context.SaveChangesAsync(ct);
                _logger.LogInformation("Batch {BatchNum} saved successfully", i + 1);
                
                // Rate limiting için kısa bekleme
                if (i < batches - 1)
                {
                    await Task.Delay(100, ct);
                }
            }
            
            _logger.LogInformation("Depot sync completed - Added: {Added}, Updated: {Updated}", added, updated);
            
            return Ok(new
            {
                success = true,
                message = $"Sync tamamlandı: {added} yeni, {updated} güncellendi",
                stats = new
                {
                    totalFromKoza = kozaDepots.Count,
                    added,
                    updated,
                    batchesProcessed = batches
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync depots from Koza");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Koza'da yeni depo oluştur
    /// POST /api/admin/koza/depots/create?idempotent=true
    /// idempotent=true: Mevcut depot'lar skip edilir (idempotent işlem)
    /// idempotent=false (default): Mevcut depot'lar 409 Conflict döner
    /// </summary>
    [HttpPost("create")]
    [ProducesResponseType(typeof(KozaResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        [FromBody] KozaCreateDepotRequest? request,
        CancellationToken ct,
        [FromQuery] bool idempotent = false)
    {
        try
        {
            // Request null kontrolü
            if (request == null)
            {
                _logger.LogWarning("Depot create request is null");
                return BadRequest(new { error = "Request body boş veya geçersiz format" });
            }

            // StkDepo null kontrolü
            if (request.StkDepo == null)
            {
                _logger.LogWarning("Depot create request.StkDepo is null");
                return BadRequest(new { error = "stkDepo alanı zorunludur. Örnek format: { \"stkDepo\": { \"kod\": \"...\", \"tanim\": \"...\", \"kategoriKod\": \"...\" } }" });
            }

            // DEBUG 1: Request ilk geldiği andaki HAM veriyi logla
            _logger.LogWarning("=== DEPOT CREATE - RAW REQUEST (idempotent={Idempotent}) ===", idempotent);
            _logger.LogWarning("RECEIVED Kod: {Kod}", request.StkDepo.Kod ?? "NULL");
            _logger.LogWarning("RECEIVED Tanim: {Tanim}", request.StkDepo.Tanim ?? "NULL");
            _logger.LogWarning("RECEIVED KategoriKod (BEFORE normalization): {KategoriKod}", request.StkDepo.KategoriKod ?? "NULL");
            _logger.LogWarning("RECEIVED SisDepoKategoriAgacKodu: {SisKod}", request.StkDepo.SisDepoKategoriAgacKodu ?? "NULL");
            _logger.LogWarning("RECEIVED Full JSON: {Json}", System.Text.Json.JsonSerializer.Serialize(request));

            if (string.IsNullOrWhiteSpace(request.StkDepo.Kod))
            {
                return BadRequest(new { error = "Depo kodu (kod) zorunludur" });
            }

            if (string.IsNullOrWhiteSpace(request.StkDepo.Tanim))
            {
                return BadRequest(new { error = "Depo adı (tanim) zorunludur" });
            }

            // ✅ FIX 2: KategoriKod tutarsızlığını düzelt
            // Luca'nın beklediği değer sisDepoKategoriAgacKodu'dur
            if (request.StkDepo.KategoriKod != request.StkDepo.SisDepoKategoriAgacKodu)
            {
                _logger.LogWarning(
                    "⚠️ KategoriKod mismatch! kategoriKod={K1}, sisDepoKategoriAgacKodu={K2}. Using sisDepoKategoriAgacKodu.",
                    request.StkDepo.KategoriKod ?? "NULL",
                    request.StkDepo.SisDepoKategoriAgacKodu ?? "NULL"
                );
                
                // Luca'nın beklediği değeri kullan
                request.StkDepo.KategoriKod = request.StkDepo.SisDepoKategoriAgacKodu ?? "002";
            }
            
            // ✅ FIX 4: KategoriKod kontrolü ve normalizasyon
            // "MERKEZ" gibi kategori ADLARI değil, numerik KOD bekleniyor
            // sisDepoKategoriAgacKodu ile uyumlu olmalı
            var originalKategoriKod = request.StkDepo.KategoriKod;
            if (string.IsNullOrWhiteSpace(request.StkDepo.KategoriKod) || 
                request.StkDepo.KategoriKod.Equals("MERKEZ", StringComparison.OrdinalIgnoreCase))
            {
                // sisDepoKategoriAgacKodu ile uyumlu olmalı
                request.StkDepo.KategoriKod = request.StkDepo.SisDepoKategoriAgacKodu ?? "002";
                _logger.LogWarning("KategoriKod set to match SisDepoKategoriAgacKodu: '{KategoriKod}'", 
                    request.StkDepo.KategoriKod);
            }

            // DÜZELTME: Luca depo kategori ağacı varsayılan değerleri ekle
            // Eğer frontend göndermemişse, MERKEZ DEPO için varsayılan değerleri kullan
            if (!request.StkDepo.DepoKategoriAgacId.HasValue)
            {
                request.StkDepo.DepoKategoriAgacId = 11356; // Luca MERKEZ DEPO kategori ağacı ID
                _logger.LogWarning("DepoKategoriAgacId set to default: 11356");
            }
            if (string.IsNullOrWhiteSpace(request.StkDepo.SisDepoKategoriAgacKodu))
            {
                request.StkDepo.SisDepoKategoriAgacKodu = "002"; // Luca MERKEZ DEPO kodu
                _logger.LogWarning("SisDepoKategoriAgacKodu set to default: 002");
            }

            // ✅ FIX 3: Yeni depo oluşturulurken depoId null olmalı
            if (request.StkDepo.DepoId.HasValue && request.StkDepo.DepoId.Value == 0)
            {
                request.StkDepo.DepoId = null; // 0 değerini null'a çevir
                _logger.LogWarning("DepoId was 0, set to null for new depot creation");
            }

            // DEBUG 2: Normalizasyon sonrası veriyi logla
            _logger.LogWarning("=== DEPOT CREATE - AFTER NORMALIZATION ===");
            _logger.LogWarning("NORMALIZED Kod: {Kod}", request.StkDepo.Kod);
            _logger.LogWarning("NORMALIZED Tanim: {Tanim}", request.StkDepo.Tanim);
            _logger.LogWarning("NORMALIZED KategoriKod: {KategoriKod}", request.StkDepo.KategoriKod);
            _logger.LogWarning("NORMALIZED DepoId: {DepoId}", request.StkDepo.DepoId?.ToString() ?? "NULL");
            _logger.LogWarning("NORMALIZED Full JSON: {Json}", System.Text.Json.JsonSerializer.Serialize(request));

            _logger.LogInformation("Creating Koza depot: {Kod} - {Tanim} - {KategoriKod}", 
                request.StkDepo.Kod, request.StkDepo.Tanim, request.StkDepo.KategoriKod);

            // Location Existence Check (EF Core default transaction kullanılıyor)
            // Manuel transaction kaldırıldı - SqlServerRetryingExecutionStrategy ile uyumlu değildi
            var existingDepot = await _context.KozaDepots
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Kod == request.StkDepo.Kod, ct);
            
            // Eğer zaten varsa
            if (existingDepot != null)
            {
                _logger.LogWarning("Location existence check - Depot found: {Kod} (ID: {ExistingId}, Name: {ExistingName})", 
                    request.StkDepo.Kod, existingDepot.Id, existingDepot.Tanim);
                
                // Idempotent mode: skip silently ve success döner
                if (idempotent)
                {
                    _logger.LogInformation("Skipping depot creation (idempotent mode): {Kod} - Already exists with ID {ExistingId}", 
                        request.StkDepo.Kod, existingDepot.Id);
                    
                    return Ok(new KozaResult
                    {
                        Success = true,
                        Message = $"Depo zaten mevcut (ID: {existingDepot.Id}) - İşlem atlandı (idempotent mode)",
                        Data = new
                        {
                            skipped = true,
                            existingId = existingDepot.Id,
                            existingName = existingDepot.Tanim
                        }
                    });
                }
                // Default mode: conflict error döner
                else
                {
                    _logger.LogWarning("Duplicate depot code detected: {Kod} (ID: {ExistingId})", 
                        request.StkDepo.Kod, existingDepot.Id);
                    
                    return Conflict(new
                    {
                        error = "Depo kodu zaten mevcut",
                        code = "DUPLICATE_LOCATION",
                        details = new
                        {
                            locationCode = request.StkDepo.Kod,
                            existingId = existingDepot.Id,
                            existingName = existingDepot.Tanim
                        }
                    });
                }
            }
            
            _logger.LogInformation("Location existence check passed - No existing depot found for code: {Kod}", request.StkDepo.Kod);

            // DEBUG 3: LucaService'e gönderilmeden HEMEN ÖNCE son kontrol
            _logger.LogWarning("=== SENDING TO LUCA SERVICE ===");
            _logger.LogWarning("FINAL REQUEST: {Json}", System.Text.Json.JsonSerializer.Serialize(request));

            var result = await _lucaService.CreateDepotAsync(request, ct);
            
            // DEBUG 4: Koza'dan dönen sonucu logla
            _logger.LogWarning("=== KOZA RESPONSE ===");
            _logger.LogWarning("Success: {Success}", result.Success);
            _logger.LogWarning("Message: {Message}", result.Message ?? "NULL");
            
            // 1) Normal success
            if (result.Success)
            {
                _logger.LogInformation("Depot created successfully: {Kod}", request.StkDepo.Kod);
                
                // Koza'dan başarı dönmüşse local cache'e de ekleyelim
                var now = DateTime.UtcNow;
                var existing = await _context.KozaDepots
                    .FirstOrDefaultAsync(d => d.Kod == request.StkDepo.Kod, ct);
                
                if (existing == null)
                {
                    var newDepot = new KozaDepot
                    {
                        DepoId       = request.StkDepo.DepoId,
                        Kod          = request.StkDepo.Kod!,
                        Tanim        = request.StkDepo.Tanim ?? "",
                        KategoriKod  = NormalizeKategoriKod(request.StkDepo.KategoriKod),
                        Ulke         = request.StkDepo.Ulke,
                        Il           = request.StkDepo.Il,
                        Ilce         = request.StkDepo.Ilce,
                        AdresSerbest = request.StkDepo.AdresSerbest,
                        CreatedAt    = now,
                        UpdatedAt    = now
                    };
                    
                    await _context.KozaDepots.AddAsync(newDepot, ct);
                    await _context.SaveChangesAsync(ct);
                }
                
                return Ok(result);
            }
            
            // 2) Koza: "Depo kodu kullanımda!" → zaten var, idempotent success gibi davran
            if (!string.IsNullOrWhiteSpace(result.Message) &&
                result.Message.Contains("Depo kodu kullanımda", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Koza: 'Depo kodu kullanımda' döndü. {Kod} zaten mevcut, idempotent success olarak ele alınıyor.",
                    request.StkDepo.Kod);
                
                var now = DateTime.UtcNow;
                var existing = await _context.KozaDepots
                    .FirstOrDefaultAsync(d => d.Kod == request.StkDepo.Kod, ct);
                
                if (existing == null)
                {
                    var newDepot = new KozaDepot
                    {
                        DepoId       = request.StkDepo.DepoId, // şu an Koza depoId bilmiyoruz, null kalabilir
                        Kod          = request.StkDepo.Kod!,
                        Tanim        = request.StkDepo.Tanim ?? "",
                        KategoriKod  = NormalizeKategoriKod(request.StkDepo.KategoriKod),
                        Ulke         = request.StkDepo.Ulke,
                        Il           = request.StkDepo.Il,
                        Ilce         = request.StkDepo.Ilce,
                        AdresSerbest = request.StkDepo.AdresSerbest,
                        CreatedAt    = now,
                        UpdatedAt    = now
                    };
                    
                    await _context.KozaDepots.AddAsync(newDepot, ct);
                    await _context.SaveChangesAsync(ct);
                }
                
                return Ok(new KozaResult
                {
                    Success = true,
                    Message = "Depo Koza'da zaten mevcut (kod kullanımda).",
                    Data = new
                    {
                        alreadyExists = true,
                        code = request.StkDepo.Kod
                    }
                });
            }
            
            // 3) Diğer tüm hatalar normal şekilde 400 dönsün
            _logger.LogWarning("Depot creation failed: {Message}", result.Message);
            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Koza depot");
            return StatusCode(500, new { error = ex.Message });
        }
    }
    
    /// <summary>
    /// Kategori kodunu normalize et
    /// "MERKEZ", "merkez" gibi ADları numerik KODA çevir
    /// </summary>
    private static string NormalizeKategoriKod(string? kategoriKod)
    {
        if (string.IsNullOrWhiteSpace(kategoriKod))
            return "01"; // Varsayılan
            
        // "MERKEZ" gibi kategori adlarını koda çevir
        if (kategoriKod.Equals("MERKEZ", StringComparison.OrdinalIgnoreCase))
            return "01";
            
        // Zaten numerik kod gelmiş olabilir, değiştirme
        return kategoriKod;
    }
}
