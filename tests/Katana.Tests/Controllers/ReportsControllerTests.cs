using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Katana.API.Controllers;
using Katana.Data.Context;
using Katana.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Katana.Tests.Controllers;

public class ReportsControllerTests
{
    private static IntegrationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new IntegrationDbContext(options);
    }

    [Fact]
    public async Task GetIntegrationLogs_ReturnsOk_WithEmptyResult()
    {
        // Arrange
        await using var db = CreateContext();
        var controller = new ReportsController(db, new Microsoft.Extensions.Logging.Abstractions.NullLogger<ReportsController>());

        // Act
        var result = await controller.GetIntegrationLogs();

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var value = ok.Value!;
        var totalCount = (int)(value.GetType().GetProperty("totalCount")?.GetValue(value) ?? -1);
        totalCount.Should().Be(0);
        value.GetType().GetProperty("logs").Should().NotBeNull();
    }

    [Fact]
    public async Task GetLastSyncReports_ReturnsThreeDefaults_WhenNoLogs()
    {
        // Arrange
        await using var db = CreateContext();
        var controller = new ReportsController(db, new Microsoft.Extensions.Logging.Abstractions.NullLogger<ReportsController>());

        // Act
        var result = await controller.GetLastSyncReports();

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var items = ok.Value.Should().BeAssignableTo<IEnumerable<object>>().Subject.ToList();
        items.Count.Should().Be(3);
        var syncTypes = items.Select(i => (string?)i.GetType().GetProperty("syncType")?.GetValue(i)).ToList();
        syncTypes.Should().BeEquivalentTo(new[] { "STOCK", "INVOICE", "CUSTOMER" });
    }

    [Fact]
    public async Task GetFailedRecords_ReturnsOk_WithPaging()
    {
        // Arrange
        await using var db = CreateContext();
        // Seed one failed record
        var log = new IntegrationLog { SyncType = "STOCK", Status = Katana.Core.Enums.SyncStatus.Failed, StartTime = DateTime.UtcNow };
        db.IntegrationLogs.Add(log);
        await db.SaveChangesAsync();
        db.FailedSyncRecords.Add(new FailedSyncRecord
        {
            IntegrationLogId = log.Id,
            RecordType = "STOCK",
            RecordId = "1",
            ErrorMessage = "Test",
            OriginalData = "{}",
            FailedAt = DateTime.UtcNow,
            Status = "FAILED"
        });
        await db.SaveChangesAsync();

        var controller = new ReportsController(db, new Microsoft.Extensions.Logging.Abstractions.NullLogger<ReportsController>());

        // Act
        var result = await controller.GetFailedRecords(page: 1, pageSize: 10);

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var value = ok.Value!;
        var totalCount = (int)(value.GetType().GetProperty("totalCount")?.GetValue(value) ?? -1);
        totalCount.Should().Be(1);
    }
}

