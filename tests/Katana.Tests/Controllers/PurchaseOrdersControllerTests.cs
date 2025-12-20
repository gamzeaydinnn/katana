using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Katana.API.Controllers;
using Katana.Data.Context;
using Katana.Business.Interfaces;
using Katana.Core.Interfaces;
using Katana.Core.Entities;
using Katana.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Katana.Tests.Controllers;

public class PurchaseOrdersControllerTests
{
    private readonly Mock<ILucaService> _mockLucaService;
    private readonly Mock<ILoggingService> _mockLoggingService;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly Mock<IMemoryCache> _mockCache;
    private readonly IntegrationDbContext _context;
    private readonly PurchaseOrdersController _controller;

    public PurchaseOrdersControllerTests()
    {
        var options = new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_PO_" + System.Guid.NewGuid())
            .Options;
        
        _context = new IntegrationDbContext(options);
        _mockLucaService = new Mock<ILucaService>();
        _mockLoggingService = new Mock<ILoggingService>();
        _mockAuditService = new Mock<IAuditService>();
        _mockCache = new Mock<IMemoryCache>();
        
        _controller = new PurchaseOrdersController(
            _context,
            _mockLucaService.Object,
            _mockLoggingService.Object,
            _mockAuditService.Object,
            _mockCache.Object
        );
    }

    [Fact]
    public async Task Create_ReturnsCreatedResult_WithValidData()
    {
        // Arrange
        var supplier = new Supplier 
        { 
            Id = 1, 
            Name = "Test Supplier", 
            Code = "SUP001" 
        };
        _context.Suppliers.Add(supplier);
        
        var product = new Product 
        { 
            Id = 1, 
            Name = "Test Product", 
            SKU = "PROD001" 
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var request = new CreatePurchaseOrderRequest
        {
            SupplierId = 1,
            OrderDate = System.DateTime.UtcNow,
            Items = new List<CreatePurchaseOrderItemRequest>
            {
                new CreatePurchaseOrderItemRequest
                {
                    ProductId = 1,
                    Quantity = 10,
                    UnitPrice = 100
                }
            }
        };

        // Act
        var result = await _controller.Create(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.NotNull(createdResult.Value);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenSupplierNotFound()
    {
        // Arrange
        var request = new CreatePurchaseOrderRequest
        {
            SupplierId = 999,
            Items = new List<CreatePurchaseOrderItemRequest>()
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
        var supplier = new Supplier { Id = 1, Name = "Test Supplier", Code = "SUP001" };
        _context.Suppliers.Add(supplier);
        
        var order = new PurchaseOrder
        {
            Id = 1,
            OrderNo = "PO-001",
            SupplierId = 1,
            Supplier = supplier,
            Status = PurchaseOrderStatus.Pending,
            TotalAmount = 1000,
            OrderDate = System.DateTime.UtcNow
        };
        _context.PurchaseOrders.Add(order);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetById(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<PurchaseOrderDetailDto>(okResult.Value);
        Assert.Equal("PO-001", dto.OrderNo);
    }

    [Fact]
    public async Task Delete_ReturnsOk_WhenOrderNotSynced()
    {
        // Arrange
        var supplier = new Supplier { Id = 1, Name = "Test Supplier", Code = "SUP001" };
        _context.Suppliers.Add(supplier);
        
        var order = new PurchaseOrder
        {
            Id = 1,
            OrderNo = "PO-001",
            SupplierId = 1,
            Status = PurchaseOrderStatus.Pending,
            TotalAmount = 1000,
            IsSyncedToLuca = false,
            OrderDate = System.DateTime.UtcNow
        };
        _context.PurchaseOrders.Add(order);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Delete(1);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        
        // Verify deletion
        var deletedOrder = await _context.PurchaseOrders.FindAsync(1);
        Assert.Null(deletedOrder);
    }

    [Fact]
    public async Task Delete_ReturnsBadRequest_WhenOrderIsSynced()
    {
        // Arrange
        var supplier = new Supplier { Id = 1, Name = "Test Supplier", Code = "SUP001" };
        _context.Suppliers.Add(supplier);
        
        var order = new PurchaseOrder
        {
            Id = 1,
            OrderNo = "PO-001",
            SupplierId = 1,
            Status = PurchaseOrderStatus.Pending,
            TotalAmount = 1000,
            IsSyncedToLuca = true,
            OrderDate = System.DateTime.UtcNow
        };
        _context.PurchaseOrders.Add(order);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Delete(1);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }
}
