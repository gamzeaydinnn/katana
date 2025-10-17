using Katana.Core.DTOs;

namespace Katana.Core.Interfaces;

/// <summary>
/// Stok yönetimi için servis arayüzü
/// </summary>
public interface IStockService
{
    /// <summary>
    /// Tüm stok hareketlerini getirir
    /// </summary>
    Task<List<StockDto>> GetAllStockMovementsAsync();
    
    /// <summary>
    /// Belirli bir ürünün stok hareketlerini getirir
    /// </summary>
    Task<List<StockDto>> GetStockMovementsByProductIdAsync(int productId);
    
    /// <summary>
    /// Belirli bir lokasyondaki stok hareketlerini getirir
    /// </summary>
    Task<List<StockDto>> GetStockMovementsByLocationAsync(string location);
    
    /// <summary>
    /// Tarih aralığına göre stok hareketlerini getirir
    /// </summary>
    Task<List<StockDto>> GetStockMovementsByDateRangeAsync(DateTime startDate, DateTime endDate);
    
    /// <summary>
    /// Yeni stok hareketi oluşturur
    /// </summary>
    Task<StockDto> CreateStockMovementAsync(CreateStockMovementDto dto);
    
    /// <summary>
    /// Stok hareketi siler
    /// </summary>
    Task<bool> DeleteStockMovementAsync(int id);
    
    /// <summary>
    /// Ürün bazlı stok özetini getirir
    /// </summary>
    Task<List<StockSummaryDto>> GetStockSummaryAsync();
    
    /// <summary>
    /// Belirli bir ürünün stok özetini getirir
    /// </summary>
    Task<StockSummaryDto?> GetStockSummaryByProductIdAsync(int productId);
    
    /// <summary>
    /// Senkronize edilmemiş stok hareketlerini getirir
    /// </summary>
    Task<List<StockDto>> GetUnsyncedStockMovementsAsync();
    
    /// <summary>
    /// Stok hareketini senkronize edildi olarak işaretler
    /// </summary>
    Task<bool> MarkAsSyncedAsync(int id);
}
