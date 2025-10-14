namespace ECommerce.Core.Interfaces;
public interface IMappingService
{
    Task<Dictionary<string, string>> GetSkuToAccountMappingAsync();
    Task<Dictionary<string, string>> GetLocationMappingAsync();
    Task UpdateSkuMappingAsync(string sku, string accountCode);
    Task UpdateLocationMappingAsync(string location, string warehouseCode);
}
