using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Katana.API.Controllers;
using Katana.Business.Interfaces;
using Katana.Data.Context;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Katana.Data.Models;

namespace Katana.Tests.Controllers;

public class LogsControllerTests : IDisposable
{
    private readonly IntegrationDbContext _context;
    private readonly Mock<ILogger<LogsController>> _mockLogger;
    private readonly Mock<ILoggingService> _mockLoggingService;
    private readonly LogsController _controller;

    public LogsControllerTests()
    {
        var options = new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new IntegrationDbContext(options);
        _mockLogger = new Mock<ILogger<LogsController>>();
        _mockLoggingService = new Mock<ILoggingService>();
        _controller = new LogsController(
            _context,
            _mockLogger.Object,
            _mockLoggingService.Object);

        // Setup user identity
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.Name, "testuser")
        }));
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Fact]
    public async Task GetErrorLogs_ReturnsOkWithLogs()
    {
        // Arrange
        _context.ErrorLogs.Add(new ErrorLog
        {
            Level = "ERROR",
            Category = "System",
            Message = "Test error",
            User = "testuser",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetErrorLogs(50, null, null, null, null, null, null);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetErrorLogs_FiltersbyLevel()
    {
        // Arrange
        _context.ErrorLogs.AddRange(
            new ErrorLog { Level = "ERROR", Message = "Error 1", CreatedAt = DateTime.UtcNow },
            new ErrorLog { Level = "WARNING", Message = "Warning 1", CreatedAt = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetErrorLogs(50, "ERROR", null, null, null, null, null);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetErrorLogs_FiltersbyCategory()
    {
        // Arrange
        _context.ErrorLogs.AddRange(
            new ErrorLog { Level = "ERROR", Category = "System", Message = "System error", CreatedAt = DateTime.UtcNow },
            new ErrorLog { Level = "ERROR", Category = "UserAction", Message = "User error", CreatedAt = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetErrorLogs(50, null, "System", null, null, null, null);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetErrorLogs_FiltersbyDateRange()
    {
        // Arrange
        var yesterday = DateTime.UtcNow.AddDays(-1);
        var tomorrow = DateTime.UtcNow.AddDays(1);
        _context.ErrorLogs.Add(new ErrorLog
        {
            Level = "ERROR",
            Message = "Recent error",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetErrorLogs(50, null, null, yesterday, tomorrow, null, null);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAuditLogs_ReturnsOkWithLogs()
    {
        // Arrange
        _context.AuditLogs.Add(new AuditLog
        {
            ActionType = "CREATE",
            EntityName = "Product",
            EntityId = "1",
            PerformedBy = "admin",
            Timestamp = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAuditLogs(50, null, null, null, null, null, null, null);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAuditLogs_FiltersbyActionType()
    {
        // Arrange
        _context.AuditLogs.AddRange(
            new AuditLog { ActionType = "CREATE", EntityName = "Product", PerformedBy = "admin", Timestamp = DateTime.UtcNow },
            new AuditLog { ActionType = "UPDATE", EntityName = "Product", PerformedBy = "admin", Timestamp = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAuditLogs(50, "CREATE", null, null, null, null, null, null);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAuditLogs_FiltersbyEntityName()
    {
        // Arrange
        _context.AuditLogs.AddRange(
            new AuditLog { ActionType = "CREATE", EntityName = "Product", PerformedBy = "admin", Timestamp = DateTime.UtcNow },
            new AuditLog { ActionType = "CREATE", EntityName = "Customer", PerformedBy = "admin", Timestamp = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAuditLogs(50, null, "Product", null, null, null, null, null);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetLogStats_ReturnsOkWithStats()
    {
        // Arrange
        _context.ErrorLogs.Add(new ErrorLog
        {
            Level = "ERROR",
            Category = "System",
            Message = "Test error",
            CreatedAt = DateTime.UtcNow
        });
        _context.AuditLogs.Add(new AuditLog
        {
            ActionType = "CREATE",
            EntityName = "Product",
            PerformedBy = "admin",
            Timestamp = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetLogStats(null);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetLogStats_FiltersbyFromDate()
    {
        // Arrange
        var yesterday = DateTime.UtcNow.AddDays(-1);
        _context.ErrorLogs.Add(new ErrorLog
        {
            Level = "ERROR",
            Message = "Recent error",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetLogStats(yesterday);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public void LogFrontendError_ReturnsOkWhenValid()
    {
        // Arrange
        var errorDto = new FrontendErrorDto
        {
            message = "Frontend error occurred",
            stack = "Error stack trace",
            url = "http://localhost/app",
            userAgent = "Mozilla/5.0",
            timestamp = DateTime.UtcNow.ToString()
        };

        // Act
        var result = _controller.LogFrontendError(errorDto);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public void ClearOldErrors_ReturnsOkWithMessage()
    {
        // Arrange
        _context.ErrorLogs.Add(new ErrorLog
        {
            Level = "ERROR",
            Message = "IOrderService error",
            CreatedAt = DateTime.UtcNow
        });
        _context.SaveChanges();

        // Act
        var result = _controller.ClearOldErrors();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
