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

namespace Katana.Tests.Services;

public class EventPublisherTests : IDisposable
{
    private readonly IntegrationDbContext _context;
    private readonly Mock<ILogger<EventPublisher>> _mockLogger;
    private readonly EventPublisher _eventPublisher;

    public EventPublisherTests()
    {
        var options = new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new IntegrationDbContext(options);
        _mockLogger = new Mock<ILogger<EventPublisher>>();
        _eventPublisher = new EventPublisher(_mockLogger.Object, _context);
    }

    [Fact]
    public async Task PublishAsync_InvoiceSyncedEvent_Should_CreateNotification()
    {
        // Arrange
        var invoice = new Invoice
        {
            InvoiceNo = "INV-001",
            CustomerId = 1,
            Amount = 500
        };
        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        var evt = new InvoiceSyncedEvent(invoice, "TestSync");

        // Act
        await _eventPublisher.PublishAsync(evt);

        // Assert
        var notifications = await _context.Notifications
            .Where(n => n.Type == "InvoiceSynced")
            .ToListAsync();

        notifications.Should().HaveCount(1);
        notifications[0].Title.Should().Contain("Fatura Senkronize Edildi");
        notifications[0].Link.Should().Contain($"/invoices/{invoice.Id}");
    }

    [Fact]
    public async Task PublishAsync_PurchaseOrderCancelled_Should_CancelPendingAdjustments()
    {
        // Arrange
        var po = await CreateTestPurchaseOrder();
        var pending = await CreateTestPendingStockAdjustment(po.Id);

        var evt = new PurchaseOrderStatusChangedEvent
        {
            PurchaseOrderId = po.Id,
            OldStatus = PurchaseOrderStatus.Approved,
            NewStatus = PurchaseOrderStatus.Cancelled,
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
    public async Task PublishAsync_PurchaseOrderStatusChanged_Should_CreateNotification()
    {
        // Arrange
        var evt = new PurchaseOrderStatusChangedEvent
        {
            PurchaseOrderId = 1,
            OldStatus = PurchaseOrderStatus.Pending,
            NewStatus = PurchaseOrderStatus.Approved,
            ChangedBy = "TestUser",
            ChangedAt = DateTime.UtcNow
        };

        // Act
        await _eventPublisher.PublishAsync(evt);

        // Assert
        var notifications = await _context.Notifications
            .Where(n => n.Type == "PurchaseOrderStatusChanged")
            .ToListAsync();

        notifications.Should().HaveCount(1);
        notifications[0].Title.Should().Contain("Satınalma Siparişi #1");
        notifications[0].Title.Should().Contain("Onaylandı");
    }

    [Fact]
    public async Task PublishAsync_Should_LogInformation()
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
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Publishing event")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task PublishAsync_Should_NotThrow_OnError()
    {
        // Arrange - Invalid event that might cause error
        var evt = new OrderStatusChangedEvent
        {
            OrderId = 999999, // Non-existent order
            OldStatus = OrderStatus.Pending,
            NewStatus = OrderStatus.Cancelled,
            ChangedBy = "TestUser",
            ChangedAt = DateTime.UtcNow
        };

        // Act
        var act = async () => await _eventPublisher.PublishAsync(evt);

        // Assert - Should not throw
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PurchaseOrder_Cancel_Should_Cascade_To_PendingStockAdjustments()
    {
        // Arrange
        var po = await CreateTestPurchaseOrder();
        var pending1 = await CreateTestPendingStockAdjustment(po.Id);
        var pending2 = await CreateTestPendingStockAdjustment(po.Id);

        var evt = new PurchaseOrderStatusChangedEvent
        {
            PurchaseOrderId = po.Id,
            OldStatus = PurchaseOrderStatus.Approved,
            NewStatus = PurchaseOrderStatus.Cancelled,
            ChangedBy = "TestUser",
            ChangedAt = DateTime.UtcNow
        };

        // Act
        await _eventPublisher.PublishAsync(evt);

        // Assert
        var allPending = await _context.PendingStockAdjustments
            .Where(p => p.ExternalOrderId == po.Id.ToString())
            .ToListAsync();

        allPending.Should().HaveCount(2);
        allPending.Should().OnlyContain(p => p.Status == "Cancelled");
    }

    // Helper methods
    private async Task<PurchaseOrder> CreateTestPurchaseOrder()
    {
        var po = new PurchaseOrder
        {
            SupplierId = 1,
            Status = PurchaseOrderStatus.Approved,
            TotalAmount = 1000,
            OrderDate = DateTime.UtcNow
        };
        _context.PurchaseOrders.Add(po);
        await _context.SaveChangesAsync();
        return po;
    }

    private async Task<PendingStockAdjustment> CreateTestPendingStockAdjustment(int poId)
    {
        var pending = new PendingStockAdjustment
        {
            ExternalOrderId = poId.ToString(),
            ProductId = 1,
            Sku = "TEST-001",
            Quantity = 10,
            Status = "Pending",
            RequestedBy = "system",
            RequestedAt = DateTimeOffset.UtcNow
        };
        _context.PendingStockAdjustments.Add(pending);
        await _context.SaveChangesAsync();
        return pending;
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
