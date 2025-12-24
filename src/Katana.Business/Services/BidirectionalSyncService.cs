using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Katana.Core.Entities;
using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Katana.Business.Services;

/// <summary>
/// Iki yonlu senkronizasyon:
/// 1. Luca'da guncellenen urun -> Katana'da AYNI URUN guncellenir
/// 2. Katana'da guncellenen urun -> Luca'da AYNI URUN guncellenir
/// YENI SKU/VERSIYON ACILMAZ!
/// </summary>
public class BidirectionalSyncService
{
    private readonly IntegrationDbContext _context;
    private readonly ILucaService _lucaService;
    private readonly IKatanaService _katanaService;
    private readonly IMappingService _mappingService;
    private readonly ILogger<BidirectionalSyncService> _logger;

    public BidirectionalSyncService(
        IntegrationDbContext context,
        ILucaService lucaService,
        IKatanaService katanaService,
        IMappingService mappingService,
        ILogger<BidirectionalSyncService> logger)
    {
        _context = context;
        _lucaService = lucaService;
        _katanaService = katanaService;
        _mappingService = mappingService;
        _logger = logger;
    }

    /// <summary>
    /// LUCA -> KATANA: Luca'da guncellenen urunler Katana'da AYNI URUN'u gunceller
    /// </summary>
    public async Task<SyncResult> SyncFromLucaToKatanaAsync(
        DateTime? sinceDate = null,
        int maxCount = 100)
    {
        var result = new SyncResult { Direction = "Luca -> Katana" };
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("[LUCA -> KATANA] Senkronizasyon basliyor...");

            var lucaProducts = await _lucaService.FetchProductsAsync(sinceDate);
            if (maxCount > 0)
            {
                lucaProducts = lucaProducts.Take(maxCount).ToList();
            }

            _logger.LogInformation("[LUCA -> KATANA] {Count} urun kontrol ediliyor", lucaProducts.Count);

            foreach (var lucaProduct in lucaProducts)
            {
                var sku = lucaProduct.ProductCode?.Trim();
                if (string.IsNullOrWhiteSpace(sku))
                {
                    result.SkippedCount++;
                    continue;
                }

                try
                {
                    var localProduct = await _context.Products
                        .FirstOrDefaultAsync(p => p.LucaId == lucaProduct.SkartId);
                    var lucaIdUpdated = false;

                    if (localProduct == null)
                    {
                        localProduct = await _context.Products
                            .FirstOrDefaultAsync(p => p.SKU == sku);

                        if (localProduct == null)
                        {
                            _logger.LogWarning("[ATLA] Urun local'de bulunamadi: {Sku}", sku);
                            result.SkippedCount++;
                            continue;
                        }

                        localProduct.LucaId = lucaProduct.SkartId;
                        lucaIdUpdated = true;
                    }
                    else if (localProduct.LucaId != lucaProduct.SkartId)
                    {
                        localProduct.LucaId = lucaProduct.SkartId;
                        lucaIdUpdated = true;
                    }

                    if (!localProduct.KatanaProductId.HasValue)
                    {
                        var katanaBySku = await _katanaService.GetProductBySkuAsync(sku);
                        if (katanaBySku != null && int.TryParse(katanaBySku.Id, out var katanaId))
                        {
                            localProduct.KatanaProductId = katanaId;
                        }
                    }

                    if (!localProduct.KatanaProductId.HasValue)
                    {
                        _logger.LogWarning("[ATLA] KatanaProductId yok: {Sku}", localProduct.SKU);
                        result.SkippedCount++;
                        continue;
                    }

                    var katanaProduct = await _katanaService.GetProductByIdAsync(localProduct.KatanaProductId.Value);
                    if (katanaProduct == null)
                    {
                        _logger.LogWarning("[ATLA] Katana'da urun bulunamadi: {Sku}", localProduct.SKU);
                        result.SkippedCount++;
                        continue;
                    }

                    var lucaDetails = await _lucaService.GetStockCardDetailsBySkuAsync(sku);
                    var lucaName = lucaDetails?.KartAdi ?? lucaProduct.ProductName ?? sku;
                    var lucaSalesPrice = lucaDetails?.SatisFiyat.HasValue == true
                        ? (decimal?)Convert.ToDecimal(lucaDetails.SatisFiyat.Value)
                        : null;

                    var changes = new List<string>();
                    string? updatedName = null;
                    decimal? updatedSalesPrice = null;

                    var normalizedLucaName = NormalizeName(lucaName);
                    var normalizedKatanaName = NormalizeName(katanaProduct.Name);
                    if (!string.Equals(normalizedLucaName, normalizedKatanaName, StringComparison.Ordinal))
                    {
                        updatedName = lucaName;
                        changes.Add("Isim guncellendi");
                    }

                    if (lucaSalesPrice.HasValue)
                    {
                        var katanaPrice = katanaProduct.SalesPrice ?? katanaProduct.Price;
                        if (Math.Abs(katanaPrice - lucaSalesPrice.Value) > 0.01m)
                        {
                            updatedSalesPrice = lucaSalesPrice.Value;
                            changes.Add($"Fiyat: {katanaPrice} -> {lucaSalesPrice.Value}");
                        }
                    }

                    if (changes.Count == 0)
                    {
                        _logger.LogDebug("[ATLA] Degisiklik yok: {Sku}", localProduct.SKU);
                        if (lucaIdUpdated)
                        {
                            localProduct.UpdatedAt = DateTime.UtcNow;
                            await _context.SaveChangesAsync();
                        }
                        result.SkippedCount++;
                        continue;
                    }

                    var updated = await _katanaService.UpdateProductAsync(
                        localProduct.KatanaProductId.Value,
                        updatedName ?? string.Empty,
                        updatedSalesPrice,
                        null);

                    if (!updated)
                    {
                        _logger.LogWarning("[HATA] Katana guncellemesi basarisiz: {Sku}", localProduct.SKU);
                        result.FailCount++;
                        result.Errors.Add(new SyncError
                        {
                            ProductSku = localProduct.SKU,
                            ErrorMessage = "Katana update failed"
                        });
                        continue;
                    }

                    if (updatedName != null)
                    {
                        localProduct.Name = lucaName;
                    }

                    if (updatedSalesPrice.HasValue)
                    {
                        localProduct.Price = updatedSalesPrice.Value;
                    }

                    localProduct.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    result.SuccessCount++;
                    result.UpdatedProducts.Add(new UpdatedProductInfo
                    {
                        SKU = localProduct.SKU,
                        Changes = changes
                    });

                    _logger.LogInformation("[BASARILI] {Sku}: {Changes}", localProduct.SKU, string.Join(", ", changes));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[HATA] {Sku}", sku);
                    result.FailCount++;
                    result.Errors.Add(new SyncError
                    {
                        ProductSku = sku,
                        ErrorMessage = ex.Message
                    });
                }
            }

            result.DurationMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[LUCA -> KATANA TAMAM] Basarili={Success}, Atla={Skipped}, Hata={Fail}",
                result.SuccessCount, result.SkippedCount, result.FailCount);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LUCA -> KATANA HATA]");
            throw;
        }
    }

    /// <summary>
    /// KATANA -> LUCA: Katana'da guncellenen urunler Luca'da AYNI URUN'u gunceller
    /// </summary>
    public async Task<SyncResult> SyncFromKatanaToLucaAsync(
        DateTime? sinceDate = null,
        int maxCount = 100)
    {
        var result = new SyncResult { Direction = "Katana -> Luca" };
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("[KATANA -> LUCA] Senkronizasyon basliyor...");

            var katanaProducts = await _katanaService.GetProductsAsync();
            if (maxCount > 0)
            {
                katanaProducts = katanaProducts.Take(maxCount).ToList();
            }

            _logger.LogInformation("[KATANA -> LUCA] {Count} urun kontrol ediliyor", katanaProducts.Count);

            var categoryMappings = await _mappingService.GetCategoryMappingAsync();
            var unitMappings = await _mappingService.GetUnitMappingAsync();

            foreach (var katanaProduct in katanaProducts)
            {
                try
                {
                    var katanaId = TryParseInt(katanaProduct.Id);
                    Product? localProduct = null;

                    if (katanaId.HasValue)
                    {
                        localProduct = await _context.Products
                            .FirstOrDefaultAsync(p => p.KatanaProductId == katanaId.Value);
                    }

                    if (localProduct == null)
                    {
                        localProduct = await _context.Products
                            .FirstOrDefaultAsync(p => p.SKU == katanaProduct.SKU);
                    }

                    if (localProduct == null)
                    {
                        _logger.LogWarning("[ATLA] Urun local'de bulunamadi: {Sku}", katanaProduct.SKU);
                        result.SkippedCount++;
                        continue;
                    }

                    if (sinceDate.HasValue && localProduct.UpdatedAt < sinceDate.Value)
                    {
                        result.SkippedCount++;
                        continue;
                    }

                    var lucaDetails = await _lucaService.GetStockCardDetailsBySkuAsync(katanaProduct.SKU);
                    if (lucaDetails == null)
                    {
                        _logger.LogWarning("[ATLA] Luca'da urun bulunamadi: {Sku}", katanaProduct.SKU);
                        result.SkippedCount++;
                        continue;
                    }

                    var changes = new List<string>();
                    var updateRequest = BuildLucaUpdateRequest(
                        katanaProduct,
                        lucaDetails,
                        categoryMappings,
                        unitMappings,
                        changes);

                    if (changes.Count == 0)
                    {
                        _logger.LogDebug("[ATLA] Degisiklik yok: {Sku}", localProduct.SKU);
                        result.SkippedCount++;
                        continue;
                    }

                    var updated = await _lucaService.UpdateStockCardAsync(updateRequest);
                    if (!updated)
                    {
                        _logger.LogWarning("[HATA] Luca guncellemesi basarisiz: {Sku}", localProduct.SKU);
                        result.FailCount++;
                        result.Errors.Add(new SyncError
                        {
                            ProductSku = localProduct.SKU,
                            ErrorMessage = "Luca update failed"
                        });
                        continue;
                    }

                    localProduct.LucaId = lucaDetails.SkartId;
                    localProduct.Name = katanaProduct.Name;
                    localProduct.Price = katanaProduct.SalesPrice ?? katanaProduct.Price;
                    localProduct.Barcode = katanaProduct.Barcode;
                    localProduct.KategoriAgacKod = updateRequest.KategoriAgacKod ?? localProduct.KategoriAgacKod;
                    localProduct.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();

                    result.SuccessCount++;
                    result.UpdatedProducts.Add(new UpdatedProductInfo
                    {
                        SKU = localProduct.SKU,
                        Changes = changes
                    });

                    _logger.LogInformation("[BASARILI] {Sku}: {Changes}", localProduct.SKU, string.Join(", ", changes));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[HATA] {Sku}", katanaProduct.SKU);
                    result.FailCount++;
                    result.Errors.Add(new SyncError
                    {
                        ProductSku = katanaProduct.SKU,
                        ErrorMessage = ex.Message
                    });
                }
            }

            result.DurationMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[KATANA -> LUCA TAMAM] Basarili={Success}, Atla={Skipped}, Hata={Fail}",
                result.SuccessCount, result.SkippedCount, result.FailCount);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[KATANA -> LUCA HATA]");
            throw;
        }
    }

    private static int? TryParseInt(string? value)
    {
        return int.TryParse(value, out var parsed) ? parsed : null;
    }

    /// <summary>
    /// Luca update request olustur.
    /// ONEMLI: SADECE DEGISEN ALANLAR gonderilir, NULL degerler GONDERILMEZ.
    /// </summary>
    private LucaUpdateStokKartiRequest BuildLucaUpdateRequest(
        KatanaProductDto katanaProduct,
        LucaStockCardDetails lucaDetails,
        Dictionary<string, string> categoryMappings,
        Dictionary<string, string> unitMappings,
        List<string> changes)
    {
        var request = new LucaUpdateStokKartiRequest
        {
            SkartId = lucaDetails.SkartId,
            KartKodu = lucaDetails.KartKodu ?? katanaProduct.SKU,
            KartAdi = lucaDetails.KartAdi ?? katanaProduct.Name
        };

        var normalizedKatanaName = NormalizeName(katanaProduct.Name);
        var normalizedLucaName = NormalizeName(lucaDetails.KartAdi ?? string.Empty);
        if (!string.Equals(normalizedKatanaName, normalizedLucaName, StringComparison.Ordinal))
        {
            request.KartAdi = normalizedKatanaName;
            changes.Add("Isim guncellendi");
        }

        var katanaSalesPrice = katanaProduct.SalesPrice ?? katanaProduct.Price;
        if (lucaDetails.SatisFiyat.HasValue &&
            Math.Abs(Convert.ToDecimal(lucaDetails.SatisFiyat.Value) - katanaSalesPrice) > 0.01m)
        {
            request.PerakendeSatisBirimFiyat = (double)katanaSalesPrice;
            changes.Add($"Fiyat: {lucaDetails.SatisFiyat} -> {katanaSalesPrice}");
        }

        if (!string.IsNullOrWhiteSpace(katanaProduct.Category) &&
            categoryMappings.TryGetValue(katanaProduct.Category, out var categoryCode) &&
            !string.Equals(categoryCode, lucaDetails.KategoriAgacKod, StringComparison.Ordinal))
        {
            request.KategoriAgacKod = categoryCode;
            changes.Add("Kategori guncellendi");
        }

        // NOT: Luca GuncelleStkWsSkart.do endpoint'i OlcumBirimiId güncellemesini desteklemiyor
        // Bu alan sadece stok kartı oluşturulurken ayarlanabilir

        if (!IsVersionedSku(katanaProduct.SKU) &&
            !string.IsNullOrWhiteSpace(katanaProduct.Barcode) &&
            !string.Equals(katanaProduct.Barcode, lucaDetails.Barkod, StringComparison.Ordinal))
        {
            request.Barkod = katanaProduct.Barcode;
            changes.Add("Barkod guncellendi");
        }

        return request;
    }

    private static string NormalizeName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        return name
            .Replace("Ø", "O").Replace("ø", "o")
            .Replace("Ğ", "G").Replace("ğ", "g")
            .Replace("Ü", "U").Replace("ü", "u")
            .Replace("Ş", "S").Replace("ş", "s")
            .Replace("İ", "I").Replace("ı", "i")
            .Replace("Ö", "O").Replace("ö", "o")
            .Replace("Ç", "C").Replace("ç", "c")
            .Trim();
    }

    private static bool IsVersionedSku(string? sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
            return false;

        return sku.Contains("-V", StringComparison.OrdinalIgnoreCase) ||
               sku.Contains("_V", StringComparison.OrdinalIgnoreCase);
    }
}

#region DTOs

public class SyncResult
{
    public string Direction { get; set; } = string.Empty;
    public int SuccessCount { get; set; }
    public int FailCount { get; set; }
    public int SkippedCount { get; set; }
    public double DurationMs { get; set; }
    public List<UpdatedProductInfo> UpdatedProducts { get; set; } = new();
    public List<SyncError> Errors { get; set; } = new();
}

public class UpdatedProductInfo
{
    public string SKU { get; set; } = string.Empty;
    public List<string> Changes { get; set; } = new();
}

public class SyncError
{
    public string ProductSku { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}

#endregion
