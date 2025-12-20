using Katana.Core.DTOs;

namespace Katana.Business.Interfaces;

/// <summary>
/// Service for syncing measurement units from Luca and creating unit mappings
/// </summary>
public interface IOlcumBirimiSyncService
{
    /// <summary>
    /// Fetches measurement units from Luca and creates UNIT type mappings in the database
    /// </summary>
    /// <returns>Number of newly added mappings</returns>
    Task<int> SyncOlcumBirimiMappingsAsync();

    /// <summary>
    /// Gets all measurement units from Luca API
    /// </summary>
    /// <returns>List of Luca measurement units</returns>
    Task<List<LucaOlcumBirimiDto>> GetLucaOlcumBirimleriAsync();
}
