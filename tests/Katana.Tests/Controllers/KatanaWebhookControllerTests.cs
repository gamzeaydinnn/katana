using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Katana.API.Controllers;
using Katana.Business.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using Katana.Data.Models;

namespace Katana.Tests.Controllers;

public class KatanaWebhookControllerTests
{
    private readonly Mock<IPendingStockAdjustmentService> _mockPendingService;
    private readonly Mock<ILogger<KatanaWebhookController>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly KatanaWebhookController _controller;

    public KatanaWebhookControllerTests()
    {
        _mockPendingService = new Mock<IPendingStockAdjustmentService>();
        _mockLogger = new Mock<ILogger<KatanaWebhookController>>();
        _mockConfiguration = new Mock<IConfiguration>();
        
        // Setup configuration
        _mockConfiguration.Setup(c => c["KatanaApi:WebhookSecret"]).Returns("test-secret-key");
        
        _controller = new KatanaWebhookController(
            _mockPendingService.Object,
            _mockLogger.Object,
            _mockConfiguration.Object);

        // Setup HttpContext with headers
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        _controller.Request.Headers["X-Katana-Signature"] = "test-secret-key";
    }

    [Fact]
    public async Task ReceiveStockChange_ReturnsOkWhenValidSignature()
    {
        // Arrange
        var webhook = new KatanaStockChangeWebhook
        {
            Event = "stock.updated",
            OrderId = "ORD-12345",
            ProductId = 1,
            Sku = "SKU001",
            QuantityChange = -5,
            Timestamp = DateTime.UtcNow
        };
        var createdAdjustment = new PendingStockAdjustment
        {
            Id = 1,
            ExternalOrderId = webhook.OrderId,
            ProductId = webhook.ProductId,
            Sku = webhook.Sku,
            Quantity = webhook.QuantityChange
        };
        _mockPendingService.Setup(s => s.CreateAsync(It.IsAny<PendingStockAdjustment>()))
            .ReturnsAsync(createdAdjustment);

        // Act
        var result = await _controller.ReceiveStockChange(webhook);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
        _mockPendingService.Verify(s => s.CreateAsync(It.IsAny<PendingStockAdjustment>()), Times.Once);
    }

    [Fact]
    public async Task ReceiveStockChange_ReturnsUnauthorizedWhenInvalidSignature()
    {
        // Arrange
        _controller.Request.Headers["X-Katana-Signature"] = "invalid-signature";
        var webhook = new KatanaStockChangeWebhook
        {
            Event = "stock.updated",
            OrderId = "ORD-12345",
            ProductId = 1,
            Sku = "SKU001",
            QuantityChange = -5
        };

        // Act
        var result = await _controller.ReceiveStockChange(webhook);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task ReceiveStockChange_ReturnsUnauthorizedWhenNoSignature()
    {
        // Arrange
        _controller.Request.Headers.Remove("X-Katana-Signature");
        var webhook = new KatanaStockChangeWebhook
        {
            Event = "stock.updated",
            OrderId = "ORD-12345",
            ProductId = 1,
            Sku = "SKU001",
            QuantityChange = -5
        };

        // Act
        var result = await _controller.ReceiveStockChange(webhook);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task ReceiveStockChange_ReturnsBadRequestWhenExceptionOccurs()
    {
        // Arrange
        var webhook = new KatanaStockChangeWebhook
        {
            Event = "stock.updated",
            OrderId = "ORD-12345",
            ProductId = 1,
            Sku = "SKU001",
            QuantityChange = -5
        };
        _mockPendingService.Setup(s => s.CreateAsync(It.IsAny<PendingStockAdjustment>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.ReceiveStockChange(webhook);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task ReceiveStockChange_CreatesCorrectPendingAdjustment()
    {
        // Arrange
        var webhook = new KatanaStockChangeWebhook
        {
            Event = "stock.updated",
            OrderId = "ORD-12345",
            ProductId = 1,
            Sku = "SKU001",
            QuantityChange = -5,
            Timestamp = DateTime.UtcNow
        };
        PendingStockAdjustment? capturedAdjustment = null;
        _mockPendingService.Setup(s => s.CreateAsync(It.IsAny<PendingStockAdjustment>()))
            .Callback<PendingStockAdjustment>(adj => capturedAdjustment = adj)
            .ReturnsAsync(new PendingStockAdjustment { Id = 1 });

        // Act
        await _controller.ReceiveStockChange(webhook);

        // Assert
        capturedAdjustment.Should().NotBeNull();
        capturedAdjustment!.ExternalOrderId.Should().Be("ORD-12345");
        capturedAdjustment.ProductId.Should().Be(1);
        capturedAdjustment.Sku.Should().Be("SKU001");
        capturedAdjustment.Quantity.Should().Be(-5);
        capturedAdjustment.RequestedBy.Should().Be("Katana-API");
        capturedAdjustment.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task TestWebhook_ReturnsOk()
    {
        // Arrange
        _mockPendingService.Setup(s => s.CreateAsync(It.IsAny<PendingStockAdjustment>()))
            .ReturnsAsync(new PendingStockAdjustment { Id = 1 });

        // Act
        var result = await _controller.TestWebhook();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task ReceiveStockChange_UsesDefaultSkuWhenNotProvided()
    {
        // Arrange
        var webhook = new KatanaStockChangeWebhook
        {
            Event = "stock.updated",
            OrderId = "ORD-12345",
            ProductId = 123,
            Sku = null, // No SKU provided
            QuantityChange = -5
        };
        PendingStockAdjustment? capturedAdjustment = null;
        _mockPendingService.Setup(s => s.CreateAsync(It.IsAny<PendingStockAdjustment>()))
            .Callback<PendingStockAdjustment>(adj => capturedAdjustment = adj)
            .ReturnsAsync(new PendingStockAdjustment { Id = 1 });

        // Act
        await _controller.ReceiveStockChange(webhook);

        // Assert
        capturedAdjustment.Should().NotBeNull();
        capturedAdjustment!.Sku.Should().Be("PRODUCT-123");
    }
}
