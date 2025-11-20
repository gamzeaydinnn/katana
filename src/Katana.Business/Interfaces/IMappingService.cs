using System;
using System.Collections.Generic;
using Katana.Core.DTOs;

namespace Katana.Business.Interfaces;

/// <summary>
/// Katana ↔ Luca/Koza mapping ve senkronizasyon orkestrasyonu.
/// </summary>
public interface IMappingService
{
    // Mapping CRUD
    Task<MappingDto> CreateSkuMappingAsync(CreateMappingRequestDto dto);
    Task<MappingDto> CreateLocationMappingAsync(CreateMappingRequestDto dto);
    Task<MappingDto> UpdateMappingAsync(int id, UpdateMappingRequestDto dto);
    Task<bool> DeleteMappingAsync(int id);
    Task<MappingDto?> GetMappingByIdAsync(int id);
    Task<List<MappingDto>> GetAllMappingsAsync(string? mappingType = null);
    Task<MappingDto?> GetMappingBySourceAsync(string sourceValue, string mappingType);
    Task<MappingStatsDto> GetMappingStatsAsync();

    // Temel mapping yönetimi
    Task<Dictionary<string, string>> GetSkuToAccountMappingAsync();
    Task<Dictionary<string, string>> GetLocationMappingAsync();
    Task UpdateSkuMappingAsync(string sku, string accountCode);
    Task UpdateLocationMappingAsync(string location, string warehouseCode);

    // Katana → Luca senkronizasyon
    Task<SyncResultDto> SyncProductsToLucaAsync(DateTime? fromDate = null);
    Task<SyncResultDto> SyncSalesOrdersToLucaAsync(DateTime fromDate, DateTime toDate);
    Task<SyncResultDto> SyncCustomersToLucaAsync(DateTime fromDate, DateTime toDate);
    Task<SyncResultDto> SyncInvoicesToLucaAsync(DateTime fromDate, DateTime toDate);

    // Luca → Katana senkronizasyon
    Task<SyncResultDto> SyncProductsFromLucaAsync(DateTime? fromDate = null);
    Task<SyncResultDto> SyncStockFromLucaAsync(DateTime? fromDate = null);
    Task<SyncResultDto> SyncInvoicesFromLucaAsync(DateTime? fromDate = null);

    // Mapping doğrulama ve raporlama
    Task<MappingValidationDto> ValidateMappingsAsync();
    Task<List<MappingErrorDto>> GetUnmappedRecordsAsync();
}

