using ECommerce.Core.DTOs;
using ECommerce.Core.Helpers;
using FluentAssertions;
using Xunit;

namespace ECommerce.Tests.Helpers;

public class MappingHelperTests
{
    [Fact]
    public void MapToProduct_Should_MapCorrectly()
    {
        // Arrange
        var katanaDto = new KatanaProductDto
        {
            SKU = "PRD-123",
            Name = "Test Product",
            Price = 100.50m,
            CategoryId = 1,
            IsActive = true
        };

        // Act
        var result = MappingHelper.MapToProduct(katanaDto);

        // Assert
        result.Should().NotBeNull();
        result.SKU.Should().Be("PRD-123");
        result.Name.Should().Be("Test Product");
        result.Price.Should().Be(100.50m);
    }

    [Fact]
    public void MapToLucaStock_Should_MapCorrectly()
    {
        // Arrange
        var stock = new Core.Entities.Stock
        {
            ProductId = 1,
            Location = "MAIN",
            Quantity = 10,
            Type = "IN",
            Timestamp = DateTime.UtcNow
        };
        
        var product = new Core.Entities.Product
        {
            Id = 1,
            SKU = "PRD-123",
            Name = "Test Product",
            Price = 100.50m
        };
        
        var locationMapping = new Dictionary<string, string>
        {
            { "MAIN", "WH001" }
        };

        // Act
        var result = MappingHelper.MapToLucaStock(stock, product, locationMapping);

        // Assert
        result.Should().NotBeNull();
        result.ProductCode.Should().Be("PRD-123");
        result.Quantity.Should().Be(10);
        result.WarehouseCode.Should().Be("WH001");
    }
}