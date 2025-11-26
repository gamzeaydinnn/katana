using System;
using System.Collections.Generic;
using Katana.Core.DTOs;

namespace Katana.Business.Interfaces;
public interface IMappingService
{
    Task<MappingDto> CreateSkuMappingAsync(CreateMappingRequestDto dto);
    Task<MappingDto> CreateLocationMappingAsync(CreateMappingRequestDto dto);
    Task<MappingDto> UpdateMappingAsync(int id, UpdateMappingRequestDto dto);
    Task<bool> DeleteMappingAsync(int id);
    Task<MappingDto?> GetMappingByIdAsync(int id);
    Task<List<MappingDto>> GetAllMappingsAsync(string? mappingType = null);
    Task<MappingDto?> GetMappingBySourceAsync(string sourceValue, string mappingType);
    Task<MappingStatsDto> GetMappingStatsAsync();
    Task<Dictionary<string, string>> GetSkuToAccountMappingAsync();
    Task<Dictionary<string, string>> GetLocationMappingAsync();
    Task UpdateSkuMappingAsync(string sku, string accountCode);
    Task UpdateLocationMappingAsync(string location, string warehouseCode);

    
    Task<SyncResultDto> SyncProductsToLucaAsync(DateTime? fromDate = null);
    Task<SyncResultDto> SyncSalesOrdersToLucaAsync(DateTime fromDate, DateTime toDate);
    Task<SyncResultDto> SyncCustomersToLucaAsync(DateTime fromDate, DateTime toDate);
    Task<SyncResultDto> SyncInvoicesToLucaAsync(DateTime fromDate, DateTime toDate);

    
    Task<SyncResultDto> SyncProductsFromLucaAsync(DateTime? fromDate = null);
    Task<SyncResultDto> SyncStockFromLucaAsync(DateTime? fromDate = null);
    Task<SyncResultDto> SyncInvoicesFromLucaAsync(DateTime? fromDate = null);

    
    Task<MappingValidationDto> ValidateMappingsAsync();
    Task<List<MappingErrorDto>> GetUnmappedRecordsAsync();
}

