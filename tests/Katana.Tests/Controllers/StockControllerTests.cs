using FluentAssertions;
using Katana.API.Controllers;
using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Katana.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Katana.Tests.Controllers;

public class StockControllerTests
{
    private readonly Mock<IStockService> _mockStockService;
    private readonly Mock<IKatanaService> _mockKatanaService;
    private readonly Mock<ILogger<StockController>> _mockLogger;
    private readonly StockController _controller;

    public StockControllerTests()
    {
        _mockStockService = new Mock<IStockService>();
        _mockKatanaService = new Mock<IKatanaService>();
        _mockLogger = new Mock<ILogger<StockController>>();
        _controller = new StockController(_mockStockService.Object, _mockKatanaService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetLocalStockMovements_ReturnsOkWithData()
    {
        
        var movements = new List<StockDto>
        {
            new() { Id = 1, ProductId = 1, Quantity = 10, Type = "IN" },
            new() { Id = 2, ProductId = 2, Quantity = 5, Type = "OUT" }
        };
        _mockStockService.Setup(s => s.GetAllStockMovementsAsync()).ReturnsAsync(movements);

        
        var result = await _controller.GetLocalStockMovements();

        
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<object>().Subject;
        response.GetType().GetProperty("count")?.GetValue(response).Should().Be(2);
    }

    [Fact]
    public async Task GetStockMovementsByProduct_ReturnsOkWithData()
    {
        
        var productId = 1;
        var movements = new List<StockDto>
        {
            new() { Id = 1, ProductId = productId, Quantity = 10, Type = "IN" }
        };
        _mockStockService.Setup(s => s.GetStockMovementsByProductIdAsync(productId)).ReturnsAsync(movements);

        
        var result = await _controller.GetStockMovementsByProduct(productId);

        
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task CreateStockMovement_ReturnsCreated_WhenValid()
    {
        
        var dto = new CreateStockMovementDto { ProductId = 1, Quantity = 10, Type = "IN" };
        var created = new StockDto { Id = 1, ProductId = 1, Quantity = 10, Type = "IN" };
        _mockStockService.Setup(s => s.CreateStockMovementAsync(dto)).ReturnsAsync(created);

        
        var result = await _controller.CreateStockMovement(dto);

        
        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task CreateStockMovement_ReturnsBadRequest_WhenInvalid()
    {
        
        var dto = new CreateStockMovementDto { ProductId = 0, Quantity = 0, Type = "" };
        _mockStockService.Setup(s => s.CreateStockMovementAsync(dto))
            .ThrowsAsync(new ArgumentException("Invalid data"));

        
        var result = await _controller.CreateStockMovement(dto);

        
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task DeleteStockMovement_ReturnsOk_WhenExists()
    {
        
        var id = 1;
        _mockStockService.Setup(s => s.DeleteStockMovementAsync(id)).ReturnsAsync(true);

        
        var result = await _controller.DeleteStockMovement(id);

        
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task DeleteStockMovement_ReturnsNotFound_WhenNotExists()
    {
        
        var id = 999;
        _mockStockService.Setup(s => s.DeleteStockMovementAsync(id)).ReturnsAsync(false);

        
        var result = await _controller.DeleteStockMovement(id);

        
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetStockSummary_ReturnsOkWithData()
    {
        
        var summary = new List<StockSummaryDto>
        {
            new() { ProductId = 1, TotalStock = 100 },
            new() { ProductId = 2, TotalStock = 50 }
        };
        _mockStockService.Setup(s => s.GetStockSummaryAsync()).ReturnsAsync(summary);

        
        var result = await _controller.GetStockSummary();

        
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetStockSummaryByProduct_ReturnsOk_WhenExists()
    {
        
        var productId = 1;
        var summary = new StockSummaryDto { ProductId = productId, TotalStock = 100 };
        _mockStockService.Setup(s => s.GetStockSummaryByProductIdAsync(productId)).ReturnsAsync(summary);

        
        var result = await _controller.GetStockSummaryByProduct(productId);

        
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetStockSummaryByProduct_ReturnsNotFound_WhenNotExists()
    {
        
        var productId = 999;
        _mockStockService.Setup(s => s.GetStockSummaryByProductIdAsync(productId)).ReturnsAsync((StockSummaryDto?)null);

        
        var result = await _controller.GetStockSummaryByProduct(productId);

        
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetUnsyncedStockMovements_ReturnsOkWithData()
    {
        
        var unsynced = new List<StockDto>
        {
            new() { Id = 1, ProductId = 1, IsSynced = false }
        };
        _mockStockService.Setup(s => s.GetUnsyncedStockMovementsAsync()).ReturnsAsync(unsynced);

        
        var result = await _controller.GetUnsyncedStockMovements();

        
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task MarkAsSynced_ReturnsOk_WhenExists()
    {
        
        var id = 1;
        _mockStockService.Setup(s => s.MarkAsSyncedAsync(id)).ReturnsAsync(true);

        
        var result = await _controller.MarkAsSynced(id);

        
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task MarkAsSynced_ReturnsNotFound_WhenNotExists()
    {
        
        var id = 999;
        _mockStockService.Setup(s => s.MarkAsSyncedAsync(id)).ReturnsAsync(false);

        
        var result = await _controller.MarkAsSynced(id);

        
        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
