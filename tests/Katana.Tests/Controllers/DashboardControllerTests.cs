using FluentAssertions;
using Katana.API.Controllers;
using Katana.Business.Interfaces;
using Katana.Business.Services;
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

public class DashboardControllerTests : IDisposable
{
    private readonly Mock<IKatanaService> _mockKatanaService;
    private readonly Mock<IDashboardService> _mockDashboardService;
    private readonly Mock<ILogger<DashboardController>> _mockLogger;
    private readonly IntegrationDbContext _context;
    private readonly DashboardController _controller;

    public DashboardControllerTests()
    {
        var options = new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new IntegrationDbContext(options);
        _mockKatanaService = new Mock<IKatanaService>();
        _mockDashboardService = new Mock<IDashboardService>();
        _mockLogger = new Mock<ILogger<DashboardController>>();

        
        var realDashboardService = new DashboardService(_context);

        _controller = new DashboardController(
            realDashboardService,
            _mockKatanaService.Object,
            _context,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Get_ReturnsOkWithStats()
    {
        
        var products = new List<KatanaProductDto>
        {
            new() { SKU = "SKU1", Name = "Product1", IsActive = true },
            new() { SKU = "SKU2", Name = "Product2", IsActive = true },
            new() { SKU = "SKU3", Name = "Product3", IsActive = false }
        };
        _mockKatanaService.Setup(s => s.GetProductsAsync()).ReturnsAsync(products);

        
        var result = await _controller.Get();

        
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSyncStats_ReturnsOkWithData_WhenHealthy()
    {
        
        _mockKatanaService.Setup(s => s.TestConnectionAsync()).ReturnsAsync(true);
        _mockKatanaService.Setup(s => s.GetProductsAsync()).ReturnsAsync(new List<KatanaProductDto>
        {
            new() { SKU = "SKU1", Name = "Product1", IsActive = true },
            new() { SKU = "SKU2", Name = "Product2", IsActive = false }
        });

        _context.SyncOperationLogs.Add(new SyncOperationLog
        {
            SyncType = "PRODUCT",
            Status = "SUCCESS",
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        
        var result = await _controller.GetSyncStats();

        
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSyncStats_ReturnsWarning_WhenUnhealthy()
    {
        
        _mockKatanaService.Setup(s => s.TestConnectionAsync()).ReturnsAsync(false);

        
        var result = await _controller.GetSyncStats();

        
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<object>().Subject;
        var warningProp = response.GetType().GetProperty("warning");
        warningProp.Should().NotBeNull();
    }

    [Fact]
    public async Task GetDashboardStats_ReturnsOk_WhenServiceSucceeds()
    {
        
        _context.Invoices.Add(new Invoice 
        { 
            TotalAmount = 5000, 
            InvoiceDate = DateTime.UtcNow,
            InvoiceNo = "INV-001",
            CustomerId = 1,
            Status = "PAID",
            IsSynced = true
        });
        _context.Invoices.Add(new Invoice 
        { 
            TotalAmount = 5000, 
            InvoiceDate = DateTime.UtcNow,
            InvoiceNo = "INV-002",
            CustomerId = 1,
            Status = "PAID",
            IsSynced = true
        });
        await _context.SaveChangesAsync();

        
        var result = await _controller.GetDashboardStats();

        
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var stats = okResult.Value.Should().BeOfType<DashboardStatsDto>().Subject;
        stats.TotalSales.Should().Be(10000);
    }

    [Fact]
    public async Task GetRecentActivities_ReturnsOkWithActivities()
    {
        
        _context.SyncOperationLogs.AddRange(new[]
        {
            new SyncOperationLog
            {
                SyncType = "PRODUCT",
                Status = "SUCCESS",
                StartTime = DateTime.UtcNow.AddMinutes(-10),
                EndTime = DateTime.UtcNow.AddMinutes(-9)
            },
            new SyncOperationLog
            {
                SyncType = "STOCK",
                Status = "FAILED",
                StartTime = DateTime.UtcNow.AddMinutes(-5),
                EndTime = DateTime.UtcNow.AddMinutes(-4)
            }
        });
        await _context.SaveChangesAsync();

        
        var result = await _controller.GetRecentActivities();

        
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<object>().Subject;
        var countProp = response.GetType().GetProperty("count");
        countProp?.GetValue(response).Should().Be(2);
    }

    [Fact]
    public async Task Get_ReturnsServerError_WhenExceptionThrown()
    {
        
        _mockKatanaService.Setup(s => s.GetProductsAsync()).ThrowsAsync(new Exception("API Error"));

        
        var result = await _controller.Get();

        
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
