using Katana.Core.Entities;
using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Katana.Business.Services;

/// <summary>
/// Location → Koza Depo mapping servisi
/// Depo kodu validation ve error handling
/// </summary>
public interface ILocationMappingService
{
    Task<string> GetDepoKoduByLocationIdAsync(string locationId);
    Task<string> GetDepoKoduByLocationIdAsync(int locationId);
    Task<long?> GetDepoIdByLocationIdAsync(string locationId);
    Task<LocationKozaDepotMapping?> GetMappingByLocationIdAsync(string locationId);
    Task<LocationKozaDepotMapping> CreateOrUpdateMappingAsync(
        string katanaLocationId,
        string kozaDepoKodu,
        long? kozaDepoId = null,
        string? katanaLocationName = null,
        string? kozaDepoTanim = null);
    Task<bool> ValidateDepoKoduAsync(string depoKodu);
    Task<List<LocationKozaDepotMapping>> GetAllMappingsAsync();
    Task<Dictionary<string, string>> GetLocationToDepoKoduMapAsync();
}

public class LocationMappingService : ILocationMappingService
{
    private readonly IntegrationDbContext _context;
    private readonly ILogger<LocationMappingService> _logger;
    private const string DEFAULT_DEPO_KODU = "001";

    public LocationMappingService(
        IntegrationDbContext context,
        ILogger<LocationMappingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Location ID'ye göre depo kodu al (string)
    /// </summary>
    public async Task<string> GetDepoKoduByLocationIdAsync(string locationId)
    {
        if (string.IsNullOrWhiteSpace(locationId))
        {
            _logger.LogWarning("Location ID is null or empty. Using default depo: {DefaultDepo}", DEFAULT_DEPO_KODU);
            return DEFAULT_DEPO_KODU;
        }

        try
        {
            var mapping = await _context.LocationKozaDepotMappings
                .Where(m => m.KatanaLocationId == locationId)
                .FirstOrDefaultAsync();

            if (mapping != null && !string.IsNullOrWhiteSpace(mapping.KozaDepoKodu))
            {
                return mapping.KozaDepoKodu;
            }

            // Fallback: MappingTable'dan kontrol et
            var fallback = await _context.MappingTables
                .Where(m => m.MappingType == "LOCATION_WAREHOUSE" 
                    && m.SourceValue == locationId
                    && m.IsActive)
                .FirstOrDefaultAsync();

            if (fallback != null && !string.IsNullOrWhiteSpace(fallback.TargetValue))
            {
                _logger.LogInformation("Using fallback mapping for Location {LocationId}: {DepoKodu}",
                    locationId, fallback.TargetValue);
                return fallback.TargetValue;
            }

            _logger.LogWarning("No mapping found for Location {LocationId}. Using default: {DefaultDepo}",
                locationId, DEFAULT_DEPO_KODU);
            return DEFAULT_DEPO_KODU;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting depo kodu for Location {LocationId}. Using default: {DefaultDepo}",
                locationId, DEFAULT_DEPO_KODU);
            return DEFAULT_DEPO_KODU;
        }
    }

    /// <summary>
    /// Location ID'ye göre depo kodu al (int)
    /// </summary>
    public async Task<string> GetDepoKoduByLocationIdAsync(int locationId)
    {
        return await GetDepoKoduByLocationIdAsync(locationId.ToString());
    }

    /// <summary>
    /// Location ID'ye göre Koza depo ID al
    /// </summary>
    public async Task<long?> GetDepoIdByLocationIdAsync(string locationId)
    {
        if (string.IsNullOrWhiteSpace(locationId))
        {
            return null;
        }

        try
        {
            var mapping = await _context.LocationKozaDepotMappings
                .Where(m => m.KatanaLocationId == locationId)
                .FirstOrDefaultAsync();

            return mapping?.KozaDepoId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting depo ID for Location {LocationId}", locationId);
            return null;
        }
    }

    /// <summary>
    /// Location ID'ye göre mapping al
    /// </summary>
    public async Task<LocationKozaDepotMapping?> GetMappingByLocationIdAsync(string locationId)
    {
        if (string.IsNullOrWhiteSpace(locationId))
        {
            return null;
        }

        return await _context.LocationKozaDepotMappings
            .FirstOrDefaultAsync(m => m.KatanaLocationId == locationId);
    }

    /// <summary>
    /// Mapping oluştur veya güncelle
    /// </summary>
    public async Task<LocationKozaDepotMapping> CreateOrUpdateMappingAsync(
        string katanaLocationId,
        string kozaDepoKodu,
        long? kozaDepoId = null,
        string? katanaLocationName = null,
        string? kozaDepoTanim = null)
    {
        if (string.IsNullOrWhiteSpace(katanaLocationId))
        {
            throw new ArgumentException("Katana Location ID cannot be null or empty", nameof(katanaLocationId));
        }

        if (string.IsNullOrWhiteSpace(kozaDepoKodu))
        {
            throw new ArgumentException("Koza Depo Kodu cannot be null or empty", nameof(kozaDepoKodu));
        }

        var existing = await _context.LocationKozaDepotMappings
            .FirstOrDefaultAsync(m => m.KatanaLocationId == katanaLocationId);

        if (existing != null)
        {
            // Güncelle
            existing.KozaDepoKodu = kozaDepoKodu;
            existing.KozaDepoId = kozaDepoId;
            existing.KatanaLocationName = katanaLocationName ?? existing.KatanaLocationName;
            existing.KozaDepoTanim = kozaDepoTanim ?? existing.KozaDepoTanim;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.SyncStatus = "PENDING";

            _context.LocationKozaDepotMappings.Update(existing);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated location mapping: {LocationId} → {DepoKodu}",
                katanaLocationId, kozaDepoKodu);

            return existing;
        }
        else
        {
            // Yeni oluştur
            var newMapping = new LocationKozaDepotMapping
            {
                KatanaLocationId = katanaLocationId,
                KozaDepoKodu = kozaDepoKodu,
                KozaDepoId = kozaDepoId,
                KatanaLocationName = katanaLocationName,
                KozaDepoTanim = kozaDepoTanim,
                SyncStatus = "PENDING",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.LocationKozaDepotMappings.Add(newMapping);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created location mapping: {LocationId} → {DepoKodu}",
                katanaLocationId, kozaDepoKodu);

            return newMapping;
        }
    }

    /// <summary>
    /// Depo kodunun geçerli olup olmadığını kontrol et
    /// </summary>
    public async Task<bool> ValidateDepoKoduAsync(string depoKodu)
    {
        if (string.IsNullOrWhiteSpace(depoKodu))
        {
            return false;
        }

        // KozaDepots tablosunda var mı kontrol et
        var exists = await _context.KozaDepots
            .AnyAsync(d => d.Kod == depoKodu);

        if (!exists)
        {
            _logger.LogWarning("Depo kodu validation failed: {DepoKodu} not found in KozaDepots", depoKodu);
        }

        return exists;
    }

    /// <summary>
    /// Tüm mapping'leri getir
    /// </summary>
    public async Task<List<LocationKozaDepotMapping>> GetAllMappingsAsync()
    {
        return await _context.LocationKozaDepotMappings
            .OrderBy(m => m.KatanaLocationId)
            .ToListAsync();
    }

    /// <summary>
    /// Location ID → Depo Kodu dictionary'si al
    /// </summary>
    public async Task<Dictionary<string, string>> GetLocationToDepoKoduMapAsync()
    {
        return await _context.LocationKozaDepotMappings
            .Where(m => !string.IsNullOrWhiteSpace(m.KozaDepoKodu))
            .ToDictionaryAsync(
                m => m.KatanaLocationId,
                m => m.KozaDepoKodu);
    }
}
