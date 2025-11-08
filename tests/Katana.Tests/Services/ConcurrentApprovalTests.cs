using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Katana.Business.Services;
using Katana.Data.Context;
using Katana.Data.Models;
using Katana.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Katana.Tests.Services;

public class ConcurrentApprovalTests : IDisposable
{
    private readonly DbContextOptions<IntegrationDbContext> _options;

    public ConcurrentApprovalTests()
    {
        _options = new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var ctx = new IntegrationDbContext(_options);
        ctx.Database.EnsureCreated();

        // seed a product and one pending adjustment
        var category = new Category
        {
            Name = "Default",
            Description = "Test Category",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        ctx.Categories.Add(category);
        ctx.SaveChanges();
        // keep foreign key checks off during seeding to avoid incidental FK constraints in model

        var product = new Product
        {
            Name = "Test Product",
            SKU = "CONC-001",
            Price = 0m,
            Stock = 10,
            CategoryId = category.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        };
        ctx.Products.Add(product);
        ctx.SaveChanges();

        var pending = new PendingStockAdjustment
        {
            ExternalOrderId = $"ORD-{Guid.NewGuid():N}",
            ProductId = product.Id,
            Sku = product.SKU,
            Quantity = 5,
            RequestedBy = "tester",
            RequestedAt = DateTimeOffset.UtcNow,
            Status = "Pending"
        };
        ctx.PendingStockAdjustments.Add(pending);
        ctx.SaveChanges();
    }

    // NOTE: Concurrency approval tests require relational FK setup behaving consistently.
    // This suite uses InMemory provider and serves as a placeholder for future relational tests.

    public void Dispose() { }
}
