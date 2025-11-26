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

public class MappingControllerTests : IDisposable
{
    private readonly Mock<IMappingService> _mockMappingService;
    private readonly IntegrationDbContext _context;
    private readonly Mock<ILogger<MappingController>> _mockLogger;
    private readonly Mock<IAuditLoggerService> _mockAuditLogger;
    private readonly MappingController _controller;

    public MappingControllerTests()
    {
        _mockMappingService = new Mock<IMappingService>();
        var options = new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new IntegrationDbContext(options);
        _mockLogger = new Mock<ILogger<MappingController>>();
        _mockAuditLogger = new Mock<IAuditLoggerService>();
        _controller = new MappingController(
            _mockMappingService.Object,
            _context,
            _mockLogger.Object,
            _mockAuditLogger.Object);

        
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
    public async Task GetMappings_ReturnsOkWithMappings()
    {
        
        _context.MappingTables.Add(new MappingTable
        {
            MappingType = "SKU",
            SourceValue = "TEST001",
            TargetValue = "ACC001",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        
        var result = await _controller.GetMappings(null, 1, 50);

        
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetMappings_FiltersbyMappingType()
    {
        
        _context.MappingTables.AddRange(
            new MappingTable { MappingType = "SKU", SourceValue = "S1", TargetValue = "T1", IsActive = true, CreatedAt = DateTime.UtcNow },
            new MappingTable { MappingType = "LOCATION", SourceValue = "L1", TargetValue = "W1", IsActive = true, CreatedAt = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        
        var result = await _controller.GetMappings("SKU", 1, 50);

        
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSkuAccountMappings_ReturnsOkWithMappings()
    {
        
        var mappings = new Dictionary<string, string>
        {
            { "SKU001", "ACC001" },
            { "SKU002", "ACC002" }
        };
        _mockMappingService.Setup(s => s.GetSkuToAccountMappingAsync()).ReturnsAsync(mappings);

        
        var result = await _controller.GetSkuAccountMappings();

        
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedMappings = okResult.Value.Should().BeAssignableTo<Dictionary<string, string>>().Subject;
        returnedMappings.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetLocationMappings_ReturnsOkWithMappings()
    {
        
        var mappings = new Dictionary<string, string>
        {
            { "LOC001", "WH001" },
            { "LOC002", "WH002" }
        };
        _mockMappingService.Setup(s => s.GetLocationMappingAsync()).ReturnsAsync(mappings);

        
        var result = await _controller.GetLocationMappings();

        
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedMappings = okResult.Value.Should().BeAssignableTo<Dictionary<string, string>>().Subject;
        returnedMappings.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateMapping_ReturnsCreatedWhenValid()
    {
        
        var request = new CreateMappingRequest
        {
            MappingType = "SKU",
            SourceValue = "TEST001",
            TargetValue = "ACC001",
            Description = "Test mapping",
            IsActive = true
        };

        
        var result = await _controller.CreateMapping(request);

        
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.Value.Should().NotBeNull();
        _mockAuditLogger.Verify(a => a.LogAsync(
            "CREATE",
            "MappingTable",
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task CreateMapping_ReturnsBadRequestWhenDuplicate()
    {
        
        _context.MappingTables.Add(new MappingTable
        {
            MappingType = "SKU",
            SourceValue = "TEST001",
            TargetValue = "ACC001",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var request = new CreateMappingRequest
        {
            MappingType = "SKU",
            SourceValue = "TEST001",
            TargetValue = "ACC002"
        };

        
        var result = await _controller.CreateMapping(request);

        
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetMappingById_ReturnsOkWhenExists()
    {
        
        var mapping = new MappingTable
        {
            MappingType = "SKU",
            SourceValue = "TEST001",
            TargetValue = "ACC001",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.MappingTables.Add(mapping);
        await _context.SaveChangesAsync();

        
        var result = await _controller.GetMappingById(mapping.Id);

        
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetMappingById_ReturnsNotFoundWhenDoesNotExist()
    {
        
        var result = await _controller.GetMappingById(999);

        
        var notFoundResult = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateMapping_ReturnsOkWhenValid()
    {
        
        var mapping = new MappingTable
        {
            MappingType = "SKU",
            SourceValue = "TEST001",
            TargetValue = "ACC001",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.MappingTables.Add(mapping);
        await _context.SaveChangesAsync();

        var request = new UpdateMappingRequest
        {
            TargetValue = "ACC002",
            Description = "Updated",
            IsActive = true
        };

        
        var result = await _controller.UpdateMapping(mapping.Id, request);

        
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
        _mockAuditLogger.Verify(a => a.LogAsync(
            "UPDATE",
            "MappingTable",
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task UpdateMapping_ReturnsNotFoundWhenDoesNotExist()
    {
        
        var request = new UpdateMappingRequest { TargetValue = "ACC002" };

        
        var result = await _controller.UpdateMapping(999, request);

        
        var notFoundResult = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteMapping_ReturnsNoContentWhenSuccessful()
    {
        
        var mapping = new MappingTable
        {
            MappingType = "SKU",
            SourceValue = "TEST001",
            TargetValue = "ACC001",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.MappingTables.Add(mapping);
        await _context.SaveChangesAsync();

        
        var result = await _controller.DeleteMapping(mapping.Id);

        
        result.Should().BeOfType<NoContentResult>();
        _mockAuditLogger.Verify(a => a.LogAsync(
            "DELETE",
            "MappingTable",
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task DeleteMapping_ReturnsNotFoundWhenDoesNotExist()
    {
        
        var result = await _controller.DeleteMapping(999);

        
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().NotBeNull();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
