using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Katana.API.Controllers;
using Katana.Data.Context;
using Katana.Business.Interfaces;
using Katana.Core.Interfaces;
using Katana.Core.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Katana.Business.Services;

namespace Katana.Tests.Controllers;

public class SalesOrdersControllerTests
{
    private readonly Mock<ILucaService> _mockLucaService;
    private readonly Mock<ILoggingService> _mockLoggingService;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly Mock<IKatanaService> _mockKatanaService;
    private readonly Mock<Microsoft.Extensions.Logging.ILogger<SalesOrdersController>> _mockLogger;
    private readonly Mock<ILocationMappingService> _mockLocationMappingService;
    private readonly IntegrationDbContext _context;
    private readonly SalesOrdersController _controller;

    public SalesOrdersControllerTests()
    {
        // In-memory database setup
        var options = new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_" + System.Guid.NewGuid())
            .Options;
        
        _context = new IntegrationDbContext(options);
        _mockLucaService = new Mock<ILucaService>();
        _mockLoggingService = new Mock<ILoggingService>();
        _mockAuditService = new Mock<IAuditService>();
        _mockKatanaService = new Mock<IKatanaService>();
        _mockLogger = new Mock<Microsoft.Extensions.Logging.ILogger<SalesOrdersController>>();
        _mockLocationMappingService = new Mock<ILocationMappingService>();
        
        _controller = new SalesOrdersController(
            _context,
            _mockLucaService.Object,
            _mockLoggingService.Object,
            _mockAuditService.Object,
            _mockLogger.Object,
            _mockKatanaService.Object,
            _mockLocationMappingService.Object
        );
    }

    [Fact]
    public async Task GetAll_ReturnsOkResult_WithListOfOrders()
    {
        // Arrange
        var customer = new Customer { Id = 1, Title = "Test Customer", Code = "CUST001" };
        _context.Customers.Add(customer);
        
        var orders = new List<SalesOrder>
        {
            new SalesOrder 
            { 
                Id = 1, 
                OrderNo = "SO-001", 
                CustomerId = 1,
                Customer = customer,
                Status = "Open",
                Total = 1000,
                OrderCreatedDate = System.DateTime.UtcNow
            },
            new SalesOrder 
            { 
                Id = 2, 
                OrderNo = "SO-002", 
                CustomerId = 1,
                Customer = customer,
                Status = "Closed",
                Total = 2000,
                OrderCreatedDate = System.DateTime.UtcNow
            }
        };
        
        _context.SalesOrders.AddRange(orders);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedOrders = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);
        Assert.Equal(2, returnedOrders.Count());
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
    public async Task GetById_ReturnsOkResult_WithOrder()
    {
        // Arrange
        var customer = new Customer { Id = 1, Title = "Test Customer", Code = "CUST001" };
        _context.Customers.Add(customer);
        
        var order = new SalesOrder 
        { 
            Id = 1, 
            OrderNo = "SO-001", 
            CustomerId = 1,
            Customer = customer,
            Status = "Open",
            Total = 1000,
            OrderCreatedDate = System.DateTime.UtcNow
        };
        
        _context.SalesOrders.Add(order);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetById(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetStats_ReturnsCorrectStatistics()
    {
        // Arrange
        var customer = new Customer { Id = 1, Title = "Test Customer", Code = "CUST001" };
        _context.Customers.Add(customer);
        
        var orders = new List<SalesOrder>
        {
            new SalesOrder 
            { 
                OrderNo = "SO-001", 
                CustomerId = 1,
                IsSyncedToLuca = true,
                Total = 1000,
                OrderCreatedDate = System.DateTime.UtcNow
            },
            new SalesOrder 
            { 
                OrderNo = "SO-002", 
                CustomerId = 1,
                IsSyncedToLuca = false,
                Total = 2000,
                OrderCreatedDate = System.DateTime.UtcNow
            },
            new SalesOrder 
            { 
                OrderNo = "SO-003", 
                CustomerId = 1,
                IsSyncedToLuca = false,
                LastSyncError = "Error",
                Total = 1500,
                OrderCreatedDate = System.DateTime.UtcNow
            }
        };
        
        _context.SalesOrders.AddRange(orders);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetStats();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        dynamic stats = okResult.Value;
        Assert.Equal(3, stats.TotalOrders);
        Assert.Equal(1, stats.SyncedOrders);
        Assert.Equal(1, stats.ErrorOrders);
        Assert.Equal(1, stats.PendingOrders);
    }
}
