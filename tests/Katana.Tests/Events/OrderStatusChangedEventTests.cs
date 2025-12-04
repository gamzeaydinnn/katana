using Xunit;
using FluentAssertions;
using Katana.Business.Services;
using Katana.Core.Entities;
using Katana.Core.Enums;
using Katana.Core.Events;
using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Katana.Tests.Events;

public class OrderStatusChangedEventTests : IDisposable
{
    private readonly IntegrationDbContext _context;
    private readonly Mock<ILogger<EventPublisher>> _mockLogger;
    private readonly EventPublisher _eventPublisher;

    public OrderStatusChangedEventTests()
    {
        var options = new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new IntegrationDbContext(options);
        _mockLogger = new Mock<ILogger<EventPublisher>>();
        _eventPublisher = new EventPublisher(_mockLogger.Object, _context);
    }

    [Fact]
    public async Task OrderStatusChanged_Should_CreateNotification()
    {
        // Arrange
        var evt = new OrderStatusChangedEvent
        {
            OrderId = 1,
            OldStatus = OrderStatus.Pending,
            NewStatus = OrderStatus.Processing,
            ChangedBy = "TestUser",
            ChangedAt = DateTime.UtcNow
        };

        // Act
        await _eventPublisher.PublishAsync(evt);

        // Assert
        var notifications = await _context.Notifications
            .Where(n => n.Type == "OrderStatusChanged")
            .ToListAsync();

        notifications.Should().HaveCount(1);
        notifications[0].Title.Should().Contain("Sipariş #1");
    }

    [Fact]
    public async Task OrderCancelled_Should_CancelInvoice()
    {
        // Arrange
        var order = new Order
        {
            Id = 10,
            CustomerId = 1,
            Status = OrderStatus.Processing,
            TotalAmount = 500
        };
        _context.Orders.Add(order);

        var invoice = new Invoice
        {
            InvoiceNo = "INV-001",
            CustomerId = 1, // Same as order.CustomerId
            Amount = 500,
            Status = "ACTIVE"
        };
        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        var evt = new OrderStatusChangedEvent
        {
            OrderId = order.Id,
            OldStatus = OrderStatus.Processing,
            NewStatus = OrderStatus.Cancelled,
            ChangedBy = "TestUser",
            ChangedAt = DateTime.UtcNow
        };

        // Act
        await _eventPublisher.PublishAsync(evt);

        // Assert
        var updatedInvoice = await _context.Invoices.FindAsync(invoice.Id);
        updatedInvoice.Should().NotBeNull();
        updatedInvoice!.Status.Should().Be("CANCELLED");
    }

    [Fact]
    public async Task OrderCancelled_Should_CancelPendingStockAdjustments()
    {
        // Arrange
        var order = new Order
        {
            Id = 20,
            CustomerId = 1,
            Status = OrderStatus.Processing,
            TotalAmount = 500
        };
        _context.Orders.Add(order);

        var pending = new Katana.Data.Models.PendingStockAdjustment
        {
            ExternalOrderId = "20",
            ProductId = 1,
            Sku = "TEST-001",
            Quantity = -5,
            Status = "Pending",
            RequestedBy = "system",
            RequestedAt = DateTimeOffset.UtcNow
        };
        _context.PendingStockAdjustments.Add(pending);
        await _context.SaveChangesAsync();

        var evt = new OrderStatusChangedEvent
        {
            OrderId = order.Id,
            OldStatus = OrderStatus.Processing,
            NewStatus = OrderStatus.Cancelled,
            ChangedBy = "TestUser",
            ChangedAt = DateTime.UtcNow
        };

        // Act
        await _eventPublisher.PublishAsync(evt);

        // Assert
        var updatedPending = await _context.PendingStockAdjustments.FindAsync(pending.Id);
        updatedPending.Should().NotBeNull();
        updatedPending!.Status.Should().Be("Cancelled");
    }

    [Fact]
    public async Task OrderReturned_Should_CreateReverseStockMovement()
    {
        // Arrange
        var product = new Product { Id = 1, Name = "Test Product", SKU = "TEST-001" };
        _context.Products.Add(product);

        var order = new Order
        {
            Id = 30,
            CustomerId = 1,
            Status = OrderStatus.Delivered,
            TotalAmount = 500,
            Items = new List<OrderItem>
            {
                new() { ProductId = 1, Quantity = 5, UnitPrice = 100 }
            }
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var evt = new OrderStatusChangedEvent
        {
            OrderId = order.Id,
            OldStatus = OrderStatus.Delivered,
            NewStatus = OrderStatus.Returned,
            ChangedBy = "TestUser",
            ChangedAt = DateTime.UtcNow
        };

        // Act
        await _eventPublisher.PublishAsync(evt);

        // Assert
        var movements = await _context.StockMovements
            .Where(m => m.SourceDocument == $"RETURN-ORDER-{order.Id}")
            .ToListAsync();

        movements.Should().NotBeEmpty();
        movements.Should().Contain(m => m.MovementType == MovementType.In);
        movements.Should().Contain(m => m.ChangeQuantity > 0); // Pozitif miktar (iade)
        movements.Sum(m => m.ChangeQuantity).Should().Be(5);
    }

    [Fact]
    public async Task OrderCancelled_Should_RemovePayments()
    {
        // Arrange
        var invoice = new Invoice
        {
            InvoiceNo = "INV-002",
            CustomerId = 1,
            Amount = 500,
            Status = "ACTIVE"
        };
        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        var payment = new Payment
        {
            InvoiceId = invoice.Id,
            Amount = 500,
            PaymentDate = DateTime.UtcNow
        };
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        var evt = new OrderStatusChangedEvent
        {
            OrderId = 1, // CustomerId matches invoice
            OldStatus = OrderStatus.Processing,
            NewStatus = OrderStatus.Cancelled,
            ChangedBy = "TestUser",
            ChangedAt = DateTime.UtcNow
        };

        // Act
        await _eventPublisher.PublishAsync(evt);

        // Assert
        var remainingPayments = await _context.Payments
            .Where(p => p.InvoiceId == invoice.Id)
            .ToListAsync();

        remainingPayments.Should().BeEmpty();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
