using System;
using Katana.Business.UseCases.Sync;
using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using Xunit;

namespace Katana.Tests.Services;

public class SyncServiceTests : IDisposable
{
    private readonly Mock<ILogger<SyncService>> _mockLogger;
    private readonly IntegrationDbContext _context;
    private readonly SyncService _syncService;

    public SyncServiceTests()
    {
        _mockLogger = new Mock<ILogger<SyncService>>();

        var options = new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new IntegrationDbContext(options);

        _syncService = new SyncService(_context, _mockLogger.Object);
    }

    [Fact]
    public async Task SyncStockAsync_WhenCalled_ShouldReturnMockResult()
    {
        // Arrange
        var fromDate = DateTime.UtcNow.AddDays(-7);

        // Act
        var result = await _syncService.SyncStockAsync(fromDate);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.SyncType.Should().Be("STOCK");
    }

    [Fact]
    public async Task SyncInvoicesAsync_WhenCalled_ShouldReturnMockResult()
    {
        // Arrange
        var fromDate = DateTime.UtcNow.AddDays(-7);

        // Act
        var result = await _syncService.SyncInvoicesAsync(fromDate);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.SyncType.Should().Be("INVOICE");
    }

    [Fact]
    public async Task SyncCustomersAsync_WhenCalled_ShouldReturnMockResult()
    {
        // Arrange
        var fromDate = DateTime.UtcNow.AddDays(-7);

        // Act
        var result = await _syncService.SyncCustomersAsync(fromDate);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.SyncType.Should().Be("CUSTOMER");
    }

    [Fact]
    public async Task SyncAllAsync_WhenCalled_ShouldReturnBatchResult()
    {
        // Arrange
        var fromDate = DateTime.UtcNow.AddDays(-7);

        // Act
        var result = await _syncService.SyncAllAsync(fromDate);

        // Assert
        result.Should().NotBeNull();
        result.Results.Should().HaveCount(3);
        result.OverallSuccess.Should().BeTrue();
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}

