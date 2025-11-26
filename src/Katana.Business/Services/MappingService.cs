using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Katana.Data.Context;
using Katana.Data.Models;
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

    
    
    
    public async Task<MappingDto> CreateSkuMappingAsync(CreateMappingRequestDto dto) =>
        await CreateMappingInternalAsync(new CreateMappingRequestDto
        {
            MappingType = "Product",
            SourceValue = dto.SourceValue,
            TargetValue = dto.TargetValue,
            Description = dto.Description,
            IsActive = dto.IsActive
        });

    public async Task<MappingDto> CreateLocationMappingAsync(CreateMappingRequestDto dto) =>
        await CreateMappingInternalAsync(new CreateMappingRequestDto
        {
            MappingType = "Location",
            SourceValue = dto.SourceValue,
            TargetValue = dto.TargetValue,
            Description = dto.Description,
            IsActive = dto.IsActive
        });

    public async Task<MappingDto> UpdateMappingAsync(int id, UpdateMappingRequestDto dto)
    {
        var mapping = await _context.MappingTables.FirstOrDefaultAsync(m => m.Id == id);
        if (mapping == null) throw new InvalidOperationException("Mapping not found");

        mapping.TargetValue = dto.TargetValue ?? mapping.TargetValue;
        mapping.Description = dto.Description ?? mapping.Description;
        if (dto.IsActive.HasValue) mapping.IsActive = dto.IsActive.Value;
        mapping.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return ToDto(mapping);
    }

    public async Task<bool> DeleteMappingAsync(int id)
    {
        var mapping = await _context.MappingTables.FirstOrDefaultAsync(m => m.Id == id);
        if (mapping == null) return false;
        _context.MappingTables.Remove(mapping);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<MappingDto?> GetMappingByIdAsync(int id)
    {
        var mapping = await _context.MappingTables.FirstOrDefaultAsync(m => m.Id == id);
        return mapping == null ? null : ToDto(mapping);
    }

    public async Task<List<MappingDto>> GetAllMappingsAsync(string? mappingType = null)
    {
        var query = _context.MappingTables.AsQueryable();
        if (!string.IsNullOrWhiteSpace(mappingType))
        {
            var normalized = mappingType.Trim().ToUpperInvariant();
            query = query.Where(m => m.MappingType == normalized);
        }

        var list = await query.ToListAsync();
        return list.Select(ToDto).ToList();
    }

    public async Task<MappingDto?> GetMappingBySourceAsync(string sourceValue, string mappingType)
    {
        var normalizedSource = (sourceValue ?? string.Empty).Trim().ToUpperInvariant();
        var normalizedType = (mappingType ?? string.Empty).Trim().ToUpperInvariant();

        var mapping = await _context.MappingTables
            .FirstOrDefaultAsync(m => m.MappingType == normalizedType && m.SourceValue == normalizedSource);

        return mapping == null ? null : ToDto(mapping);
    }

    
    
    
    public async Task<MappingStatsDto> GetMappingStatsAsync()
    {
        try
        {
            var total = await _context.MappingTables.CountAsync();
            var active = await _context.MappingTables.CountAsync(m => m.IsActive);
            return new MappingStatsDto
            {
                TotalMappings = total,
                ActiveMappings = active,
                InactiveMappings = total - active,
                ProductMappings = await _context.MappingTables.CountAsync(m => m.MappingType == "Product"),
                CustomerMappings = await _context.MappingTables.CountAsync(m => m.MappingType == "Customer"),
                LocationMappings = await _context.MappingTables.CountAsync(m => m.MappingType == "Location"),
                LastUpdated = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get mapping stats");
            return new MappingStatsDto { LastUpdated = DateTime.UtcNow };
        }
    }

    
    
    
    public Task<SyncResultDto> SyncProductsToLucaAsync(DateTime? fromDate = null) =>
        Task.FromResult(new SyncResultDto { SyncType = "PRODUCT_TO_LUCA", IsSuccess = false, Message = "Not implemented" });

    public Task<SyncResultDto> SyncSalesOrdersToLucaAsync(DateTime fromDate, DateTime toDate) =>
        Task.FromResult(new SyncResultDto { SyncType = "SALES_ORDER_TO_LUCA", IsSuccess = false, Message = "Not implemented" });

    public Task<SyncResultDto> SyncCustomersToLucaAsync(DateTime fromDate, DateTime toDate) =>
        Task.FromResult(new SyncResultDto { SyncType = "CUSTOMER_TO_LUCA", IsSuccess = false, Message = "Not implemented" });

    public Task<SyncResultDto> SyncInvoicesToLucaAsync(DateTime fromDate, DateTime toDate) =>
        Task.FromResult(new SyncResultDto { SyncType = "INVOICE_TO_LUCA", IsSuccess = false, Message = "Not implemented" });

    
    
    
    public Task<SyncResultDto> SyncProductsFromLucaAsync(DateTime? fromDate = null) =>
        Task.FromResult(new SyncResultDto { SyncType = "PRODUCT_FROM_LUCA", IsSuccess = false, Message = "Not implemented" });

    public Task<SyncResultDto> SyncStockFromLucaAsync(DateTime? fromDate = null) =>
        Task.FromResult(new SyncResultDto { SyncType = "STOCK_FROM_LUCA", IsSuccess = false, Message = "Not implemented" });

    public Task<SyncResultDto> SyncInvoicesFromLucaAsync(DateTime? fromDate = null) =>
        Task.FromResult(new SyncResultDto { SyncType = "INVOICE_FROM_LUCA", IsSuccess = false, Message = "Not implemented" });

    
    
    
    public Task<MappingValidationDto> ValidateMappingsAsync() =>
        Task.FromResult(new MappingValidationDto
        {
            IsValid = false,
            Errors = { "Validation not implemented" },
            TotalMappings = 0,
            ActiveMappings = 0,
            ValidationDate = DateTime.UtcNow
        });

    public Task<List<MappingErrorDto>> GetUnmappedRecordsAsync() =>
        Task.FromResult(new List<MappingErrorDto>
        {
            new MappingErrorDto
            {
                RecordType = "System",
                RecordId = "UNIMPLEMENTED",
                ErrorMessage = "Unmapped records lookup not implemented",
                Timestamp = DateTime.UtcNow
            }
        });

    
    
    
    private async Task<MappingDto> CreateMappingInternalAsync(CreateMappingRequestDto dto)
    {
        var normalizedType = (dto.MappingType ?? string.Empty).Trim().ToUpperInvariant();
        var normalizedSource = (dto.SourceValue ?? string.Empty).Trim().ToUpperInvariant();

        var existing = await _context.MappingTables
            .FirstOrDefaultAsync(m => m.MappingType == normalizedType && m.SourceValue == normalizedSource);
        if (existing != null) throw new InvalidOperationException("Mapping already exists");

        var mapping = new MappingTable
        {
            MappingType = normalizedType,
            SourceValue = normalizedSource,
            TargetValue = dto.TargetValue ?? string.Empty,
            Description = dto.Description,
            IsActive = dto.IsActive ?? true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.MappingTables.Add(mapping);
        await _context.SaveChangesAsync();
        return ToDto(mapping);
    }

    private static MappingDto ToDto(MappingTable m) => new MappingDto
    {
        MappingType = m.MappingType,
        SourceValue = m.SourceValue,
        TargetValue = m.TargetValue,
        Description = m.Description,
        IsActive = m.IsActive,
        CreatedAt = m.CreatedAt,
        UpdatedAt = m.UpdatedAt
    };
}
