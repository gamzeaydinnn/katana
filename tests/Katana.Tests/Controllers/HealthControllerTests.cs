using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Katana.API.Controllers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Katana.Tests.Controllers;

public class HealthControllerTests
{
    private readonly Mock<ILogger<HealthController>> _mockLogger;
    private readonly HealthController _controller;

    public HealthControllerTests()
    {
        _mockLogger = new Mock<ILogger<HealthController>>();
        _controller = new HealthController(_mockLogger.Object);
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
    public void GetHealth_LogsInformation()
    {
        
        _controller.GetHealth();

        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("API Health check successful")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
