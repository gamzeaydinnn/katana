using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Katana.API.Controllers;
using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Katana.Data.Context;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Katana.Core.Entities;
using Katana.Business.DTOs;

namespace Katana.Tests.Controllers;

public class SyncControllerTests
{
    private readonly Mock<ISyncService> _mockSyncService;
    private readonly IntegrationDbContext _context;
    private readonly Mock<ILogger<SyncController>> _mockLogger;
    private readonly Mock<ILoggingService> _mockLoggingService;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly SyncController _controller;

    public SyncControllerTests()
    {
        _mockSyncService = new Mock<ISyncService>();
        var options = new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new IntegrationDbContext(options);
        _mockLogger = new Mock<ILogger<SyncController>>();
        _mockLoggingService = new Mock<ILoggingService>();
        _mockAuditService = new Mock<IAuditService>();
        _controller = new SyncController(
            _mockSyncService.Object,
            _context,
            _mockLogger.Object,
            _mockLoggingService.Object,
            _mockAuditService.Object);
    }

    [Fact]
    public async Task RunCompleteSync_ReturnsOkWhenSuccessful()
    {
        
        var result = new BatchSyncResultDto
        {
            Results = new List<SyncResultDto>
            {
                new() { IsSuccess = true, SyncType = "STOCK", SuccessfulRecords = 10 },
                new() { IsSuccess = true, SyncType = "INVOICE", SuccessfulRecords = 5 }
            }
        };
        _mockSyncService.Setup(s => s.SyncAllAsync(null)).ReturnsAsync(result);

        
        var actionResult = await _controller.RunCompleteSync(null);

        
        var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedResult = okResult.Value.Should().BeAssignableTo<BatchSyncResultDto>().Subject;
        returnedResult.OverallSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task RunCompleteSync_ReturnsBadRequestWhenFailed()
    {
        
        var result = new BatchSyncResultDto
        {
            Results = new List<SyncResultDto>
            {
                new() { IsSuccess = false, SyncType = "STOCK", FailedRecords = 3 }
            }
        };
        _mockSyncService.Setup(s => s.SyncAllAsync(null)).ReturnsAsync(result);

        
        var actionResult = await _controller.RunCompleteSync(null);

        
        var badRequestResult = actionResult.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var returnedResult = badRequestResult.Value.Should().BeAssignableTo<BatchSyncResultDto>().Subject;
        returnedResult.OverallSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task RunStockSync_ReturnsOkWhenSuccessful()
    {
        
        var result = new SyncResultDto
        {
            IsSuccess = true,
            SyncType = "STOCK",
            SuccessfulRecords = 15
        };
        _mockSyncService.Setup(s => s.SyncStockAsync(null)).ReturnsAsync(result);

        
        var actionResult = await _controller.RunStockSync(null);

        
        var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedResult = okResult.Value.Should().BeAssignableTo<SyncResultDto>().Subject;
        returnedResult.IsSuccess.Should().BeTrue();
        returnedResult.SyncType.Should().Be("STOCK");
    }

    [Fact]
    public async Task RunStockSync_ReturnsBadRequestWhenFailed()
    {
        
        var result = new SyncResultDto
        {
            IsSuccess = false,
            SyncType = "STOCK",
            FailedRecords = 5
        };
        _mockSyncService.Setup(s => s.SyncStockAsync(null)).ReturnsAsync(result);

        
        var actionResult = await _controller.RunStockSync(null);

        
        var badRequestResult = actionResult.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var returnedResult = badRequestResult.Value.Should().BeAssignableTo<SyncResultDto>().Subject;
        returnedResult.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task RunInvoiceSync_ReturnsOkWhenSuccessful()
    {
        
        var result = new SyncResultDto
        {
            IsSuccess = true,
            SyncType = "INVOICE",
            SuccessfulRecords = 8
        };
        _mockSyncService.Setup(s => s.SyncInvoicesAsync(null)).ReturnsAsync(result);

        
        var actionResult = await _controller.RunInvoiceSync(null);

        
        var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedResult = okResult.Value.Should().BeAssignableTo<SyncResultDto>().Subject;
        returnedResult.IsSuccess.Should().BeTrue();
        returnedResult.SyncType.Should().Be("INVOICE");
    }

    [Fact]
    public async Task RunInvoiceSync_ReturnsBadRequestWhenFailed()
    {
        
        var result = new SyncResultDto
        {
            IsSuccess = false,
            SyncType = "INVOICE",
            FailedRecords = 2
        };
        _mockSyncService.Setup(s => s.SyncInvoicesAsync(null)).ReturnsAsync(result);

        
        var actionResult = await _controller.RunInvoiceSync(null);

        
        var badRequestResult = actionResult.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var returnedResult = badRequestResult.Value.Should().BeAssignableTo<SyncResultDto>().Subject;
        returnedResult.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task RunCustomerSync_ReturnsOkWhenSuccessful()
    {
        
        var result = new SyncResultDto
        {
            IsSuccess = true,
            SyncType = "CUSTOMER",
            SuccessfulRecords = 12
        };
        _mockSyncService.Setup(s => s.SyncCustomersAsync(null)).ReturnsAsync(result);

        
        var actionResult = await _controller.RunCustomerSync(null);

        
        var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedResult = okResult.Value.Should().BeAssignableTo<SyncResultDto>().Subject;
        returnedResult.IsSuccess.Should().BeTrue();
        returnedResult.SyncType.Should().Be("CUSTOMER");
    }

    [Fact]
    public async Task RunCustomerSync_ReturnsBadRequestWhenFailed()
    {
        
        var result = new SyncResultDto
        {
            IsSuccess = false,
            SyncType = "CUSTOMER",
            FailedRecords = 3
        };
        _mockSyncService.Setup(s => s.SyncCustomersAsync(null)).ReturnsAsync(result);

        
        var actionResult = await _controller.RunCustomerSync(null);

        
        var badRequestResult = actionResult.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var returnedResult = badRequestResult.Value.Should().BeAssignableTo<SyncResultDto>().Subject;
        returnedResult.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task GetSyncStatus_ReturnsOkWithStatusList()
    {
        
        var statusList = new List<SyncStatusDto>
        {
            new() { SyncType = "STOCK", IsRunning = false, PendingRecords = 5 },
            new() { SyncType = "INVOICE", IsRunning = true, PendingRecords = 10 }
        };
        _mockSyncService.Setup(s => s.GetSyncStatusAsync()).ReturnsAsync(statusList);

        
        var actionResult = await _controller.GetSyncStatus();

        
        var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedStatus = okResult.Value.Should().BeAssignableTo<List<SyncStatusDto>>().Subject;
        returnedStatus.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetSyncTypeStatus_ReturnsOkWithStatus()
    {
        
        _mockSyncService.Setup(s => s.IsSyncRunningAsync("STOCK")).ReturnsAsync(true);

        
        var actionResult = await _controller.GetSyncTypeStatus("stock");

        
        var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task StartSync_ReturnsOkForStockSync()
    {
        
        var request = new StartSyncRequest { SyncType = "STOCK" };
        var result = new SyncResultDto
        {
            IsSuccess = true,
            SyncType = "STOCK",
            Message = "Stock sync completed"
        };
        _mockSyncService.Setup(s => s.SyncStockAsync(null)).ReturnsAsync(result);

        
        var actionResult = await _controller.StartSync(request);

        
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
        _mockAuditService.Verify(a => a.LogSync(
            "STOCK",
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task StartSync_ReturnsOkForInvoiceSync()
    {
        
        var request = new StartSyncRequest { SyncType = "INVOICE" };
        var result = new SyncResultDto
        {
            IsSuccess = true,
            SyncType = "INVOICE",
            Message = "Invoice sync completed"
        };
        _mockSyncService.Setup(s => s.SyncInvoicesAsync(null)).ReturnsAsync(result);

        
        var actionResult = await _controller.StartSync(request);

        
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task StartSync_ReturnsOkForCustomerSync()
    {
        
        var request = new StartSyncRequest { SyncType = "CUSTOMER" };
        var result = new SyncResultDto
        {
            IsSuccess = true,
            SyncType = "CUSTOMER",
            Message = "Customer sync completed"
        };
        _mockSyncService.Setup(s => s.SyncCustomersAsync(null)).ReturnsAsync(result);

        
        var actionResult = await _controller.StartSync(request);

        
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task StartSync_ReturnsInternalServerErrorWhenExceptionOccurs()
    {
        
        var request = new StartSyncRequest { SyncType = "STOCK" };
        _mockSyncService.Setup(s => s.SyncStockAsync(null))
            .ThrowsAsync(new Exception("Sync failed"));

        
        var actionResult = await _controller.StartSync(request);

        
        var statusCodeResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
    }
}
