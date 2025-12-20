using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Katana.Business.Services;
using Katana.Core.Entities;
using Katana.Data.Context;
using Katana.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Katana.Tests.Services;

public class PendingStockAdjustmentServiceTests : IDisposable
{
    private readonly IntegrationDbContext _context;
    private readonly PendingStockAdjustmentService _service;
    private readonly long _pendingId;

    public PendingStockAdjustmentServiceTests()
    {
        var options = new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new IntegrationDbContext(options);
        _context.Database.EnsureCreated();
        var loggerMock = new Mock<ILogger<PendingStockAdjustmentService>>();
        _service = new PendingStockAdjustmentService(_context, loggerMock.Object);

        _context.Categories.Add(new Category
        {
            Id = 1,
            Name = "Default",
            Description = "Test category",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        _context.Products.Add(new Product
        {
            Id = 1,
            SKU = "SKU-1",
            Name = "Test Product",
            Price = 0m,
            Stock = 10,
            CategoryId = 1,
            IsActive = true
        });

        _context.PendingStockAdjustments.Add(new PendingStockAdjustment
        {
            Id = 1,
            ProductId = 1,
            Sku = "SKU-1",
            Quantity = -4, 
            RequestedAt = DateTimeOffset.UtcNow,
            Status = "Pending",
            ExternalOrderId = "ORDER-1",
            Notes = "Order based adjustment"
        });

        _context.SaveChanges();
        _pendingId = _context.PendingStockAdjustments.Single().Id;
    }

    [Fact]
    public async Task ApproveAsync_ShouldApplyStockChangeAndPersistMovement()
    {
        
        var result = await _service.ApproveAsync(_pendingId, "admin");

        
        result.Should().BeTrue();

        var product = await _context.Products.SingleAsync();
        product.Stock.Should().Be(6);

        var adjustment = await _context.PendingStockAdjustments.SingleAsync(p => p.Id == _pendingId);
        adjustment.Status.Should().Be("Approved");
        adjustment.ApprovedBy.Should().Be("admin");
        adjustment.ApprovedAt.Should().NotBeNull();

        var stockRecord = await _context.Stocks.SingleAsync();
        stockRecord.ProductId.Should().Be(1);
        stockRecord.Quantity.Should().Be(4);
        stockRecord.Type.Should().Be("OUT");
        stockRecord.Reference.Should().Be("ORDER-1");
        stockRecord.IsSynced.Should().BeFalse();
    }

    public void Dispose()
    {
        _context.Dispose();
        
}
}
