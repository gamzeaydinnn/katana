using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Microsoft.Extensions.Logging;

namespace Katana.Business.Services;

/// <summary>
/// Service for syncing measurement units from Luca and creating unit mappings
/// </summary>
public class OlcumBirimiSyncService : IOlcumBirimiSyncService
{
    private readonly ILucaService _lucaService;
    private readonly IMappingService _mappingService;
    private readonly ILogger<OlcumBirimiSyncService> _logger;

    // Common Katana unit strings mapped to Luca unit names
    private static readonly Dictionary<string, string[]> KatanaToLucaUnitNames = new(StringComparer.OrdinalIgnoreCase)
    {
        { "pcs", new[] { "ADET", "AD", "PIECE" } },
        { "piece", new[] { "ADET", "AD", "PIECE" } },
        { "kg", new[] { "KG", "KILOGRAM" } },
        { "kilogram", new[] { "KG", "KILOGRAM" } },
        { "m", new[] { "METRE", "M", "MT" } },
        { "meter", new[] { "METRE", "M", "MT" } },
        { "metre", new[] { "METRE", "M", "MT" } },
        { "l", new[] { "LITRE", "LT", "L" } },
        { "liter", new[] { "LITRE", "LT", "L" } },
        { "litre", new[] { "LITRE", "LT", "L" } },
        { "lt", new[] { "LITRE", "LT", "L" } },
        { "g", new[] { "GRAM", "GR", "G" } },
        { "gram", new[] { "GRAM", "GR", "G" } },
        { "gr", new[] { "GRAM", "GR", "G" } },
        { "cm", new[] { "CM", "SANTIMETRE" } },
        { "mm", new[] { "MM", "MILIMETRE" } },
        { "m2", new[] { "M2", "METREKARE" } },
        { "m3", new[] { "M3", "METREKÜP" } },
        { "box", new[] { "KUTU", "KT" } },
        { "kutu", new[] { "KUTU", "KT" } },
        { "pack", new[] { "PAKET", "PKT" } },
        { "paket", new[] { "PAKET", "PKT" } },
        { "set", new[] { "SET", "TAKIM" } },
        { "pair", new[] { "ÇIFT", "ÇFT" } },
        { "roll", new[] { "RULO", "TOP" } },
        { "sheet", new[] { "TABAKA", "YAPRAK" } },
    };

    public OlcumBirimiSyncService(
        ILucaService lucaService,
        IMappingService mappingService,
        ILogger<OlcumBirimiSyncService> logger)
    {
        _lucaService = lucaService;
        _mappingService = mappingService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<LucaOlcumBirimiDto>> GetLucaOlcumBirimleriAsync()
    {
        try
        {
            _logger.LogInformation("Fetching measurement units from Luca...");
            var units = await _lucaService.GetOlcumBirimiListAsync();
            _logger.LogInformation("Retrieved {Count} measurement units from Luca", units.Count);
            return units;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch measurement units from Luca");
            return new List<LucaOlcumBirimiDto>();
        }
    }

    /// <inheritdoc />
    public async Task<int> SyncOlcumBirimiMappingsAsync()
    {
        _logger.LogInformation("Starting measurement unit mapping sync...");

        // Get existing mappings to avoid duplicates
        var existingMappings = await _mappingService.GetUnitMappingAsync();
        _logger.LogInformation("Found {Count} existing unit mappings", existingMappings.Count);

        // Fetch Luca measurement units
        var lucaUnits = await GetLucaOlcumBirimleriAsync();
        if (lucaUnits.Count == 0)
        {
            _logger.LogWarning("No measurement units retrieved from Luca, skipping sync");
            return 0;
        }

        // Build lookup dictionary for Luca units by name (case-insensitive)
        var lucaUnitsByName = lucaUnits
            .Where(u => u.Aktif && !string.IsNullOrWhiteSpace(u.Ad))
            .GroupBy(u => u.Ad.Trim().ToUpperInvariant())
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        // Also add by Kod and Kisa for better matching
        foreach (var unit in lucaUnits.Where(u => u.Aktif))
        {
            if (!string.IsNullOrWhiteSpace(unit.Kod) && !lucaUnitsByName.ContainsKey(unit.Kod.Trim().ToUpperInvariant()))
            {
                lucaUnitsByName[unit.Kod.Trim().ToUpperInvariant()] = unit;
            }
            if (!string.IsNullOrWhiteSpace(unit.Kisa) && !lucaUnitsByName.ContainsKey(unit.Kisa.Trim().ToUpperInvariant()))
            {
                lucaUnitsByName[unit.Kisa.Trim().ToUpperInvariant()] = unit;
            }
        }

        int addedCount = 0;

        // Create mappings for each Katana unit
        foreach (var (katanaUnit, lucaNames) in KatanaToLucaUnitNames)
        {
            // Skip if mapping already exists
            if (existingMappings.ContainsKey(katanaUnit))
            {
                _logger.LogDebug("Mapping already exists for Katana unit '{KatanaUnit}', skipping", katanaUnit);
                continue;
            }

            // Find matching Luca unit
            LucaOlcumBirimiDto? matchedUnit = null;
            foreach (var lucaName in lucaNames)
            {
                if (lucaUnitsByName.TryGetValue(lucaName, out var unit))
                {
                    matchedUnit = unit;
                    break;
                }
            }

            if (matchedUnit == null)
            {
                _logger.LogWarning("No Luca unit found for Katana unit '{KatanaUnit}' (searched: {SearchedNames})",
                    katanaUnit, string.Join(", ", lucaNames));
                continue;
            }

            // Create the mapping
            try
            {
                await _mappingService.UpdateUnitMappingAsync(katanaUnit, matchedUnit.Id.ToString());
                addedCount++;
                _logger.LogInformation("Created unit mapping: {KatanaUnit} -> {LucaUnitId} ({LucaUnitName})",
                    katanaUnit, matchedUnit.Id, matchedUnit.Ad);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create unit mapping for {KatanaUnit}", katanaUnit);
            }
        }

        _logger.LogInformation("Measurement unit mapping sync completed. Added {AddedCount} new mappings", addedCount);
        return addedCount;
    }
}
