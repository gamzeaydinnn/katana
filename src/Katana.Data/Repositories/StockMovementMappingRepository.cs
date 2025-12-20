using Katana.Core.Interfaces;
using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Katana.Data.Repositories;

/// <summary>
/// Katana stok hareketi ID'lerini Luca kodlarına eşleştiren repository
/// </summary>
public class StockMovementMappingRepository : IStockMovementMappingRepository
{
    private readonly IntegrationDbContext _context;
    private readonly ILogger<StockMovementMappingRepository> _logger;
    private const string DEFAULT_DEPO_KODU = "001"; // Fallback depo kodu

    public StockMovementMappingRepository(
        IntegrationDbContext context,
        ILogger<StockMovementMappingRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<string?> GetLucaDepoKoduByLocationIdAsync(int locationId)
    {
        try
        {
            // LocationKozaDepotMapping tablosundan depo kodunu al
            var mapping = await _context.LocationKozaDepotMappings
                .Where(m => m.KatanaLocationId == locationId.ToString())
                .FirstOrDefaultAsync();

            if (mapping != null)
            {
                if (string.IsNullOrWhiteSpace(mapping.KozaDepoKodu))
                {
                    _logger.LogWarning(
                        "Location {LocationId} mapping found but KozaDepoKodu is empty. Using default: {DefaultDepo}",
                        locationId, DEFAULT_DEPO_KODU);
                    return DEFAULT_DEPO_KODU;
                }

                return mapping.KozaDepoKodu;
            }

            // Mapping bulunamadı - MappingTable'dan fallback kontrol et
            var fallbackMapping = await _context.MappingTables
                .Where(m => m.MappingType == "LOCATION_WAREHOUSE" 
                    && m.SourceValue == locationId.ToString()
                    && m.IsActive)
                .FirstOrDefaultAsync();

            if (fallbackMapping != null && !string.IsNullOrWhiteSpace(fallbackMapping.TargetValue))
            {
                _logger.LogInformation(
                    "Using fallback mapping for Location {LocationId}: {DepoKodu}",
                    locationId, fallbackMapping.TargetValue);
                return fallbackMapping.TargetValue;
            }

            // Hiçbir mapping bulunamadı - default depo kodu kullan
            _logger.LogWarning(
                "No mapping found for Location {LocationId}. Using default depo: {DefaultDepo}",
                locationId, DEFAULT_DEPO_KODU);

            return DEFAULT_DEPO_KODU;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error getting depo kodu for Location {LocationId}. Using default: {DefaultDepo}",
                locationId, DEFAULT_DEPO_KODU);
            return DEFAULT_DEPO_KODU;
        }
    }

    public async Task<string?> GetLucaStokKoduByVariantIdAsync(int variantId)
    {
        // Variant tablosu yoksa Product SKU kullan
        // Gerçek implementasyonda ProductVariant tablosu gerekir
        return await Task.FromResult<string?>(null);
    }

    public async Task<string?> GetLucaStokKoduByProductIdAsync(int productId)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == productId);
        
        return product?.SKU;
    }

    public async Task SaveLucaTransferIdAsync(int katanaTransferId, long lucaTransferId)
    {
        // Transfer mapping tablosuna kaydet
        // Şimdilik basit bir çözüm
        await Task.CompletedTask;
    }

    public async Task SaveLucaAdjustmentIdAsync(int katanaAdjustmentId, long lucaDshId)
    {
        // Adjustment mapping tablosuna kaydet
        await Task.CompletedTask;
    }

    public async Task<Dictionary<int, string>> GetAllLocationMappingsAsync()
    {
        try
        {
            // LocationKozaDepotMapping'den tüm mapping'leri al
            var mappings = await _context.LocationKozaDepotMappings
                .Where(m => !string.IsNullOrWhiteSpace(m.KozaDepoKodu))
                .ToDictionaryAsync(
                    m => int.TryParse(m.KatanaLocationId, out var id) ? id : 0,
                    m => m.KozaDepoKodu);

            return mappings.Where(kvp => kvp.Key > 0).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all location mappings");
            return new Dictionary<int, string>();
        }
    }
}
