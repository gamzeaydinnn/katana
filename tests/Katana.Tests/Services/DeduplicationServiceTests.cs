using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Katana.Business.Services.Deduplication;
using Katana.Core.DTOs;
using Katana.Core.Entities;
using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Katana.Tests.Services;

public class DeduplicationServiceTests : IDisposable
{
    private readonly IntegrationDbContext _context;
    private readonly Mock<ILogger<DeduplicationService>> _mockLogger;
    private readonly DeduplicationService _service;

    public DeduplicationServiceTests()
    {
        var options = new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new IntegrationDbContext(options);
        _mockLogger = new Mock<ILogger<DeduplicationService>>();
        _service = new DeduplicationService(_context, _mockLogger.Object);
    }

    [Fact]
    public async Task BuildSkuBaseMergePlanAsync_WithNoDuplicates_ReturnsEmptyGroups()
    {
        // Arrange - Her ürün farklı SKU base'e sahip
        await SeedProductsAsync(new[]
        {
            new Product { Id = 1, SKU = "PROD-001", Name = "Product 1", IsActive = true },
            new Product { Id = 2, SKU = "PROD-002", Name = "Product 2", IsActive = true },
            new Product { Id = 3, SKU = "PROD-003", Name = "Product 3", IsActive = true }
        });

        // Act
        var plan = await _service.BuildSkuBaseMergePlanAsync();

        // Assert
        plan.Should().NotBeNull();
        plan.Groups.Should().BeEmpty("çünkü hiçbir SKU base'de duplikasyon yok");
    }

    [Fact]
    public async Task BuildSkuBaseMergePlanAsync_WithDuplicates_GroupsBySkuBase()
    {
        // Arrange - Aynı base'e sahip ürünler
        await SeedProductsAsync(new[]
        {
            new Product { Id = 101, SKU = "9340003", Name = "Base Product", IsActive = true },
            new Product { Id = 102, SKU = "9340003-1", Name = "Variant 1", IsActive = true },
            new Product { Id = 108, SKU = "9340003-2", Name = "Variant 2", IsActive = true }
        });

        // Act
        var plan = await _service.BuildSkuBaseMergePlanAsync();

        // Assert
        plan.Should().NotBeNull();
        plan.Groups.Should().HaveCount(1, "çünkü tek bir SKU base grubu var");
        
        var group = plan.Groups.First();
        group.SkuBase.Should().Be("9340003");
        group.CanonicalProductId.Should().Be(101, "çünkü SKU'su base ile birebir eşleşiyor");
        group.DuplicateProductIds.Should().BeEquivalentTo(new[] { 102, 108 });
    }

    [Fact]
    public async Task BuildSkuBaseMergePlanAsync_WithMultipleGroups_ReturnsAllGroups()
    {
        // Arrange - İki farklı SKU base grubu
        await SeedProductsAsync(new[]
        {
            // Grup 1: 9340003
            new Product { Id = 101, SKU = "9340003", Name = "Base 1", IsActive = true },
            new Product { Id = 102, SKU = "9340003-1", Name = "Variant 1-1", IsActive = true },
            
            // Grup 2: 8520045
            new Product { Id = 201, SKU = "8520045", Name = "Base 2", IsActive = true },
            new Product { Id = 202, SKU = "8520045-A", Name = "Variant 2-A", IsActive = true },
            new Product { Id = 203, SKU = "8520045-B", Name = "Variant 2-B", IsActive = true }
        });

        // Act
        var plan = await _service.BuildSkuBaseMergePlanAsync();

        // Assert
        plan.Should().NotBeNull();
        plan.Groups.Should().HaveCount(2, "çünkü iki farklı SKU base grubu var");
        
        var group1 = plan.Groups.FirstOrDefault(g => g.SkuBase == "9340003");
        group1.Should().NotBeNull();
        group1!.CanonicalProductId.Should().Be(101);
        group1.DuplicateProductIds.Should().BeEquivalentTo(new[] { 102 });
        
        var group2 = plan.Groups.FirstOrDefault(g => g.SkuBase == "8520045");
        group2.Should().NotBeNull();
        group2!.CanonicalProductId.Should().Be(201);
        group2.DuplicateProductIds.Should().BeEquivalentTo(new[] { 202, 203 });
    }

    [Fact]
    public async Task BuildSkuBaseMergePlanAsync_WithInactiveProducts_IncludesInactiveInDuplicates()
    {
        // Arrange - Bazı ürünler aktif değil
        await SeedProductsAsync(new[]
        {
            new Product { Id = 101, SKU = "9340003", Name = "Base", IsActive = true },
            new Product { Id = 102, SKU = "9340003-1", Name = "Variant 1", IsActive = false },
            new Product { Id = 103, SKU = "9340003-2", Name = "Variant 2", IsActive = true }
        });

        // Act
        var plan = await _service.BuildSkuBaseMergePlanAsync();

        // Assert
        plan.Should().NotBeNull();
        plan.Groups.Should().HaveCount(1);
        
        var group = plan.Groups.First();
        group.DuplicateProductIds.Should().Contain(102, "çünkü inactive ürünler de merge edilebilir");
        group.DuplicateProductIds.Should().Contain(103);
    }

    [Fact]
    public async Task BuildSkuBaseMergePlanAsync_WithNoExactBaseMatch_SelectsCanonicalByLogic()
    {
        // Arrange - Base SKU'ya tam eşleşen ürün yok
        await SeedProductsAsync(new[]
        {
            new Product { Id = 102, SKU = "9340003-1", Name = "Variant 1", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-10) },
            new Product { Id = 103, SKU = "9340003-2", Name = "Variant 2", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-5) }
        });

        // Act
        var plan = await _service.BuildSkuBaseMergePlanAsync();

        // Assert
        plan.Should().NotBeNull();
        plan.Groups.Should().HaveCount(1);
        
        var group = plan.Groups.First();
        group.SkuBase.Should().Be("9340003");
        group.CanonicalProductId.Should().Be(102, "çünkü daha eski ürün canonical olarak seçilir");
        group.DuplicateProductIds.Should().BeEquivalentTo(new[] { 103 });
    }

    [Fact]
    public async Task BuildSkuBaseMergePlanAsync_DoesNotModifyDatabase()
    {
        // Arrange
        await SeedProductsAsync(new[]
        {
            new Product { Id = 101, SKU = "9340003", Name = "Base", IsActive = true },
            new Product { Id = 102, SKU = "9340003-1", Name = "Variant", IsActive = true }
        });

        var productsBefore = await _context.Products.AsNoTracking().ToListAsync();

        // Act
        await _service.BuildSkuBaseMergePlanAsync();

        // Assert - DB değişmemiş olmalı
        var productsAfter = await _context.Products.AsNoTracking().ToListAsync();
        productsAfter.Should().HaveCount(productsBefore.Count);
        
        foreach (var productBefore in productsBefore)
        {
            var productAfter = productsAfter.First(p => p.Id == productBefore.Id);
            productAfter.IsActive.Should().Be(productBefore.IsActive, "IsActive değişmemeli");
            productAfter.SKU.Should().Be(productBefore.SKU, "SKU değişmemeli");
        }
    }

    [Fact]
    public async Task BuildSkuBaseMergePlanAsync_WithComplexSkuPatterns_HandlesCorrectly()
    {
        // Arrange - Farklı SKU pattern'leri
        await SeedProductsAsync(new[]
        {
            new Product { Id = 1, SKU = "ABC-123", Name = "Base 1", IsActive = true },
            new Product { Id = 2, SKU = "ABC-123-X", Name = "Variant 1", IsActive = true },
            new Product { Id = 3, SKU = "ABC-123-Y-Z", Name = "Variant 2", IsActive = true },
            
            new Product { Id = 4, SKU = "XYZ", Name = "Base 2", IsActive = true },
            new Product { Id = 5, SKU = "XYZ-001", Name = "Variant 3", IsActive = true }
        });

        // Act
        var plan = await _service.BuildSkuBaseMergePlanAsync();

        // Assert
        plan.Should().NotBeNull();
        plan.Groups.Should().HaveCount(2);
        
        var group1 = plan.Groups.FirstOrDefault(g => g.SkuBase == "ABC-123");
        group1.Should().NotBeNull();
        group1!.DuplicateProductIds.Should().HaveCount(2);
        
        var group2 = plan.Groups.FirstOrDefault(g => g.SkuBase == "XYZ");
        group2.Should().NotBeNull();
        group2!.DuplicateProductIds.Should().HaveCount(1);
    }

    private async Task SeedProductsAsync(IEnumerable<Product> products)
    {
        await _context.Products.AddRangeAsync(products);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
