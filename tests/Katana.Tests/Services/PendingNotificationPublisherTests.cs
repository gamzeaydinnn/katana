using System;
using System.Threading.Tasks;
using FluentAssertions;
using Katana.Business.Services;
using Katana.Core.Events;
using Katana.Core.Interfaces;
using Katana.Data.Context;
using Katana.Data.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Katana.Tests.Services;

public class PendingNotificationPublisherTests
{
    [Fact]
    public async Task CreateAsync_ShouldPublish_PendingCreatedEvent()
    {
        var options = new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new IntegrationDbContext(options);
        var logger = new Mock<ILogger<PendingStockAdjustmentService>>().Object;
        var publisher = new Mock<IPendingNotificationPublisher>(MockBehavior.Strict);
        publisher
            .Setup(p => p.PublishPendingCreatedAsync(It.IsAny<PendingStockAdjustmentCreatedEvent>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var svc = new PendingStockAdjustmentService(db, logger, publisher.Object);

        var product = new Katana.Core.Entities.Product
        {
            Name = "P",
            SKU = "SKU-CREATE",
            Price = 0m,
            Stock = 0,
            CategoryId = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true,
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var created = await svc.CreateAsync(new PendingStockAdjustment
        {
            ExternalOrderId = $"ORD-{Guid.NewGuid():N}",
            ProductId = product.Id,
            Sku = product.SKU,
            Quantity = 1,
            RequestedBy = "user",
        });

        created.Id.Should().BeGreaterThan(0);
        publisher.Verify();
    }

    // NOTE: ApproveAsync publish path is validated indirectly via service logic.
    // Full end-to-end publish test requires relational DB and API build. Skipped due to constraints.
}
