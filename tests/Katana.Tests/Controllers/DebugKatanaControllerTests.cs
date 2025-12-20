using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Katana.API.Controllers;
using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Katana.Tests.Controllers;

public class DebugKatanaControllerTests
{
    private readonly Mock<IKatanaService> _mockKatanaService;
    private readonly Mock<ILogger<DebugKatanaController>> _mockLogger;
    private readonly DebugKatanaController _controller;

    public DebugKatanaControllerTests()
    {
        _mockKatanaService = new Mock<IKatanaService>();
        _mockLogger = new Mock<ILogger<DebugKatanaController>>();
        _controller = new DebugKatanaController(_mockKatanaService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task TestConnection_ReturnsOkWhenConnected()
    {
        
        _mockKatanaService.Setup(s => s.TestConnectionAsync()).ReturnsAsync(true);

        
        var result = await _controller.TestConnection();

        
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task TestConnection_ReturnsOkWhenNotConnected()
    {
        
        _mockKatanaService.Setup(s => s.TestConnectionAsync()).ReturnsAsync(false);

        
        var result = await _controller.TestConnection();

        
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task TestConnection_Returns500OnException()
    {
        
        _mockKatanaService.Setup(s => s.TestConnectionAsync()).ThrowsAsync(new Exception("Connection failed"));

        
        var result = await _controller.TestConnection();

        
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task GetProducts_ReturnsOkWithCount()
    {
        
        var products = new List<KatanaProductDto>
        {
            new KatanaProductDto { SKU = "KAT001", Name = "Product 1" },
            new KatanaProductDto { SKU = "KAT002", Name = "Product 2" }
        };
        _mockKatanaService.Setup(s => s.GetProductsAsync()).ReturnsAsync(products);

        
        var result = await _controller.GetProducts(10);

        
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetProducts_HandlesEmptyList()
    {
        
        _mockKatanaService.Setup(s => s.GetProductsAsync()).ReturnsAsync(new List<KatanaProductDto>());

        
        var result = await _controller.GetProducts(10);

        
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetProducts_Returns500OnException()
    {
        
        _mockKatanaService.Setup(s => s.GetProductsAsync()).ThrowsAsync(new Exception("API error"));

        
        var result = await _controller.GetProducts();

        
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task GetKatanaInvoices_ReturnsOkWithInvoices()
    {
        
        var invoices = new List<KatanaInvoiceDto>
        {
            new KatanaInvoiceDto { InvoiceNo = "INV001", TotalAmount = 100 },
            new KatanaInvoiceDto { InvoiceNo = "INV002", TotalAmount = 200 }
        };
        _mockKatanaService.Setup(s => s.GetInvoicesAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(invoices);

        
        var result = await _controller.GetKatanaInvoices(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

        
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetKatanaInvoices_UsesDefaultDatesWhenNotProvided()
    {
        
        var invoices = new List<KatanaInvoiceDto>();
        _mockKatanaService.Setup(s => s.GetInvoicesAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(invoices);

        
        var result = await _controller.GetKatanaInvoices(null, null);

        
        result.Should().BeOfType<OkObjectResult>();
        _mockKatanaService.Verify(s => s.GetInvoicesAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Once);
    }
}
