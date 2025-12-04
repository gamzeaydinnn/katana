using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Katana.API.Controllers;
using Katana.Business.Interfaces;
using Katana.Data.Context;
using Katana.Core.Interfaces;
using Katana.Core.Entities;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Katana.Tests.Controllers;

public class ManufacturingOrdersControllerTests
{
    private readonly Mock<ILoggingService> _mockLoggingService;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly IntegrationDbContext _context;
    private readonly ManufacturingOrdersController _controller;

    public ManufacturingOrdersControllerTests()
    {
        var options = new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_MO_" + System.Guid.NewGuid())
            .Options;
        
        _context = new IntegrationDbContext(options);
        _mockLoggingService = new Mock<ILoggingService>();
        _mockAuditService = new Mock<IAuditService>();
        
        _controller = new ManufacturingOrdersController(
            _context,
            _mockLoggingService.Object,
            _mockAuditService.Object
        );
    }

    [Fact]
    public async Task Create_ReturnsCreatedResult_WithValidData()
    {
        // Arrange
        var product = new Product 
        { 
            Id = 1, 
            Name = "Test Product", 
            SKU = "PROD001" 
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var request = new CreateManufacturingOrderRequest
        {
            ProductId = 1,
            Quantity = 100,
            Status = "NotStarted",
            DueDate = System.DateTime.UtcNow.AddDays(7)
        };

        // Act
        var result = await _controller.Create(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto = Assert.IsType<ManufacturingOrderDto>(createdResult.Value);
        Assert.Equal(100, dto.Quantity);
        Assert.Equal("NotStarted", dto.Status);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenProductNotFound()
    {
        // Arrange
        var request = new CreateManufacturingOrderRequest
        {
            ProductId = 999,
            Quantity = 100
        };

        // Act
        var result = await _controller.Create(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetById_ReturnsOrder_WhenExists()
    {
        // Arrange
        var product = new Product { Id = 1, Name = "Test Product", SKU = "PROD001" };
        _context.Products.Add(product);
        
        var order = new ManufacturingOrder
        {
            Id = 1,
            OrderNo = "MO-001",
            ProductId = 1,
            Product = product,
            Quantity = 100,
            Status = "NotStarted",
            DueDate = System.DateTime.UtcNow.AddDays(7),
            CreatedAt = System.DateTime.UtcNow
        };
        _context.ManufacturingOrders.Add(order);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetById(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ManufacturingOrderDto>(okResult.Value);
        Assert.Equal("MO-001", dto.OrderNo);
        Assert.Equal(100, dto.Quantity);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenOrderDoesNotExist()
    {
        // Act
        var result = await _controller.GetById(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task Update_UpdatesOrder_WhenExists()
    {
        // Arrange
        var product = new Product { Id = 1, Name = "Test Product", SKU = "PROD001" };
        _context.Products.Add(product);
        
        var order = new ManufacturingOrder
        {
            Id = 1,
            OrderNo = "MO-001",
            ProductId = 1,
            Quantity = 100,
            Status = "NotStarted",
            DueDate = System.DateTime.UtcNow.AddDays(7),
            CreatedAt = System.DateTime.UtcNow
        };
        _context.ManufacturingOrders.Add(order);
        await _context.SaveChangesAsync();

        var updateRequest = new UpdateManufacturingOrderRequest
        {
            Quantity = 150,
            Status = "InProgress"
        };

        // Act
        var result = await _controller.Update(1, updateRequest);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ManufacturingOrderDto>(okResult.Value);
        Assert.Equal(150, dto.Quantity);
        Assert.Equal("InProgress", dto.Status);
    }

    [Fact]
    public async Task Update_ReturnsNotFound_WhenOrderDoesNotExist()
    {
        // Arrange
        var updateRequest = new UpdateManufacturingOrderRequest
        {
            Quantity = 150
        };

        // Act
        var result = await _controller.Update(999, updateRequest);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task Delete_DeletesOrder_WhenNotSynced()
    {
        // Arrange
        var product = new Product { Id = 1, Name = "Test Product", SKU = "PROD001" };
        _context.Products.Add(product);
        
        var order = new ManufacturingOrder
        {
            Id = 1,
            OrderNo = "MO-001",
            ProductId = 1,
            Quantity = 100,
            Status = "NotStarted",
            DueDate = System.DateTime.UtcNow.AddDays(7),
            IsSynced = false,
            CreatedAt = System.DateTime.UtcNow
        };
        _context.ManufacturingOrders.Add(order);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Delete(1);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        
        // Verify deletion
        var deletedOrder = await _context.ManufacturingOrders.FindAsync(1);
        Assert.Null(deletedOrder);
    }

    [Fact]
    public async Task Delete_ReturnsBadRequest_WhenOrderIsSynced()
    {
        // Arrange
        var product = new Product { Id = 1, Name = "Test Product", SKU = "PROD001" };
        _context.Products.Add(product);
        
        var order = new ManufacturingOrder
        {
            Id = 1,
            OrderNo = "MO-001",
            ProductId = 1,
            Quantity = 100,
            Status = "Completed",
            DueDate = System.DateTime.UtcNow.AddDays(7),
            IsSynced = true,
            CreatedAt = System.DateTime.UtcNow
        };
        _context.ManufacturingOrders.Add(order);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Delete(1);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetStats_ReturnsCorrectStatistics()
    {
        // Arrange
        var product = new Product { Id = 1, Name = "Test Product", SKU = "PROD001" };
        _context.Products.Add(product);
        
        var orders = new[]
        {
            new ManufacturingOrder
            {
                OrderNo = "MO-001",
                ProductId = 1,
                Quantity = 100,
                Status = "NotStarted",
                DueDate = System.DateTime.UtcNow.AddDays(7),
                IsSynced = false,
                CreatedAt = System.DateTime.UtcNow
            },
            new ManufacturingOrder
            {
                OrderNo = "MO-002",
                ProductId = 1,
                Quantity = 200,
                Status = "InProgress",
                DueDate = System.DateTime.UtcNow.AddDays(7),
                IsSynced = false,
                CreatedAt = System.DateTime.UtcNow
            },
            new ManufacturingOrder
            {
                OrderNo = "MO-003",
                ProductId = 1,
                Quantity = 150,
                Status = "Completed",
                DueDate = System.DateTime.UtcNow.AddDays(7),
                IsSynced = true,
                CreatedAt = System.DateTime.UtcNow
            }
        };
        
        _context.ManufacturingOrders.AddRange(orders);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetStats();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        dynamic stats = okResult.Value;
        Assert.Equal(3, stats.Total);
        Assert.Equal(1, stats.NotStarted);
        Assert.Equal(1, stats.InProgress);
        Assert.Equal(1, stats.Completed);
        Assert.Equal(1, stats.Synced);
        Assert.Equal(2, stats.NotSynced);
        Assert.Equal(450m, stats.TotalQuantity);
    }
}
