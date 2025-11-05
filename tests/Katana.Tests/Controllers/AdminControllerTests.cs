using FluentAssertions;
using Katana.API.Controllers;
using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Katana.Core.Entities;
using Katana.Data.Context;
using Katana.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Katana.Tests.Controllers;

public class AdminControllerTests : IDisposable
{
    private readonly Mock<IKatanaService> _mockKatanaService;
    private readonly Mock<ILogger<AdminController>> _mockLogger;
    private readonly Mock<ILoggingService> _mockLoggingService;
    private readonly Mock<IPendingStockAdjustmentService> _mockPendingService;
    private readonly IntegrationDbContext _context;
    private readonly AdminController _controller;

    public AdminControllerTests()
    {
        var options = new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new IntegrationDbContext(options);
        _mockKatanaService = new Mock<IKatanaService>();
        _mockLogger = new Mock<ILogger<AdminController>>();
        _mockLoggingService = new Mock<ILoggingService>();
        _mockPendingService = new Mock<IPendingStockAdjustmentService>();

        _controller = new AdminController(
            _mockKatanaService.Object,
            _context,
            _mockLogger.Object,
            _mockLoggingService.Object,
            _mockPendingService.Object);
    }

    [Fact]
    public async Task GetPendingAdjustments_ReturnsOkWithData()
    {
        // Arrange
        _context.PendingStockAdjustments.Add(new PendingStockAdjustment
        {
            Id = 1,
            ProductId = 1,
            Sku = "SKU1",
            Quantity = 10,
            Status = "Pending",
            RequestedAt = DateTimeOffset.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetPendingAdjustments();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<object>().Subject;
        var totalProp = response.GetType().GetProperty("total");
        totalProp?.GetValue(response).Should().Be(1);
    }

    [Fact]
    public async Task ApprovePendingAdjustment_ReturnsOk_WhenSuccessful()
    {
        // Arrange
        var id = 1L;
        _mockPendingService.Setup(s => s.ApproveAsync(id, It.IsAny<string>())).ReturnsAsync(true);

        // Act
        var result = await _controller.ApprovePendingAdjustment(id);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ApprovePendingAdjustment_ReturnsNotFound_WhenNotExists()
    {
        // Arrange
        var id = 999L;
        _mockPendingService.Setup(s => s.ApproveAsync(id, It.IsAny<string>())).ReturnsAsync(false);
        _mockPendingService.Setup(s => s.GetByIdAsync(id)).ReturnsAsync((PendingStockAdjustment?)null);

        // Act
        var result = await _controller.ApprovePendingAdjustment(id);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task RejectPendingAdjustment_ReturnsOk_WhenSuccessful()
    {
        // Arrange
        var id = 1L;
        var dto = new AdminController.RejectDto { RejectedBy = "admin", Reason = "Invalid" };
        _mockPendingService.Setup(s => s.RejectAsync(id, It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

        // Act
        var result = await _controller.RejectPendingAdjustment(id, dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task RejectPendingAdjustment_ReturnsNotFound_WhenNotExists()
    {
        // Arrange
        var id = 999L;
        var dto = new AdminController.RejectDto { RejectedBy = "admin", Reason = "Invalid" };
        _mockPendingService.Setup(s => s.RejectAsync(id, It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);
        _mockPendingService.Setup(s => s.GetByIdAsync(id)).ReturnsAsync((PendingStockAdjustment?)null);

        // Act
        var result = await _controller.RejectPendingAdjustment(id, dto);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetStatistics_ReturnsOkWithStats()
    {
        // Arrange
        _mockKatanaService.Setup(s => s.GetProductsAsync()).ReturnsAsync(new List<KatanaProductDto>
        {
            new() { SKU = "SKU1", Name = "Product1", IsActive = true },
            new() { SKU = "SKU2", Name = "Product2", IsActive = false }
        });

        // Act
        var result = await _controller.GetStatistics();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetProducts_ReturnsOkWithPaginatedData()
    {
        // Arrange
        var products = new List<KatanaProductDto>
        {
            new() { SKU = "SKU1", Name = "Product1", IsActive = true },
            new() { SKU = "SKU2", Name = "Product2", IsActive = true }
        };
        _mockKatanaService.Setup(s => s.GetProductsAsync()).ReturnsAsync(products);

        // Act
        var result = await _controller.GetProducts(page: 1, pageSize: 10);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<object>().Subject;
        var totalProp = response.GetType().GetProperty("total");
        totalProp?.GetValue(response).Should().Be(2);
    }

    [Fact]
    public async Task GetProductById_ReturnsOk_WhenExists()
    {
        // Arrange
        var sku = "SKU1";
        var product = new KatanaProductDto { SKU = sku, Name = "Product1", IsActive = true };
        _mockKatanaService.Setup(s => s.GetProductBySkuAsync(sku)).ReturnsAsync(product);

        // Act
        var result = await _controller.GetProductById(sku);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetProductById_ReturnsNotFound_WhenNotExists()
    {
        // Arrange
        var sku = "INVALID";
        _mockKatanaService.Setup(s => s.GetProductBySkuAsync(sku)).ReturnsAsync((KatanaProductDto?)null);

        // Act
        var result = await _controller.GetProductById(sku);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public void GetSyncLogs_ReturnsOkWithPaginatedData()
    {
        // Arrange
        _context.SyncOperationLogs.AddRange(new[]
        {
            new SyncOperationLog { SyncType = "PRODUCT", Status = "SUCCESS", StartTime = DateTime.UtcNow },
            new SyncOperationLog { SyncType = "STOCK", Status = "FAILED", StartTime = DateTime.UtcNow }
        });
        _context.SaveChanges();

        // Act
        var result = _controller.GetSyncLogs(page: 1, pageSize: 10);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<object>().Subject;
        var totalProp = response.GetType().GetProperty("total");
        totalProp?.GetValue(response).Should().Be(2);
    }

    [Fact]
    public async Task CheckKatanaHealth_ReturnsHealthy_WhenConnected()
    {
        // Arrange
        _mockKatanaService.Setup(s => s.TestConnectionAsync()).ReturnsAsync(true);

        // Act
        var result = await _controller.CheckKatanaHealth();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<object>().Subject;
        var healthProp = response.GetType().GetProperty("isHealthy");
        healthProp?.GetValue(response).Should().Be(true);
    }

    [Fact]
    public async Task CheckKatanaHealth_ReturnsUnhealthy_WhenDisconnected()
    {
        // Arrange
        _mockKatanaService.Setup(s => s.TestConnectionAsync()).ReturnsAsync(false);

        // Act
        var result = await _controller.CheckKatanaHealth();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<object>().Subject;
        var healthProp = response.GetType().GetProperty("isHealthy");
        healthProp?.GetValue(response).Should().Be(false);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
