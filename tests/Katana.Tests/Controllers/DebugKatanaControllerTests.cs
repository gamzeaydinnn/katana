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
        // Arrange
        _mockKatanaService.Setup(s => s.TestConnectionAsync()).ReturnsAsync(true);

        // Act
        var result = await _controller.TestConnection();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task TestConnection_ReturnsOkWhenNotConnected()
    {
        // Arrange
        _mockKatanaService.Setup(s => s.TestConnectionAsync()).ReturnsAsync(false);

        // Act
        var result = await _controller.TestConnection();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task TestConnection_Returns500OnException()
    {
        // Arrange
        _mockKatanaService.Setup(s => s.TestConnectionAsync()).ThrowsAsync(new Exception("Connection failed"));

        // Act
        var result = await _controller.TestConnection();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task GetProducts_ReturnsOkWithCount()
    {
        // Arrange
        var products = new List<KatanaProductDto>
        {
            new KatanaProductDto { SKU = "KAT001", Name = "Product 1" },
            new KatanaProductDto { SKU = "KAT002", Name = "Product 2" }
        };
        _mockKatanaService.Setup(s => s.GetProductsAsync()).ReturnsAsync(products);

        // Act
        var result = await _controller.GetProducts(10);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetProducts_HandlesEmptyList()
    {
        // Arrange
        _mockKatanaService.Setup(s => s.GetProductsAsync()).ReturnsAsync(new List<KatanaProductDto>());

        // Act
        var result = await _controller.GetProducts(10);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetProducts_Returns500OnException()
    {
        // Arrange
        _mockKatanaService.Setup(s => s.GetProductsAsync()).ThrowsAsync(new Exception("API error"));

        // Act
        var result = await _controller.GetProducts();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task GetKatanaInvoices_ReturnsOkWithInvoices()
    {
        // Arrange
        var invoices = new List<KatanaInvoiceDto>
        {
            new KatanaInvoiceDto { InvoiceNo = "INV001", TotalAmount = 100 },
            new KatanaInvoiceDto { InvoiceNo = "INV002", TotalAmount = 200 }
        };
        _mockKatanaService.Setup(s => s.GetInvoicesAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(invoices);

        // Act
        var result = await _controller.GetKatanaInvoices(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetKatanaInvoices_UsesDefaultDatesWhenNotProvided()
    {
        // Arrange
        var invoices = new List<KatanaInvoiceDto>();
        _mockKatanaService.Setup(s => s.GetInvoicesAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(invoices);

        // Act
        var result = await _controller.GetKatanaInvoices(null, null);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockKatanaService.Verify(s => s.GetInvoicesAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Once);
    }
}
