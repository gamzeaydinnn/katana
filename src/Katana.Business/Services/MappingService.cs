using Katana.Business.Interfaces;
using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Katana.Business.Services;

public class MappingService : IMappingService
{
    private readonly IntegrationDbContext _context;
    private readonly ILogger<MappingService> _logger;

    public MappingService(IntegrationDbContext context, ILogger<MappingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Dictionary<string, string>> GetSkuToAccountMappingAsync()
    {
        try
        {
            var list = await _context.MappingTables
                .Where(m => m.MappingType == "SKU_ACCOUNT" && m.IsActive)
                .Select(m => new { m.SourceValue, m.TargetValue })
                .ToListAsync();

            var mappings = list.ToDictionary(m => m.SourceValue, m => m.TargetValue, StringComparer.OrdinalIgnoreCase);
            _logger.LogInformation("Retrieved {Count} SKU to account mappings", mappings.Count);
            return mappings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving SKU to account mappings");
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    public async Task<Dictionary<string, string>> GetLocationMappingAsync()
    {
        try
        {
            var list = await _context.MappingTables
                .Where(m => m.MappingType == "LOCATION_WAREHOUSE" && m.IsActive)
                .Select(m => new { m.SourceValue, m.TargetValue })
                .ToListAsync();

            var mappings = list.ToDictionary(m => m.SourceValue, m => m.TargetValue, StringComparer.OrdinalIgnoreCase);
            _logger.LogInformation("Retrieved {Count} location to warehouse mappings", mappings.Count);
            return mappings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving location to warehouse mappings");
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    public async Task UpdateSkuMappingAsync(string sku, string accountCode)
    {
        try
        {
            var normalizedSku = (sku ?? string.Empty).Trim().ToUpperInvariant();
            var existingMapping = await _context.MappingTables
                .FirstOrDefaultAsync(m => m.MappingType == "SKU_ACCOUNT" && m.SourceValue == normalizedSku);

            if (existingMapping != null)
            {
                existingMapping.TargetValue = accountCode;
                existingMapping.UpdatedAt = DateTime.UtcNow;
                _logger.LogInformation("Updated SKU mapping: {SKU} -> {AccountCode}", sku, accountCode);
            }
            else
            {
                var newMapping = new Data.Models.MappingTable
                {
                    MappingType = "SKU_ACCOUNT",
                    SourceValue = normalizedSku,
                    TargetValue = accountCode,
                    Description = $"Auto-generated mapping for SKU {sku}",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.MappingTables.Add(newMapping);
                _logger.LogInformation("Created new SKU mapping: {SKU} -> {AccountCode}", sku, accountCode);
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating SKU mapping for {SKU}", sku);
            throw;
        }
    }

    public async Task UpdateLocationMappingAsync(string location, string warehouseCode)
    {
        try
        {
            var normalizedLocation = (location ?? string.Empty).Trim().ToUpperInvariant();
            var existingMapping = await _context.MappingTables
                .FirstOrDefaultAsync(m => m.MappingType == "LOCATION_WAREHOUSE" && m.SourceValue == normalizedLocation);

            if (existingMapping != null)
            {
                existingMapping.TargetValue = warehouseCode;
                existingMapping.UpdatedAt = DateTime.UtcNow;
                _logger.LogInformation("Updated location mapping: {Location} -> {WarehouseCode}", location, warehouseCode);
            }
            else
            {
                var newMapping = new Data.Models.MappingTable
                {
                    MappingType = "LOCATION_WAREHOUSE",
                    SourceValue = normalizedLocation,
                    TargetValue = warehouseCode,
                    Description = $"Auto-generated mapping for location {location}",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.MappingTables.Add(newMapping);
                _logger.LogInformation("Created new location mapping: {Location} -> {WarehouseCode}", location, warehouseCode);
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating location mapping for {Location}", location);
            throw;
        }
    }
}
