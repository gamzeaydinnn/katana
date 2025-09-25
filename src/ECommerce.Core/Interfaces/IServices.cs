using ECommerce.Core.DTOs;

namespace ECommerce.Core.Interfaces;

public interface IKatanaService
{
    Task<List<KatanaStockDto>> GetStockChangesAsync(DateTime fromDate, DateTime toDate);
    Task<List<KatanaProductDto>> GetProductsAsync();
    Task<List<KatanaInvoiceDto>> GetInvoicesAsync(DateTime fromDate, DateTime toDate);
    Task<KatanaProductDto?> GetProductBySkuAsync(string sku);
    Task<bool> TestConnectionAsync();
}

public interface ILucaService
{
    Task<SyncResultDto> SendInvoicesAsync(List<LucaInvoiceDto> invoices);
    Task<SyncResultDto> SendStockMovementsAsync(List<LucaStockDto> stockMovements);
    Task<SyncResultDto> SendCustomersAsync(List<LucaCustomerDto> customers);
    Task<bool> TestConnectionAsync();
}

public interface ISyncService
{
    Task<SyncResultDto> SyncStockAsync(DateTime? fromDate = null);
    Task<SyncResultDto> SyncInvoicesAsync(DateTime? fromDate = null);
    Task<SyncResultDto> SyncCustomersAsync(DateTime? fromDate = null);
    Task<BatchSyncResultDto> SyncAllAsync(DateTime? fromDate = null);
    Task<List<SyncStatusDto>> GetSyncStatusAsync();
    Task<bool> IsSyncRunningAsync(string syncType);
}

public interface IMappingService
{
    Task<Dictionary<string, string>> GetSkuToAccountMappingAsync();
    Task<Dictionary<string, string>> GetLocationMappingAsync();
    Task UpdateSkuMappingAsync(string sku, string accountCode);
    Task UpdateLocationMappingAsync(string location, string warehouseCode);
}

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<List<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
}

public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}