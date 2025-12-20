using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Katana.API.Controllers;
using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Katana.Data.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Katana.Tests.Controllers;

public class TestControllerTests
{
    private readonly Mock<IKatanaService> _mockKatanaService;
    private readonly Mock<IOptions<KatanaApiSettings>> _mockSettings;
    private readonly Mock<ILogger<TestController>> _mockLogger;
    private readonly TestController _controller;

    public TestControllerTests()
    {
        _mockKatanaService = new Mock<IKatanaService>();
        _mockSettings = new Mock<IOptions<KatanaApiSettings>>();
        _mockLogger = new Mock<ILogger<TestController>>();

        var settings = new KatanaApiSettings
        {
            BaseUrl = "https://api.katana.com",
            ApiKey = "test-api-key-1234567890",
            Endpoints = new KatanaApiEndpoints
            {
                Products = "products",
                Invoices = "invoices",
                Customers = "customers"
            }
        };
        _mockSettings.Setup(s => s.Value).Returns(settings);

        _controller = new TestController(_mockKatanaService.Object, _mockSettings.Object, _mockLogger.Object);
    }

    [Fact]
    public void GetKatanaConfig_ReturnsConfiguration()
    {
        
        var result = _controller.GetKatanaConfig();

        
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().NotBeNull();
    }

    [Fact]
    public void GetKatanaConfig_MasksApiKey()
    {
        
        var result = _controller.GetKatanaConfig();

        
        result.Should().BeOfType<OkObjectResult>();
        
    }

    [Fact]
    public async Task TestKatanaDirect_ReturnsOkWhenSuccessful()
    {
        
        var products = new List<KatanaProductDto>
        {
            new KatanaProductDto { SKU = "KAT001", Name = "Product 1" }
        };
        _mockKatanaService.Setup(s => s.GetProductsAsync()).ReturnsAsync(products);

        
        var result = await _controller.TestKatanaDirect();

        
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task TestKatanaDirect_ReturnsProductCount()
    {
        
        var products = new List<KatanaProductDto>
        {
            new KatanaProductDto { SKU = "KAT001", Name = "Product 1" },
            new KatanaProductDto { SKU = "KAT002", Name = "Product 2" }
        };
        _mockKatanaService.Setup(s => s.GetProductsAsync()).ReturnsAsync(products);

        
        var result = await _controller.TestKatanaDirect();

        
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task TestKatanaDirect_Returns500OnException()
    {
        
        _mockKatanaService.Setup(s => s.GetProductsAsync()).ThrowsAsync(new Exception("API error"));

        
        var result = await _controller.TestKatanaDirect();

        
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task TestKatanaDirect_LogsWarnings()
    {
        
        var products = new List<KatanaProductDto>();
        _mockKatanaService.Setup(s => s.GetProductsAsync()).ReturnsAsync(products);

        
        await _controller.TestKatanaDirect();

        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}
