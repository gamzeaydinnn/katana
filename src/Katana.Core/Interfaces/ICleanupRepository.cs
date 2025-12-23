using Katana.Core.DTOs;
using Katana.Core.Entities;

namespace Katana.Core.Interfaces;

public interface ICleanupRepository
{
    Task<List<SalesOrder>> GetApprovedOrdersAsync();
    Task<List<OrderProductInfo>> GetOrderProductsAsync();
    Task<List<SalesOrderLine>> GetOrderLinesAsync(int orderId);
    Task ResetOrderAsync(int orderId);
    Task ClearOrderMappingsAsync(int orderId);
    Task LogCleanupOperationAsync(CleanupOperation operation);
    Task SaveChangesAsync();
}
