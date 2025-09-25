using System;
using ECommerce.Business.Services;
using ECommerce.Core.Interfaces;
using ECommerce.Core.DTOs;
using ECommerce.Core.Entities;
using ECommerce.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using Xunit;

namespace ECommerce.Tests.Services;

public class SyncServiceTests : IDisposable
{
    private readonly Mock<IKatanaService> _mockKatanaService;
    private readonly Mock<ILucaService> _mockLucaService;
    private readonly Mock<IMappingService> _mockMappingService;
    private readonly Mock<ILogger<SyncService>> _mockLogger;
    private readonly IntegrationDbContext _context;
    private readonly SyncService _syncService;

    public SyncServiceTests()
    {
        _mockKatanaService = new Mock<IKatanaService>();
        _mockLucaService = new Mock<ILucaService>();
        _mockMappingService = new Mock<IMappingService>();
        _mockLogger = new Mock<ILogger<SyncService>>();

        var options = new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new IntegrationDbContext(options);

        _syncService = new SyncService(
            _mockKatanaService.Object,
            _mockLucaService.Object,
            _mockMappingService.Object,
            _context,
            _mockLogger.Object);
    }

    [Fact]
    public async Task SyncStockAsync_WhenSuccessful_ShouldReturnSuccessResult()
    {
        // Arrange
        // Add test product to database
        var testProduct = new Product
        {
            SKU = "PRD-123",
            Name = "Test Product",
            Price = 10.0m,
            Stock = 100,
            CategoryId = 1,
            CreatedAt = DateTime.UtcNow
        };
        _context.Products.Add(testProduct);
        await _context.SaveChangesAsync();

        var stockData = new List<KatanaStockDto>
        {
            new() { 
                ProductSKU = "PRD-123", 
                ProductName = "Test Product",
                Quantity = 10, 
                Location = "MAIN",
                MovementType = "IN",
                MovementDate = DateTime.UtcNow
            }
        };

        // Mock location mapping - required by MappingHelper.MapToLucaStock
        var locationMapping = new Dictionary<string, string>
        {
            ["MAIN"] = "MAIN_WAREHOUSE"
        };

        _mockKatanaService.Setup(x => x.GetStockChangesAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(stockData);

        _mockMappingService.Setup(x => x.GetLocationMappingAsync())
            .ReturnsAsync(locationMapping);

        _mockLucaService.Setup(x => x.SendStockMovementsAsync(It.IsAny<List<LucaStockDto>>()))
            .ReturnsAsync(new SyncResultDto 
            { 
                IsSuccess = true,
                ProcessedRecords = 1,
                SuccessfulRecords = 1,
                FailedRecords = 0,
                SyncType = "STOCK",
                Message = "Stock sync completed successfully"
            });

        // Act
        var result = await _syncService.SyncStockAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.ProcessedRecords.Should().Be(1);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _context.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}