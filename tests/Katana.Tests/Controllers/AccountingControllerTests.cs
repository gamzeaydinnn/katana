using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Katana.API.Controllers;
using Katana.Core.Interfaces;
using Katana.Core.DTOs;

namespace Katana.Tests.Controllers;

public class AccountingControllerTests
{
    private readonly Mock<IAccountingService> _mockAccountingService;
    private readonly AccountingController _controller;

    public AccountingControllerTests()
    {
        _mockAccountingService = new Mock<IAccountingService>();
        _controller = new AccountingController(_mockAccountingService.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithRecords()
    {
        
        var records = new List<AccountingRecordDto>
        {
            new() { Id = 1, TransactionNo = "TXN001", Type = "INCOME", Amount = 1000 },
            new() { Id = 2, TransactionNo = "TXN002", Type = "EXPENSE", Amount = 500 }
        };
        _mockAccountingService.Setup(s => s.GetAllRecordsAsync()).ReturnsAsync(records);

        
        var result = await _controller.GetAll();

        
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedRecords = okResult.Value.Should().BeAssignableTo<List<AccountingRecordDto>>().Subject;
        returnedRecords.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetById_ReturnsOkWhenRecordExists()
    {
        
        var record = new AccountingRecordDto { Id = 1, TransactionNo = "TXN001", Type = "INCOME" };
        _mockAccountingService.Setup(s => s.GetRecordByIdAsync(1)).ReturnsAsync(record);

        
        var result = await _controller.GetById(1);

        
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedRecord = okResult.Value.Should().BeAssignableTo<AccountingRecordDto>().Subject;
        returnedRecord.Id.Should().Be(1);
    }

    [Fact]
    public async Task GetById_ReturnsNotFoundWhenRecordDoesNotExist()
    {
        
        _mockAccountingService.Setup(s => s.GetRecordByIdAsync(99)).ReturnsAsync((AccountingRecordDto?)null);

        
        var result = await _controller.GetById(99);

        
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetByTransactionNo_ReturnsOkWhenExists()
    {
        
        var record = new AccountingRecordDto { Id = 1, TransactionNo = "TXN001" };
        _mockAccountingService.Setup(s => s.GetRecordByTransactionNoAsync("TXN001")).ReturnsAsync(record);

        
        var result = await _controller.GetByTransactionNo("TXN001");

        
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedRecord = okResult.Value.Should().BeAssignableTo<AccountingRecordDto>().Subject;
        returnedRecord.TransactionNo.Should().Be("TXN001");
    }

    [Fact]
    public async Task GetByType_ReturnsOkForIncome()
    {
        
        var records = new List<AccountingRecordDto>
        {
            new() { Id = 1, Type = "INCOME", Amount = 1000 }
        };
        _mockAccountingService.Setup(s => s.GetRecordsByTypeAsync("INCOME")).ReturnsAsync(records);

        
        var result = await _controller.GetByType("INCOME");

        
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByType_ReturnsBadRequestForInvalidType()
    {
        
        var result = await _controller.GetByType("INVALID");

        
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetByCategory_ReturnsOkWithRecords()
    {
        
        var records = new List<AccountingRecordDto>
        {
            new() { Id = 1, Category = "Sales" }
        };
        _mockAccountingService.Setup(s => s.GetRecordsByCategoryAsync("Sales")).ReturnsAsync(records);

        
        var result = await _controller.GetByCategory("Sales");

        
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByCustomer_ReturnsOkWithRecords()
    {
        
        var records = new List<AccountingRecordDto>
        {
            new() { Id = 1, CustomerId = 1 }
        };
        _mockAccountingService.Setup(s => s.GetRecordsByCustomerAsync(1)).ReturnsAsync(records);

        
        var result = await _controller.GetByCustomer(1);

        
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByInvoice_ReturnsOkWithRecords()
    {
        
        var records = new List<AccountingRecordDto>
        {
            new() { Id = 1, InvoiceId = 1 }
        };
        _mockAccountingService.Setup(s => s.GetRecordsByInvoiceAsync(1)).ReturnsAsync(records);

        
        var result = await _controller.GetByInvoice(1);

        
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetUnsynced_ReturnsOkWithRecords()
    {
        
        var records = new List<AccountingRecordDto>
        {
            new() { Id = 1, IsSynced = false }
        };
        _mockAccountingService.Setup(s => s.GetUnsyncedRecordsAsync()).ReturnsAsync(records);

        
        var result = await _controller.GetUnsynced();

        
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_ReturnsCreatedWhenValid()
    {
        
        var createDto = new CreateAccountingRecordDto 
        { 
            Type = "INCOME",
            Category = "Sales",
            Amount = 1000,
            TransactionDate = DateTime.UtcNow
        };
        var createdRecord = new AccountingRecordDto 
        { 
            Id = 1, 
            TransactionNo = "TXN001",
            Type = "INCOME",
            Amount = 1000
        };
        _mockAccountingService.Setup(s => s.CreateRecordAsync(createDto)).ReturnsAsync(createdRecord);

        
        var result = await _controller.Create(createDto);

        
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var returnedRecord = createdResult.Value.Should().BeAssignableTo<AccountingRecordDto>().Subject;
        returnedRecord.TransactionNo.Should().Be("TXN001");
    }

    [Fact]
    public async Task Delete_ReturnsNoContentWhenSuccessful()
    {
        
        _mockAccountingService.Setup(s => s.DeleteRecordAsync(1)).ReturnsAsync(true);

        
        var result = await _controller.Delete(1);

        
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_ReturnsNotFoundWhenRecordDoesNotExist()
    {
        
        _mockAccountingService.Setup(s => s.DeleteRecordAsync(99)).ReturnsAsync(false);

        
        var result = await _controller.Delete(99);

        
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task MarkAsSynced_ReturnsNoContentWhenSuccessful()
    {
        
        _mockAccountingService.Setup(s => s.MarkAsSyncedAsync(1)).ReturnsAsync(true);

        
        var result = await _controller.MarkAsSynced(1);

        
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task GetStatistics_ReturnsOkWithStats()
    {
        
        var stats = new AccountingStatisticsDto 
        { 
            TotalIncome = 10000,
            TotalExpense = 5000
        };
        _mockAccountingService.Setup(s => s.GetStatisticsAsync()).ReturnsAsync(stats);

        
        var result = await _controller.GetStatistics();

        
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedStats = okResult.Value.Should().BeAssignableTo<AccountingStatisticsDto>().Subject;
        returnedStats.TotalIncome.Should().Be(10000);
    }
}
