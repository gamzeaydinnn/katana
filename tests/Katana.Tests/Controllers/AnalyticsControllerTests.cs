using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Katana.API.Controllers;
using Katana.Business.Interfaces;
using Katana.Data.Context;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Katana.Core.DTOs;

namespace Katana.Tests.Controllers;

public class AnalyticsControllerTests : IDisposable
{
    private readonly Mock<IKatanaService> _mockKatanaService;
    private readonly IntegrationDbContext _context;
    private readonly Mock<ILogger<AnalyticsController>> _mockLogger;
    private readonly AnalyticsController _controller;

    public AnalyticsControllerTests()
    {
        _mockKatanaService = new Mock<IKatanaService>();
        var options = new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new IntegrationDbContext(options);
        _mockLogger = new Mock<ILogger<AnalyticsController>>();
        _controller = new AnalyticsController(
            _mockKatanaService.Object,
            _context,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GetStockReport_ReturnsOkWithReport()
    {
        // Arrange
        var products = new List<KatanaProductDto>
        {
            new() { SKU = "P001", Name = "Product 1", Price = 100.00m, IsActive = true },
            new() { SKU = "P002", Name = "Product 2", Price = 200.00m, IsActive = false }
        };
        _mockKatanaService.Setup(s => s.GetProductsAsync()).ReturnsAsync(products);

        // Act
        var result = await _controller.GetStockReport();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetStockReport_ReturnsInternalServerErrorWhenExceptionOccurs()
    {
        // Arrange
        _mockKatanaService.Setup(s => s.GetProductsAsync())
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await _controller.GetStockReport();

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task GetSyncReport_ReturnsOkWithLogs()
    {
        // Arrange
        _context.SyncOperationLogs.Add(new Katana.Core.Entities.SyncOperationLog
        {
            SyncType = "STOCK",
            Status = "SUCCESS",
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddSeconds(5),
            SuccessfulRecords = 10,
            FailedRecords = 0
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetSyncReport();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSyncReport_ReturnsOkWithEmptyLogsWhenNoData()
    {
        // Arrange - No logs added

        // Act
        var result = await _controller.GetSyncReport();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSummaryReport_ReturnsOkWithSummary()
    {
        // Arrange
        var products = new List<KatanaProductDto>
        {
            new() { SKU = "P001", Name = "Product 1", IsActive = true },
            new() { SKU = "P002", Name = "Product 2", IsActive = false }
        };
        _mockKatanaService.Setup(s => s.GetProductsAsync()).ReturnsAsync(products);

        _context.SyncOperationLogs.Add(new Katana.Core.Entities.SyncOperationLog
        {
            SyncType = "STOCK",
            Status = "SUCCESS",
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow,
            SuccessfulRecords = 10,
            FailedRecords = 0
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetSummaryReport();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSummaryReport_ReturnsInternalServerErrorWhenExceptionOccurs()
    {
        // Arrange
        _mockKatanaService.Setup(s => s.GetProductsAsync())
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await _controller.GetSummaryReport();

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
