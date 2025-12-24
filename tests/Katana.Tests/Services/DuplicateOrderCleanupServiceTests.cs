using Katana.Business.Services;
using Katana.Business.Interfaces;
using Katana.Core.Interfaces;
using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Katana.Tests.Services;

public class DuplicateOrderCleanupServiceTests
{
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly Mock<ILoggingService> _loggingServiceMock;
    private readonly Mock<ILogger<DuplicateOrderCleanupService>> _loggerMock;

    public DuplicateOrderCleanupServiceTests()
    {
        _auditServiceMock = new Mock<IAuditService>();
        _loggingServiceMock = new Mock<ILoggingService>();
        _loggerMock = new Mock<ILogger<DuplicateOrderCleanupService>>();
    }

    private DuplicateOrderCleanupService CreateService(IntegrationDbContext? context = null)
    {
        var dbContext = context ?? CreateInMemoryContext();
        return new DuplicateOrderCleanupService(
            dbContext,
            _auditServiceMock.Object,
            _loggingServiceMock.Object,
            _loggerMock.Object);
    }

    private static IntegrationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new IntegrationDbContext(options);
    }

    #region ExtractBaseOrderNo Tests

    [Theory]
    [InlineData("SO-SO-84", "SO-84")]
    [InlineData("SO-SO-SO-56", "SO-56")]
    [InlineData("SO-SO-SO-SO-123", "SO-123")]
    [InlineData("SO-84", "SO-84")] // Valid format, no change
    [InlineData("SO-TO-01", "SO-TO-01")] // Different prefix, no change
    [InlineData("", "")]
    [InlineData(null, null)]
    public void ExtractBaseOrderNo_ShouldExtractCorrectly(string? input, string? expected)
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.ExtractBaseOrderNo(input!);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("ABC-ABC-123", "ABC-123")]
    [InlineData("XYZ-XYZ-XYZ-999", "XYZ-999")]
    public void ExtractBaseOrderNo_ShouldHandleRepeatedPrefixes(string input, string expected)
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.ExtractBaseOrderNo(input);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region IsMalformedOrderNo Tests

    [Theory]
    [InlineData("SO-SO-84", true)]
    [InlineData("SO-SO-SO-56", true)]
    [InlineData("SO-84", false)]
    [InlineData("SO-TO-01", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsMalformedOrderNo_ShouldDetectCorrectly(string? input, bool expected)
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.IsMalformedOrderNo(input!);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region AnalyzeDuplicates Tests

    [Fact]
    public async Task AnalyzeDuplicatesAsync_WithNoDuplicates_ReturnsEmptyGroups()
    {
        // Arrange
        var context = CreateInMemoryContext();
        context.SalesOrders.AddRange(
            new Katana.Core.Entities.SalesOrder { Id = 1, OrderNo = "SO-1", Status = "PENDING" },
            new Katana.Core.Entities.SalesOrder { Id = 2, OrderNo = "SO-2", Status = "PENDING" },
            new Katana.Core.Entities.SalesOrder { Id = 3, OrderNo = "SO-3", Status = "PENDING" }
        );
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.AnalyzeDuplicatesAsync();

        // Assert
        Assert.Equal(3, result.TotalOrders);
        Assert.Equal(0, result.DuplicateGroups);
        Assert.Equal(0, result.OrdersToDelete);
        Assert.Empty(result.Groups);
    }

    [Fact]
    public async Task AnalyzeDuplicatesAsync_WithDuplicates_ReturnsCorrectGroups()
    {
        // Arrange
        var context = CreateInMemoryContext();
        context.SalesOrders.AddRange(
            new Katana.Core.Entities.SalesOrder { Id = 1, OrderNo = "SO-84", Status = "PENDING", CreatedAt = DateTime.UtcNow.AddDays(-2) },
            new Katana.Core.Entities.SalesOrder { Id = 2, OrderNo = "SO-84", Status = "PENDING", CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new Katana.Core.Entities.SalesOrder { Id = 3, OrderNo = "SO-84", Status = "PENDING", CreatedAt = DateTime.UtcNow },
            new Katana.Core.Entities.SalesOrder { Id = 4, OrderNo = "SO-85", Status = "PENDING" }
        );
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.AnalyzeDuplicatesAsync();

        // Assert
        Assert.Equal(4, result.TotalOrders);
        Assert.Equal(1, result.DuplicateGroups);
        Assert.Equal(2, result.OrdersToDelete);
        Assert.Single(result.Groups);
        Assert.Equal(1, result.Groups[0].OrderToKeep.Id); // Oldest should be kept
    }

    [Fact]
    public async Task AnalyzeDuplicatesAsync_WithDifferentStatuses_KeepsHighestStatus()
    {
        // Arrange
        var context = CreateInMemoryContext();
        context.SalesOrders.AddRange(
            new Katana.Core.Entities.SalesOrder { Id = 1, OrderNo = "SO-84", Status = "PENDING", CreatedAt = DateTime.UtcNow.AddDays(-2) },
            new Katana.Core.Entities.SalesOrder { Id = 2, OrderNo = "SO-84", Status = "APPROVED", CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new Katana.Core.Entities.SalesOrder { Id = 3, OrderNo = "SO-84", Status = "PENDING", CreatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.AnalyzeDuplicatesAsync();

        // Assert
        Assert.Equal(1, result.DuplicateGroups);
        Assert.Equal(2, result.Groups[0].OrderToKeep.Id); // APPROVED should be kept
        Assert.Equal("Most Advanced Status", result.Groups[0].OrderToKeep.KeepReason);
    }

    #endregion

    #region AnalyzeMalformed Tests

    [Fact]
    public async Task AnalyzeMalformedAsync_WithMalformedOrders_ReturnsCorrectAnalysis()
    {
        // Arrange
        var context = CreateInMemoryContext();
        context.SalesOrders.AddRange(
            new Katana.Core.Entities.SalesOrder { Id = 1, OrderNo = "SO-84", Status = "PENDING" },
            new Katana.Core.Entities.SalesOrder { Id = 2, OrderNo = "SO-SO-84", Status = "PENDING" }, // Malformed, can merge
            new Katana.Core.Entities.SalesOrder { Id = 3, OrderNo = "SO-SO-56", Status = "PENDING" }  // Malformed, can rename
        );
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.AnalyzeMalformedAsync();

        // Assert
        Assert.Equal(2, result.TotalMalformed);
        Assert.Equal(1, result.CanMerge);
        Assert.Equal(1, result.CanRename);
    }

    [Fact]
    public async Task AnalyzeMalformedAsync_WithNoMalformed_ReturnsEmpty()
    {
        // Arrange
        var context = CreateInMemoryContext();
        context.SalesOrders.AddRange(
            new Katana.Core.Entities.SalesOrder { Id = 1, OrderNo = "SO-84", Status = "PENDING" },
            new Katana.Core.Entities.SalesOrder { Id = 2, OrderNo = "SO-85", Status = "PENDING" }
        );
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.AnalyzeMalformedAsync();

        // Assert
        Assert.Equal(0, result.TotalMalformed);
        Assert.Empty(result.Orders);
    }

    #endregion

    #region CleanupDuplicates Tests

    [Fact]
    public async Task CleanupDuplicatesAsync_DryRun_DoesNotDelete()
    {
        // Arrange
        var context = CreateInMemoryContext();
        context.SalesOrders.AddRange(
            new Katana.Core.Entities.SalesOrder { Id = 1, OrderNo = "SO-84", Status = "PENDING", CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new Katana.Core.Entities.SalesOrder { Id = 2, OrderNo = "SO-84", Status = "PENDING", CreatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.CleanupDuplicatesAsync(dryRun: true);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.WasDryRun);
        Assert.Equal(1, result.OrdersDeleted); // Would delete 1
        Assert.Equal(2, await context.SalesOrders.CountAsync()); // But nothing actually deleted
    }

    #endregion
}
