using FluentAssertions;
using Katana.API.Controllers;
using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Katana.Core.Enums;
using Katana.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Katana.Tests.Controllers;

public class OrdersControllerTests
{
    private readonly Mock<IOrderService> _mockOrderService;
    private readonly Mock<ILoggingService> _mockLoggingService;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly OrdersController _controller;

    public OrdersControllerTests()
    {
        _mockOrderService = new Mock<IOrderService>();
        _mockLoggingService = new Mock<ILoggingService>();
        _mockAuditService = new Mock<IAuditService>();
        
        _controller = new OrdersController(
            _mockOrderService.Object,
            _mockLoggingService.Object,
            _mockAuditService.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithOrders()
    {
        
        var orders = new List<OrderDto>
        {
            new() { Id = 1, CustomerId = 1, Status = "Pending", TotalAmount = 100 },
            new() { Id = 2, CustomerId = 2, Status = "Delivered", TotalAmount = 200 }
        };
        _mockOrderService.Setup(s => s.GetAllAsync()).ReturnsAsync(orders);

        
        var result = await _controller.GetAll();

        
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value.Should().BeAssignableTo<IEnumerable<OrderDto>>().Subject;
        data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetById_ReturnsOkWhenOrderExists()
    {
        
        var order = new OrderDto { Id = 1, CustomerId = 1, Status = "Pending", TotalAmount = 150 };
        _mockOrderService.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(order);

        
        var result = await _controller.GetById(1);

        
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value.Should().BeOfType<OrderDto>().Subject;
        data.Id.Should().Be(1);
        data.TotalAmount.Should().Be(150);
    }

    [Fact]
    public async Task GetById_ReturnsNotFoundWhenOrderDoesNotExist()
    {
        
        _mockOrderService.Setup(s => s.GetByIdAsync(99)).ReturnsAsync((OrderDto?)null);

        
        var result = await _controller.GetById(99);

        
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Create_ReturnsCreatedWhenValid()
    {
        
        var createDto = new CreateOrderDto 
        { 
            CustomerId = 1, 
            Items = new List<CreateOrderItemDto>
            {
                new() { ProductId = 1, Quantity = 2, UnitPrice = 50 }
            }
        };
        var order = new OrderDto 
        { 
            Id = 1, 
            CustomerId = 1, 
            Status = "Pending", 
            TotalAmount = 100,
            Items = new List<OrderItemDto>
            {
                new() { ProductId = 1, ProductName = "Product 1", Quantity = 2, UnitPrice = 50 }
            }
        };
        _mockOrderService.Setup(s => s.CreateAsync(createDto)).ReturnsAsync(order);

        
        var result = await _controller.Create(createDto);

        
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var data = createdResult.Value.Should().BeOfType<OrderDto>().Subject;
        data.Id.Should().Be(1);
        data.CustomerId.Should().Be(1);
        _mockAuditService.Verify(a => a.LogCreate(
            "Order", 
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Create_ReturnsInternalServerErrorWhenExceptionOccurs()
    {
        
        var createDto = new CreateOrderDto { CustomerId = 1, Items = new() };
        _mockOrderService.Setup(s => s.CreateAsync(createDto))
            .ThrowsAsync(new Exception("Database error"));

        
        var result = await _controller.Create(createDto);

        
        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task UpdateStatus_ReturnsNoContentWhenSuccessful()
    {
        
        _mockOrderService.Setup(s => s.UpdateStatusAsync(1, OrderStatus.Delivered)).ReturnsAsync(true);

        
        var result = await _controller.UpdateStatus(1, OrderStatus.Delivered);

        
        result.Should().BeOfType<NoContentResult>();
        _mockAuditService.Verify(a => a.LogUpdate(
            "Order", 
            "1", 
            It.IsAny<string>(), 
            null, 
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task UpdateStatus_ReturnsNotFoundWhenOrderDoesNotExist()
    {
        
        _mockOrderService.Setup(s => s.UpdateStatusAsync(99, OrderStatus.Delivered)).ReturnsAsync(false);

        
        var result = await _controller.UpdateStatus(99, OrderStatus.Delivered);

        
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Delete_ReturnsNoContentWhenSuccessful()
    {
        
        _mockOrderService.Setup(s => s.DeleteAsync(1)).ReturnsAsync(true);

        
        var result = await _controller.Delete(1);

        
        result.Should().BeOfType<NoContentResult>();
        _mockAuditService.Verify(a => a.LogDelete(
            "Order", 
            "1", 
            It.IsAny<string>(), 
            null), Times.Once);
    }

    [Fact]
    public async Task Delete_ReturnsNotFoundWhenOrderDoesNotExist()
    {
        
        _mockOrderService.Setup(s => s.DeleteAsync(99)).ReturnsAsync(false);

        
        var result = await _controller.Delete(99);

        
        result.Should().BeOfType<NotFoundResult>();
    }
}
