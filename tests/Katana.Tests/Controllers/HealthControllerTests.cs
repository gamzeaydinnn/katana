using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Katana.API.Controllers;
using Katana.Business.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace Katana.Tests.Controllers;

public class HealthControllerTests
{
    private readonly Mock<ILogger<HealthController>> _mockLogger;
    private readonly Mock<IKatanaService> _mockKatanaService;
    private readonly Mock<ILucaService> _mockLucaService;
    private readonly HealthController _controller;

    public HealthControllerTests()
    {
        _mockLogger = new Mock<ILogger<HealthController>>();
        _mockKatanaService = new Mock<IKatanaService>();
        _mockLucaService = new Mock<ILucaService>();
        _controller = new HealthController(
            _mockLogger.Object,
            _mockKatanaService.Object,
            _mockLucaService.Object);
    }

    [Fact]
    public void GetHealth_ReturnsOkWithHealthyStatus()
    {
        var result = _controller.GetHealth();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public void GetHealth_ReturnsStatusHealthy()
    {
        var result = _controller.GetHealth();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = okResult.Value;
        value.Should().NotBeNull();
        
        var properties = value!.GetType().GetProperties();
        properties.Should().Contain(p => p.Name == "status");
        properties.Should().Contain(p => p.Name == "checkedAt");
    }

    [Fact]
    public async Task CheckKatanaHealth_WhenHealthy_ReturnsOk()
    {
        _mockKatanaService.Setup(x => x.TestConnectionAsync()).ReturnsAsync(true);

        var result = await _controller.CheckKatanaHealth();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var health = okResult.Value as ExternalServiceHealthResult;
        health.Should().NotBeNull();
        health!.IsHealthy.Should().BeTrue();
        health.Service.Should().Be("Katana API");
    }

    [Fact]
    public async Task CheckKatanaHealth_WhenUnhealthy_Returns503()
    {
        _mockKatanaService.Setup(x => x.TestConnectionAsync()).ReturnsAsync(false);

        var result = await _controller.CheckKatanaHealth();

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(503);
    }

    [Fact]
    public async Task CheckLucaHealth_WhenHealthy_ReturnsOk()
    {
        _mockLucaService.Setup(x => x.TestConnectionAsync()).ReturnsAsync(true);

        var result = await _controller.CheckLucaHealth();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var health = okResult.Value as ExternalServiceHealthResult;
        health.Should().NotBeNull();
        health!.IsHealthy.Should().BeTrue();
        health.Service.Should().Be("Luca API");
    }

    [Fact]
    public async Task CheckLucaHealth_WhenUnhealthy_Returns503()
    {
        _mockLucaService.Setup(x => x.TestConnectionAsync()).ReturnsAsync(false);

        var result = await _controller.CheckLucaHealth();

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(503);
    }

    [Fact]
    public async Task CheckAllServices_WhenAllHealthy_ReturnsHealthy()
    {
        _mockKatanaService.Setup(x => x.TestConnectionAsync()).ReturnsAsync(true);
        _mockLucaService.Setup(x => x.TestConnectionAsync()).ReturnsAsync(true);

        var result = await _controller.CheckAllServices();

        var okResult = result.Should().BeOfType<ObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task CheckAllServices_WhenOneUnhealthy_ReturnsDegraded()
    {
        _mockKatanaService.Setup(x => x.TestConnectionAsync()).ReturnsAsync(true);
        _mockLucaService.Setup(x => x.TestConnectionAsync()).ReturnsAsync(false);

        var result = await _controller.CheckAllServices();

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(503);
    }
}
