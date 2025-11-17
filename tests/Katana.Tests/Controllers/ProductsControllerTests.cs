using System.Linq;
using FluentAssertions;
using Katana.API.Controllers;
using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Katana.Core.Enums;
using Katana.Core.Interfaces;
using Katana.Data.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Katana.Tests.Controllers;

public class ProductsControllerTests
{
    private readonly Mock<IKatanaService> _mockKatanaService;
    private readonly Mock<IProductService> _mockProductService;
    private readonly Mock<ICategoryService> _mockCategoryService;
    private readonly Mock<ILogger<ProductsController>> _mockLogger;
    private readonly Mock<ILoggingService> _mockLoggingService;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly Mock<IOptionsSnapshot<CatalogVisibilitySettings>> _mockCatalogVisibility;
    private readonly ProductsController _controller;

    public ProductsControllerTests()
    {
        _mockKatanaService = new Mock<IKatanaService>();
        _mockProductService = new Mock<IProductService>();
        _mockCategoryService = new Mock<ICategoryService>();
        _mockLogger = new Mock<ILogger<ProductsController>>();
        _mockLoggingService = new Mock<ILoggingService>();
        _mockAuditService = new Mock<IAuditService>();
        _mockCatalogVisibility = new Mock<IOptionsSnapshot<CatalogVisibilitySettings>>();
        _mockCatalogVisibility.Setup(o => o.Value).Returns(new CatalogVisibilitySettings());
        
        _controller = new ProductsController(
            _mockKatanaService.Object,
            _mockProductService.Object,
            _mockCategoryService.Object,
            _mockCatalogVisibility.Object,
            _mockLogger.Object,
            _mockLoggingService.Object,
            _mockAuditService.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithProducts()
    {
        // Arrange
        var products = new List<ProductDto>
        {
            new() { Id = 1, SKU = "PRD001", Name = "Product 1", Price = 100, Stock = 50 },
            new() { Id = 2, SKU = "PRD002", Name = "Product 2", Price = 200, Stock = 30 }
        };
        _mockProductService.Setup(s => s.GetAllProductsAsync()).ReturnsAsync(products);

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value.Should().BeAssignableTo<IEnumerable<ProductDto>>().Subject;
        data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetCustomerCatalog_FiltersInvalidProducts()
    {
        // Arrange
        var categories = new List<CategoryDto>
        {
            new() { Id = 1, Name = "Yayınlanan", IsActive = true },
            new() { Id = 2, Name = "Pasif", IsActive = false }
        };
        _mockCategoryService.Setup(c => c.GetAllAsync()).ReturnsAsync(categories);

        var products = new List<ProductDto>
        {
            new() { Id = 1, SKU = "VISIBLE", Name = "Visible", CategoryId = 1, Stock = 5, IsActive = true },
            new() { Id = 2, SKU = "ZERO", Name = "Zero", CategoryId = 1, Stock = 0, IsActive = true },
            new() { Id = 3, SKU = "PASSIVE", Name = "Passive", CategoryId = 1, Stock = 5, IsActive = false },
            new() { Id = 4, SKU = "NO_CAT", Name = "No Category", CategoryId = 0, Stock = 5, IsActive = true }
        };
        _mockProductService.Setup(s => s.GetAllProductsAsync()).ReturnsAsync(products);

        // Act
        var result = await _controller.GetCustomerCatalog(true);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var payload = okResult.Value.Should().BeOfType<CustomerCatalogResponse>().Subject;
        payload.Data.Should().HaveCount(1);
        payload.Data.First().SKU.Should().Be("VISIBLE");
        payload.HiddenCount.Should().Be(3);
        payload.Filters.HideZeroStockProducts.Should().BeTrue();
    }

    [Fact]
    public async Task GetById_ReturnsOkWhenProductExists()
    {
        // Arrange
        var product = new ProductDto { Id = 1, SKU = "PRD001", Name = "Product 1", Price = 100 };
        _mockProductService.Setup(s => s.GetProductByIdAsync(1)).ReturnsAsync(product);

        // Act
        var result = await _controller.GetById(1);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value.Should().BeOfType<ProductDto>().Subject;
        data.Id.Should().Be(1);
    }

    [Fact]
    public async Task GetById_ReturnsNotFoundWhenProductDoesNotExist()
    {
        // Arrange
        _mockProductService.Setup(s => s.GetProductByIdAsync(99)).ReturnsAsync((ProductDto?)null);

        // Act
        var result = await _controller.GetById(99);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetBySku_ReturnsOkWhenSkuExists()
    {
        // Arrange
        var product = new ProductDto { Id = 1, SKU = "PRD001", Name = "Product 1" };
        _mockProductService.Setup(s => s.GetProductBySkuAsync("PRD001")).ReturnsAsync(product);

        // Act
        var result = await _controller.GetBySku("PRD001");

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value.Should().BeOfType<ProductDto>().Subject;
        data.SKU.Should().Be("PRD001");
    }

    [Fact]
    public async Task Search_ReturnsBadRequestWhenQueryIsEmpty()
    {
        // Act
        var result = await _controller.Search(string.Empty);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Search_ReturnsOkWithMatchingProducts()
    {
        // Arrange
        var products = new List<ProductDto>
        {
            new() { Id = 1, SKU = "PRD001", Name = "Test Product" }
        };
        _mockProductService.Setup(s => s.SearchProductsAsync("Test")).ReturnsAsync(products);

        // Act
        var result = await _controller.Search("Test");

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value.Should().BeAssignableTo<IEnumerable<ProductDto>>().Subject;
        data.Should().HaveCount(1);
    }

    [Fact]
    public async Task Create_ReturnsCreatedWhenValid()
    {
        // Arrange
        var dto = new CreateProductDto { SKU = "PRD001", Name = "New Product", Price = 100, Stock = 10, CategoryId = 1 };
        var product = new ProductDto { Id = 1, SKU = "PRD001", Name = "New Product", Price = 100 };
        _mockProductService.Setup(s => s.CreateProductAsync(dto)).ReturnsAsync(product);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var data = createdResult.Value.Should().BeOfType<ProductDto>().Subject;
        data.SKU.Should().Be("PRD001");
        _mockAuditService.Verify(a => a.LogCreate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Update_ReturnsOkWhenValid()
    {
        // Arrange
        var dto = new UpdateProductDto { SKU = "PRD001", Name = "Updated Product", Price = 150, Stock = 20, CategoryId = 1, IsActive = true };
        var product = new ProductDto { Id = 1, SKU = "PRD001", Name = "Updated Product", Price = 150 };
        _mockProductService.Setup(s => s.UpdateProductAsync(1, dto)).ReturnsAsync(product);

        // Act
        var result = await _controller.Update(1, dto);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value.Should().BeOfType<ProductDto>().Subject;
        data.Price.Should().Be(150);
        _mockAuditService.Verify(a => a.LogUpdate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Update_ReturnsNotFoundWhenProductDoesNotExist()
    {
        // Arrange
        var dto = new UpdateProductDto { SKU = "PRD999", Name = "Unknown", Price = 100, Stock = 10, CategoryId = 1, IsActive = true };
        _mockProductService.Setup(s => s.UpdateProductAsync(99, dto))
            .ThrowsAsync(new KeyNotFoundException("Product not found"));

        // Act
        var result = await _controller.Update(99, dto);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Delete_ReturnsNoContentWhenSuccessful()
    {
        // Arrange
        _mockProductService.Setup(s => s.DeleteProductAsync(1)).ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(1);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _mockAuditService.Verify(a => a.LogDelete(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task GetLowStock_ReturnsOkWithLowStockProducts()
    {
        // Arrange
        var products = new List<ProductDto>
        {
            new() { Id = 1, SKU = "PRD001", Name = "Low Stock Item", Stock = 5 }
        };
        _mockProductService.Setup(s => s.GetLowStockProductsAsync(10)).ReturnsAsync(products);

        // Act
        var result = await _controller.GetLowStock(10);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value.Should().BeAssignableTo<IEnumerable<ProductDto>>().Subject;
        data.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetStatistics_ReturnsOkWithStats()
    {
        // Arrange
        var stats = new ProductStatisticsDto
        {
            TotalProducts = 100,
            ActiveProducts = 80,
            LowStockProducts = 15,
            OutOfStockProducts = 5,
            TotalInventoryValue = 50000
        };
        _mockProductService.Setup(s => s.GetProductStatisticsAsync()).ReturnsAsync(stats);

        // Act
        var result = await _controller.GetStatistics();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value.Should().BeOfType<ProductStatisticsDto>().Subject;
        data.TotalProducts.Should().Be(100);
        data.TotalInventoryValue.Should().Be(50000);
    }

    [Fact]
    public async Task GetKatanaProducts_ReturnsOkWithProducts()
    {
        // Arrange
        var products = new List<KatanaProductDto> { new() { SKU = "KAT001", Name = "Katana Product" } };
        _mockKatanaService.Setup(s => s.GetProductsAsync()).ReturnsAsync(products);

        // Act
        var result = await _controller.GetKatanaProducts(null, null);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }
}
