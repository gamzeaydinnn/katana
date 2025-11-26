using Katana.Core.DTOs;

namespace Katana.Core.Interfaces;




public interface IStockService
{
    
    
    
    Task<List<StockDto>> GetAllStockMovementsAsync();
    
    
    
    
    Task<List<StockDto>> GetStockMovementsByProductIdAsync(int productId);
    
    
    
    
    Task<List<StockDto>> GetStockMovementsByLocationAsync(string location);
    
    
    
    
    Task<List<StockDto>> GetStockMovementsByDateRangeAsync(DateTime startDate, DateTime endDate);
    
    
    
    
    Task<StockDto> CreateStockMovementAsync(CreateStockMovementDto dto);
    
    
    
    
    Task<bool> DeleteStockMovementAsync(int id);
    
    
    
    
    Task<List<StockSummaryDto>> GetStockSummaryAsync();
    
    
    
    
    Task<StockSummaryDto?> GetStockSummaryByProductIdAsync(int productId);
    
    
    
    
    Task<List<StockDto>> GetUnsyncedStockMovementsAsync();
    
    
    
    
    Task<bool> MarkAsSyncedAsync(int id);
}
