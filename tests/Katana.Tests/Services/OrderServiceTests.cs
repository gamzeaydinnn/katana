using Xunit;
using FluentAssertions;
using Katana.Business.Services;
using Katana.Core.DTOs;
using Katana.Core.Entities;
using Katana.Core.Enums;
using Katana.Core.Events;
using Katana.Core.Interfaces;
using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Katana.Tests.Services;

public class OrderServiceTests : IDisposable
{
    private readonly IntegrationDbContext _context;
    private readonly Mock<Katana.Business.Interfaces.IPendingStockAdjustmentService> _mockPendingService;
    private readonly Mock<IEventPublisher> _mockEventPublisher;
    private readonly OrderService _orderService;

    public OrderServiceTests()
    {
        var options = new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new IntegrationDbContext(options);
        _mockPendingService = new Mock<Katana.Business.Interfaces.IPendingStockAdjustmentService>();
        _mockEventPublisher = new Mock<IEventPublisher>();
        
        _orderService = new OrderService(_context, _mockPendingService.Object, _mockEventPublisher.Object);
    }

    [Fact]
    public async Task CreateAsync_Should_CreateOrder_WithPendingStockAdjustments()
    {
        // Arrange
        var product = new Product { Id = 1, Name = "Test Product", SKU = "TEST-001" };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var createDto = new CreateOrderDto
        {
            CustomerId = 1,
            Items = new List<CreateOrderItemDto>
            {
                new() { ProductId = 1, Quantity = 5, UnitPrice = 100 }
            }
        };

        // Act
        var result = await _orderService.CreateAsync(createDto);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.TotalAmount.Should().Be(500);
        result.Status.Should().Be("Pending");

        var pendingAdjustments = await _context.PendingStockAdjustments
            .Where(p => p.ExternalOrderId == result.Id.ToString())
            .ToListAsync();

        pendingAdjustments.Should().HaveCount(1);
        pendingAdjustments[0].Quantity.Should().Be(-5);
        pendingAdjustments[0].Status.Should().Be("Pending");
    }

    [Fact]
    public async Task UpdateStatusAsync_Should_PublishEvent()
    {
        // Arrange
        var order = new Order
        {
            CustomerId = 1,
            Status = OrderStatus.Pending,
            TotalAmount = 100
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Act
        var result = await _orderService.UpdateStatusAsync(order.Id, OrderStatus.Processing);

        // Assert
        result.Should().BeTrue();
        
        _mockEventPublisher.Verify(
            x => x.PublishAsync(It.Is<OrderStatusChangedEvent>(
                e => e.OrderId == order.Id && 
                     e.OldStatus == OrderStatus.Pending && 
                     e.NewStatus == OrderStatus.Processing)),
            Times.Once);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
