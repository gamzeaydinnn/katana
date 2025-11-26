using System;
using System.Threading.Tasks;
using FluentAssertions;
using Katana.API.Controllers;
using Katana.Business.Services;
using Katana.Core.Interfaces;
using Katana.Data.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Katana.Tests.Integration;

public class WebhookNotificationFlowTests
{
    [Fact]
    public async Task KatanaWebhook_ShouldCreatePendingAndPublishNotification()
    {
        
        var options = new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new IntegrationDbContext(options);

        var product = new Katana.Core.Entities.Product
        {
            Name = "Test Product",
            SKU = "WEBHOOK-SKU-001",
            Price = 99.99m,
            Stock = 10,
            CategoryId = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var publisherMock = new Mock<IPendingNotificationPublisher>(MockBehavior.Strict);
        publisherMock
            .Setup(p => p.PublishPendingCreatedAsync(It.IsAny<Katana.Core.Events.PendingStockAdjustmentCreatedEvent>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var pendingService = new PendingStockAdjustmentService(
            db,
            new Mock<ILogger<PendingStockAdjustmentService>>().Object,
            publisherMock.Object);

        var configDict = new System.Collections.Generic.Dictionary<string, string?>
        {
            ["KatanaApi:WebhookSecret"] = "test-secret-key"
        };
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();

        var controller = new KatanaWebhookController(
            pendingService,
            new Mock<ILogger<KatanaWebhookController>>().Object,
            config);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext()
        };
        controller.Request.Headers["X-Katana-Signature"] = "test-secret-key";

        var webhook = new KatanaStockChangeWebhook
        {
            Event = "stock.updated",
            Sku = "WEBHOOK-SKU-001",
            QuantityChange = 5,
            OrderId = "ORD-WEBHOOK-001",
            ProductId = product.Id,
            Timestamp = DateTime.UtcNow
        };

        
        var result = await controller.ReceiveStockChange(webhook);

        
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);

        var pending = await db.PendingStockAdjustments.FirstOrDefaultAsync(p => p.Sku == "WEBHOOK-SKU-001");
        pending.Should().NotBeNull();
        pending!.Quantity.Should().Be(5);
        pending.Status.Should().Be("Pending");

        publisherMock.Verify(
            p => p.PublishPendingCreatedAsync(It.Is<Katana.Core.Events.PendingStockAdjustmentCreatedEvent>(
                e => e.Sku == "WEBHOOK-SKU-001" && e.Quantity == 5)),
            Times.Once);
    }

    [Fact]
    public async Task KatanaWebhook_InvalidSignature_ShouldReturnUnauthorized()
    {
        
        var options = new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new IntegrationDbContext(options);

        var pendingService = new PendingStockAdjustmentService(
            db,
            new Mock<ILogger<PendingStockAdjustmentService>>().Object,
            new Mock<IPendingNotificationPublisher>().Object);

        var configDict = new System.Collections.Generic.Dictionary<string, string?>
        {
            ["KatanaApi:WebhookSecret"] = "correct-secret"
        };
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();

        var controller = new KatanaWebhookController(
            pendingService,
            new Mock<ILogger<KatanaWebhookController>>().Object,
            config);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext()
        };
        controller.Request.Headers["X-Katana-Signature"] = "wrong-secret";

        var webhook = new KatanaStockChangeWebhook 
        { 
            Event = "test",
            Sku = "ANY", 
            QuantityChange = 1, 
            OrderId = "ANY",
            ProductId = 1,
            Timestamp = DateTime.UtcNow
        };

        
        var result = await controller.ReceiveStockChange(webhook);

        
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task ApprovePending_ShouldPublishApprovedNotification()
    {
        
        var options = new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new IntegrationDbContext(options);

        
        var category = new Katana.Core.Entities.Category
        {
            Name = "Test Category",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        };
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        var product = new Katana.Core.Entities.Product
        {
            Name = "Approve Test",
            SKU = "APPROVE-SKU",
            Price = 50m,
            Stock = 100,
            CategoryId = category.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        };
        db.Products.Add(product);
        await db.SaveChangesAsync(); 

        var pending = new Katana.Data.Models.PendingStockAdjustment
        {
            ExternalOrderId = "ORD-APPROVE-001",
            ProductId = product.Id, 
            Sku = "APPROVE-SKU",
            Quantity = 10,
            Status = "Pending",
            RequestedBy = "TestUser",
            RequestedAt = DateTimeOffset.UtcNow
        };
        db.PendingStockAdjustments.Add(pending);
        await db.SaveChangesAsync();

        var publisherMock = new Mock<IPendingNotificationPublisher>(MockBehavior.Strict);
        publisherMock
            .Setup(p => p.PublishPendingApprovedAsync(It.IsAny<Katana.Core.Events.PendingStockAdjustmentApprovedEvent>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var pendingService = new PendingStockAdjustmentService(
            db,
            new Mock<ILogger<PendingStockAdjustmentService>>().Object,
            publisherMock.Object);

        
        await pendingService.ApproveAsync(pending.Id, "AdminUser");

        
        var updated = await db.PendingStockAdjustments.FindAsync(pending.Id);
        updated!.Status.Should().Be("Approved");
        updated.ApprovedBy.Should().Be("AdminUser");

        var updatedProduct = await db.Products.FindAsync(product.Id);
        updatedProduct!.Stock.Should().Be(110);

        publisherMock.Verify(
            p => p.PublishPendingApprovedAsync(It.Is<Katana.Core.Events.PendingStockAdjustmentApprovedEvent>(
                e => e.Id == pending.Id && e.Quantity == 10)),
            Times.Once);
    }
}
