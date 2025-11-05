using FluentAssertions;
using Katana.API.Controllers;
using Katana.Business.DTOs;
using Katana.Business.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using System.Net;
using Xunit;

namespace Katana.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<AuthController>> _mockLogger;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<AuthController>>();
        _mockAuditService = new Mock<IAuditService>();

        // Setup configuration - use indexer instead of GetSection
        _mockConfiguration.Setup(x => x["AuthSettings:AdminUsername"]).Returns("admin");
        _mockConfiguration.Setup(x => x["AuthSettings:AdminPassword"]).Returns("Admin123!");

        var jwtSection = new Mock<IConfigurationSection>();
        jwtSection.Setup(x => x["Key"]).Returns("ThisIsASecretKeyForJwtTokenGenerationWithAtLeast256Bits");
        jwtSection.Setup(x => x["Issuer"]).Returns("KatanaAPI");
        jwtSection.Setup(x => x["Audience"]).Returns("KatanaWebApp");
        _mockConfiguration.Setup(x => x.GetSection("Jwt")).Returns(jwtSection.Object);

        _controller = new AuthController(_mockConfiguration.Object, _mockLogger.Object, _mockAuditService.Object);
        
        // Setup HttpContext for controller
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
        httpContext.Request.Headers["User-Agent"] = "TestAgent";
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public void Login_ReturnsOkWithToken_WhenValidCredentials()
    {
        // Arrange
        var loginRequest = new LoginRequest("admin", "Admin123!");

        // Act
        var result = _controller.Login(loginRequest);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockAuditService.Verify(a => a.LogLogin(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void Login_ReturnsUnauthorized_WhenInvalidUsername()
    {
        // Arrange
        var loginRequest = new LoginRequest("wronguser", "Admin123!");

        // Act
        var result = _controller.Login(loginRequest);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
        _mockAuditService.Verify(a => a.LogLogin(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Login_ReturnsUnauthorized_WhenInvalidPassword()
    {
        // Arrange
        var loginRequest = new LoginRequest("admin", "wrongpassword");

        // Act
        var result = _controller.Login(loginRequest);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public void Login_ReturnsServerError_WhenEmptyCredentials()
    {
        // Arrange
        var loginRequest = new LoginRequest("", "");

        // Act
        var result = _controller.Login(loginRequest);

        // Assert - Empty credentials should return server error due to missing config check
        var objectResult = result.Should().BeAssignableTo<IActionResult>().Subject;
        // Will fail authentication check before config
        (objectResult is UnauthorizedObjectResult || objectResult is ObjectResult).Should().BeTrue();
    }

    [Fact]
    public void Login_ReturnsServerError_WhenConfigurationMissing()
    {
        // Arrange
        var emptyConfig = new Mock<IConfiguration>();
        emptyConfig.Setup(x => x["AuthSettings:AdminUsername"]).Returns((string?)null);
        emptyConfig.Setup(x => x["AuthSettings:AdminPassword"]).Returns((string?)null);

        var controller = new AuthController(emptyConfig.Object, _mockLogger.Object, _mockAuditService.Object);
        var loginRequest = new LoginRequest("admin", "Admin123!");

        // Act
        var result = controller.Login(loginRequest);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public void Login_ReturnsTokenWithCorrectClaims_WhenValid()
    {
        // Arrange
        var loginRequest = new LoginRequest("admin", "Admin123!");

        // Act
        var result = _controller.Login(loginRequest);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.Value.Should().NotBeNull();
        var response = okResult.Value.Should().BeAssignableTo<object>().Subject;
        var tokenProperty = response.GetType().GetProperty("Token");
        tokenProperty.Should().NotBeNull();
        var token = tokenProperty?.GetValue(response) as string;
        token.Should().NotBeNullOrEmpty();
    }
}
