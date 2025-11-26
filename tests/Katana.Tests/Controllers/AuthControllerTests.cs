using FluentAssertions;
using Katana.API.Controllers;
using Katana.Business.DTOs;
using Katana.Business.Interfaces;
using Katana.Core.Entities;
using Katana.Data.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace Katana.Tests.Controllers;

public class AuthControllerTests : IDisposable
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<AuthController>> _mockLogger;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly IntegrationDbContext _context;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<AuthController>>();
        _mockAuditService = new Mock<IAuditService>();

        
        var jwtSection = new Mock<IConfigurationSection>();
        jwtSection.Setup(x => x["Key"]).Returns("ThisIsASecretKeyForJwtTokenGenerationWithAtLeast256Bits");
        jwtSection.Setup(x => x["Issuer"]).Returns("KatanaAPI");
        jwtSection.Setup(x => x["Audience"]).Returns("KatanaWebApp");
        _mockConfiguration.Setup(x => x.GetSection("Jwt")).Returns(jwtSection.Object);

        
        var options = new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new IntegrationDbContext(options);

        
        using var sha = SHA256.Create();
        var passwordHash = Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes("Katana2025!")));
        _context.Users.Add(new User
        {
            Id = 1,
            Username = "admin",
            PasswordHash = passwordHash,
            Role = "Admin",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        _context.SaveChanges();

        _controller = new AuthController(_mockConfiguration.Object, _mockLogger.Object, _mockAuditService.Object, _context);
        
        
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
        httpContext.Request.Headers["User-Agent"] = "TestAgent";
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    [Fact]
    public async Task Login_ReturnsOkWithToken_WhenValidCredentials()
    {
        
        var loginRequest = new LoginRequest("admin", "Katana2025!");

        
        var result = await _controller.Login(loginRequest);

        
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.Value.Should().NotBeNull();
        _mockAuditService.Verify(a => a.LogLogin(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenInvalidUsername()
    {
        
        var loginRequest = new LoginRequest("wronguser", "Katana2025!");

        
        var result = await _controller.Login(loginRequest);

        
        result.Should().BeOfType<UnauthorizedObjectResult>();
        _mockAuditService.Verify(a => a.LogLogin(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenInvalidPassword()
    {
        
        var loginRequest = new LoginRequest("admin", "wrongpassword");

        
        var result = await _controller.Login(loginRequest);

        
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenUserInactive()
    {
        
        using var sha = SHA256.Create();
        var passwordHash = Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes("Test123!")));
        _context.Users.Add(new User
        {
            Id = 2,
            Username = "inactive",
            PasswordHash = passwordHash,
            Role = "User",
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        });
        _context.SaveChanges();

        var loginRequest = new LoginRequest("inactive", "Test123!");

        
        var result = await _controller.Login(loginRequest);

        
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Login_ReturnsTokenWithCorrectFormat_WhenValid()
    {
        
        var loginRequest = new LoginRequest("admin", "Katana2025!");

        
        var result = await _controller.Login(loginRequest);

        
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.Value.Should().NotBeNull();
        
        var response = okResult.Value.Should().BeAssignableTo<LoginResponse>().Subject;
        response.Token.Should().NotBeNullOrEmpty();
        
        var parts = response.Token.Split('.');
        parts.Should().HaveCount(3, "JWT should have 3 parts (header.payload.signature)");
    }
}
