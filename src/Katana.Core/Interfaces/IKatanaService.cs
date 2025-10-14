using Katana.Core.DTOs;

namespace Katana.Core.Interfaces;

public interface IKatanaService
{
    Task<List<KatanaStockDto>> GetStockChangesAsync(DateTime fromDate, DateTime toDate);
    Task<List<KatanaProductDto>> GetProductsAsync();
    Task<List<KatanaInvoiceDto>> GetInvoicesAsync(DateTime fromDate, DateTime toDate);
    Task<KatanaProductDto?> GetProductBySkuAsync(string sku);
    Task<bool> TestConnectionAsync();
}

