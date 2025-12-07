using System.Collections.Generic;
using System.Threading.Tasks;
using Katana.Core.Entities;

namespace Katana.Core.Interfaces;

public interface IUoMMappingService
{
    /// <summary>
    /// Gets Koza olcumBirimiId by Katana UoM string
    /// Returns default ID if mapping not found
    /// </summary>
    Task<long> GetOlcumBirimiIdByUoMStringAsync(string? katanaUoMString, long defaultId = 5);
    
    /// <summary>
    /// Gets UoM mapping by Katana UoM string (case-insensitive)
    /// </summary>
    Task<UoMMapping?> GetMappingByUoMStringAsync(string katanaUoMString);
    
    /// <summary>
    /// Creates or updates UoM mapping
    /// </summary>
    Task<UoMMapping> CreateOrUpdateMappingAsync(string katanaUoMString, long kozaOlcumBirimiId, string? description = null);
    
    /// <summary>
    /// Gets all active UoM mappings
    /// </summary>
    Task<List<UoMMapping>> GetAllMappingsAsync();
    
    /// <summary>
    /// Gets dictionary of Katana UoM string → Koza olcumBirimiId for bulk operations
    /// </summary>
    Task<Dictionary<string, long>> GetUoMToOlcumBirimiIdMapAsync();
    
    /// <summary>
    /// Deletes UoM mapping
    /// </summary>
    Task<bool> DeleteMappingAsync(string katanaUoMString);
}
