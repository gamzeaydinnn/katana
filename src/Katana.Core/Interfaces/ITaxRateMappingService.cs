using System.Collections.Generic;
using System.Threading.Tasks;
using Katana.Core.Entities;

namespace Katana.Core.Interfaces;

public interface ITaxRateMappingService
{
    /// <summary>
    /// Gets Koza KDV oranı by Katana tax_rate_id
    /// Returns default rate if mapping not found
    /// </summary>
    Task<decimal> GetKdvOranByTaxRateIdAsync(long katanaTaxRateId, decimal defaultRate = 0.20m);
    
    /// <summary>
    /// Gets tax rate mapping by Katana tax_rate_id
    /// </summary>
    Task<TaxRateMapping?> GetMappingByTaxRateIdAsync(long katanaTaxRateId);
    
    /// <summary>
    /// Creates or updates tax rate mapping
    /// </summary>
    Task<TaxRateMapping> CreateOrUpdateMappingAsync(long katanaTaxRateId, decimal kozaKdvOran, string? description = null);
    
    /// <summary>
    /// Gets all active tax rate mappings
    /// </summary>
    Task<List<TaxRateMapping>> GetAllMappingsAsync();
    
    /// <summary>
    /// Gets dictionary of Katana tax_rate_id → Koza KDV oranı for bulk operations
    /// </summary>
    Task<Dictionary<long, decimal>> GetTaxRateToKdvOranMapAsync();
    
    /// <summary>
    /// Deletes tax rate mapping
    /// </summary>
    Task<bool> DeleteMappingAsync(long katanaTaxRateId);
}
