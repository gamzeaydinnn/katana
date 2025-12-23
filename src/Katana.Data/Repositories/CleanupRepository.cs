using Katana.Core.DTOs;
using Katana.Core.Entities;
using Katana.Core.Interfaces;
using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Katana.Data.Repositories;

public class CleanupRepository : ICleanupRepository
{
    private readonly IntegrationDbContext _context;
    private readonly ILogger<CleanupRepository> _logger;

    public CleanupRepository(IntegrationDbContext context, ILogger<CleanupRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<SalesOrder>> GetApprovedOrdersAsync()
    {
        return await _context.SalesOrders
            .Include(o => o.Lines)
            .Where(o => o.Status == "Approved")
            .ToListAsync();
    }

    public async Task<List<OrderProductInfo>> GetOrderProductsAsync()
    {
        var query = from order in _context.SalesOrders
                    join line in _context.SalesOrderLines on order.Id equals line.SalesOrderId
                    where order.Status == "Approved"
                    select new OrderProductInfo
                    {
                        OrderId = order.Id,
                        OrderNo = order.OrderNo ?? string.Empty,
                        SKU = line.SKU ?? string.Empty,
                        ProductName = line.ProductName ?? string.Empty,
                        KatanaOrderId = line.KatanaOrderId,
                        ApprovedDate = order.ApprovedDate ?? DateTime.MinValue
                    };

        return await query.ToListAsync();
    }

    public async Task<List<SalesOrderLine>> GetOrderLinesAsync(int orderId)
    {
        return await _context.SalesOrderLines
            .Where(l => l.SalesOrderId == orderId)
            .ToListAsync();
    }

    public async Task ResetOrderAsync(int orderId)
    {
        var order = await _context.SalesOrders.FindAsync(orderId);
        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found for reset", orderId);
            return;
        }

        order.Status = "Pending";
        order.ApprovedDate = null;
        order.ApprovedBy = null;
        order.SyncStatus = null;

        await _context.SaveChangesAsync();
    }

    public async Task ClearOrderMappingsAsync(int orderId)
    {
        var mappings = await _context.OrderMappings
            .Where(m => m.OrderId == orderId)
            .ToListAsync();

        _context.OrderMappings.RemoveRange(mappings);
        await _context.SaveChangesAsync();
    }

    public async Task LogCleanupOperationAsync(CleanupOperation operation)
    {
        // This will be implemented when we create the CleanupOperation entity
        // For now, just log to console
        _logger.LogInformation(
            "Cleanup operation: {Type}, Status: {Status}, Started: {Start}",
            operation.OperationType,
            operation.Status,
            operation.StartTime);

        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
