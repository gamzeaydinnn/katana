using Katana.Core.DTOs;
using Katana.Core.Entities;
using Katana.Core.Enums;
using Katana.Core.Interfaces;
using Katana.Core.Helpers;
using Katana.Core.Events;
using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Katana.Business.Services;

public class OrderService : IOrderService
{
    private readonly IntegrationDbContext _context;
    private readonly Katana.Business.Interfaces.IPendingStockAdjustmentService _pendingService;
    private readonly IEventPublisher _eventPublisher;

    public OrderService(
        IntegrationDbContext context, 
        Katana.Business.Interfaces.IPendingStockAdjustmentService pendingService,
        IEventPublisher eventPublisher)
    {
        _context = context;
        _pendingService = pendingService;
        _eventPublisher = eventPublisher;
    }

    public async Task<IEnumerable<OrderDto>> GetAllAsync()
    {
        var orders = await _context.Orders.Include(o => o.Items).ToListAsync();
        return orders.Select(o => new OrderDto
        {
            Id = o.Id,
            CustomerId = o.CustomerId,
            Status = o.Status.ToString(),
            TotalAmount = o.TotalAmount,
            Items = o.Items.Select(i => new OrderItemDto
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        });
    }

    public async Task<OrderDto?> GetByIdAsync(int id)
    {
        var order = await _context.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
        if (order == null) return null;

        return new OrderDto
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            Status = order.Status.ToString(),
            TotalAmount = order.TotalAmount,
            Items = order.Items.Select(i => new OrderItemDto
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };
    }

    public async Task<OrderDto> CreateAsync(CreateOrderDto dto)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        Order? createdOrder = null;

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = new Order
                {
                    CustomerId = dto.CustomerId,
                    Status = OrderStatus.Pending,
                    Items = dto.Items.Select(i => new OrderItem
                    {
                        ProductId = i.ProductId,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice
                    }).ToList()
                };

                order.TotalAmount = order.Items.Sum(i => i.UnitPrice * i.Quantity);

                _context.Orders.Add(order);
                
                // Order + Items kaydet
                await _context.SaveChangesAsync();

                // PendingStockAdjustments ekle (SaveChanges yapmadan)
                foreach (var item in order.Items)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    var sku = product?.SKU ?? string.Empty;

                    var pending = new Katana.Data.Models.PendingStockAdjustment
                    {
                        ExternalOrderId = order.Id.ToString(),
                        ProductId = item.ProductId,
                        Sku = sku,
                        Quantity = -item.Quantity,
                        RequestedBy = "system",
                        RequestedAt = DateTimeOffset.UtcNow,
                        Status = "Pending",
                        Notes = $"Order #{order.Id} created: {item.Quantity} x ProductId {item.ProductId}"
                    };

                    _context.PendingStockAdjustments.Add(pending);
                }
                
                // Tüm pending adjustments'ı kaydet
                await _context.SaveChangesAsync();
                
                await transaction.CommitAsync();
                createdOrder = order;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });

        if (createdOrder == null)
            throw new InvalidOperationException("Order could not be created.");

        return new OrderDto
        {
            Id = createdOrder.Id,
            CustomerId = createdOrder.CustomerId,
            Status = createdOrder.Status.ToString(),
            TotalAmount = createdOrder.TotalAmount
        };
    }

    public async Task<bool> UpdateStatusAsync(int id, OrderStatus newStatus)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null) return false;

        var oldStatus = order.Status;

        // Status transition validation
        if (!StatusMapper.IsValidTransition(oldStatus, newStatus))
        {
            throw new InvalidOperationException(
                $"Invalid status transition from {oldStatus} to {newStatus}");
        }

        order.Status = newStatus;
        order.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Publish status change event
        await _eventPublisher.PublishAsync(new OrderStatusChangedEvent
        {
            OrderId = id,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            ChangedBy = "System",
            ChangedAt = DateTime.UtcNow
        });

        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null) return false;

        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();
        return true;
    }
}

