using System;
using System.Threading.Tasks;
using FluentAssertions;
using Katana.API.Notifications;
using Katana.API.Hubs;
using Katana.Core.Events;
using Katana.Data.Context;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Katana.Tests.Notifications;

public class SignalRNotificationPublisherTests
{
    [Fact]
    public async Task PublishPendingCreatedAsync_ShouldSendToAllClients()
    {
        
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockClients.Setup(c => c.All).Returns(mockClientProxy.Object);

        var mockContext = new Mock<IHubContext<NotificationHub>>();
        mockContext.Setup(h => h.Clients).Returns(mockClients.Object);

        var options = new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new IntegrationDbContext(options);
        var logger = new Mock<ILogger<SignalRNotificationPublisher>>().Object;

        var publisher = new SignalRNotificationPublisher(mockContext.Object, logger, db);

        var evt = new PendingStockAdjustmentCreatedEvent(
            id: 42,
            externalOrderId: "ORD-001",
            sku: "TEST-SKU",
            quantity: 10,
            requestedBy: "TestUser",
            requestedAt: DateTimeOffset.UtcNow
        );

        
        await publisher.PublishPendingCreatedAsync(evt);

        
        mockClientProxy.Verify(
            c => c.SendCoreAsync(
                "PendingStockAdjustmentCreated",
                It.Is<object[]>(args => args.Length == 1),
                default),
            Times.Once);

        var notifications = await db.Notifications.ToListAsync();
        notifications.Should().ContainSingle(n => n.Type == "PendingStockAdjustmentCreated");
    }

    [Fact]
    public async Task PublishPendingApprovedAsync_ShouldSendToAllClients()
    {
        
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockClients.Setup(c => c.All).Returns(mockClientProxy.Object);

        var mockContext = new Mock<IHubContext<NotificationHub>>();
        mockContext.Setup(h => h.Clients).Returns(mockClients.Object);

        var options = new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new IntegrationDbContext(options);
        var logger = new Mock<ILogger<SignalRNotificationPublisher>>().Object;

        var publisher = new SignalRNotificationPublisher(mockContext.Object, logger, db);

        var evt = new PendingStockAdjustmentApprovedEvent(
            id: 99,
            externalOrderId: "ORD-002",
            sku: "APPROVED-SKU",
            quantity: 5,
            approvedBy: "Admin",
            approvedAt: DateTimeOffset.UtcNow
        );

        
        await publisher.PublishPendingApprovedAsync(evt);

        
        mockClientProxy.Verify(
            c => c.SendCoreAsync(
                "PendingStockAdjustmentApproved",
                It.Is<object[]>(args => args.Length == 1),
                default),
            Times.Once);

        var notifications = await db.Notifications.ToListAsync();
        notifications.Should().ContainSingle(n => n.Type == "PendingStockAdjustmentApproved");
    }

    [Fact]
    public async Task PublishPendingCreatedAsync_ShouldHandleExceptions()
    {
        
        var mockClients = new Mock<IHubClients>();
        mockClients.Setup(c => c.All).Throws(new Exception("SignalR failure"));

        var mockContext = new Mock<IHubContext<NotificationHub>>();
        mockContext.Setup(h => h.Clients).Returns(mockClients.Object);

        var options = new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new IntegrationDbContext(options);
        var logger = new Mock<ILogger<SignalRNotificationPublisher>>().Object;

        var publisher = new SignalRNotificationPublisher(mockContext.Object, logger, db);

        var evt = new PendingStockAdjustmentCreatedEvent(
            id: 1,
            externalOrderId: "ORD-ERR",
            sku: "ERROR-SKU",
            quantity: 1,
            requestedBy: "Test",
            requestedAt: DateTimeOffset.UtcNow
        );

        
        await publisher.Invoking(p => p.PublishPendingCreatedAsync(evt))
            .Should().NotThrowAsync();
    }
}
