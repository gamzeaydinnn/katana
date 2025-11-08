using FluentAssertions;
using Katana.API.Controllers;
using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Katana.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Katana.Tests.Controllers;

public class InvoicesControllerTests
{
    private readonly Mock<IInvoiceService> _mockInvoiceService;
    private readonly Mock<ILogger<InvoicesController>> _mockLogger;
    private readonly Mock<ILoggingService> _mockLoggingService;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly InvoicesController _controller;

    public InvoicesControllerTests()
    {
        _mockInvoiceService = new Mock<IInvoiceService>();
        _mockLogger = new Mock<ILogger<InvoicesController>>();
        _mockLoggingService = new Mock<ILoggingService>();
        _mockAuditService = new Mock<IAuditService>();
        
        _controller = new InvoicesController(
            _mockInvoiceService.Object,
            _mockLogger.Object,
            _mockLoggingService.Object,
            _mockAuditService.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithInvoices()
    {
        // Arrange
        var invoices = new List<InvoiceSummaryDto>
        {
            new() { Id = 1, InvoiceNo = "INV001", CustomerName = "Customer 1", TotalAmount = 1000, Status = "Draft", InvoiceDate = DateTime.Now },
            new() { Id = 2, InvoiceNo = "INV002", CustomerName = "Customer 2", TotalAmount = 2000, Status = "Paid", InvoiceDate = DateTime.Now }
        };
        _mockInvoiceService.Setup(s => s.GetAllInvoicesAsync()).ReturnsAsync(invoices);

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<object>().Subject;
        response.GetType().GetProperty("count")?.GetValue(response).Should().Be(2);
    }

    [Fact]
    public async Task GetById_ReturnsOkWhenInvoiceExists()
    {
        // Arrange
        var invoice = new InvoiceDto 
        { 
            Id = 1, 
            InvoiceNo = "INV001", 
            CustomerId = 1,
            CustomerName = "Test Customer",
            TotalAmount = 1000, 
            Status = "Draft" 
        };
        _mockInvoiceService.Setup(s => s.GetInvoiceByIdAsync(1)).ReturnsAsync(invoice);

        // Act
        var result = await _controller.GetById(1);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value.Should().BeOfType<InvoiceDto>().Subject;
        data.Id.Should().Be(1);
        data.InvoiceNo.Should().Be("INV001");
    }

    [Fact]
    public async Task GetById_ReturnsNotFoundWhenInvoiceDoesNotExist()
    {
        // Arrange
        _mockInvoiceService.Setup(s => s.GetInvoiceByIdAsync(99)).ReturnsAsync((InvoiceDto?)null);

        // Act
        var result = await _controller.GetById(99);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByNumber_ReturnsOkWhenInvoiceExists()
    {
        // Arrange
        var invoice = new InvoiceDto { Id = 1, InvoiceNo = "INV001", TotalAmount = 1000 };
        _mockInvoiceService.Setup(s => s.GetInvoiceByNumberAsync("INV001")).ReturnsAsync(invoice);

        // Act
        var result = await _controller.GetByNumber("INV001");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value.Should().BeOfType<InvoiceDto>().Subject;
        data.InvoiceNo.Should().Be("INV001");
    }

    [Fact]
    public async Task GetByCustomer_ReturnsOkWithInvoices()
    {
        // Arrange
        var invoices = new List<InvoiceSummaryDto>
        {
            new() { Id = 1, InvoiceNo = "INV001", CustomerName = "Customer 1", TotalAmount = 1000, Status = "Draft", InvoiceDate = DateTime.Now }
        };
        _mockInvoiceService.Setup(s => s.GetInvoicesByCustomerIdAsync(1)).ReturnsAsync(invoices);

        // Act
        var result = await _controller.GetByCustomer(1);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetOverdue_ReturnsOkWithOverdueInvoices()
    {
        // Arrange
        var overdueInvoices = new List<InvoiceSummaryDto>
        {
            new() { Id = 1, InvoiceNo = "INV001", CustomerName = "Customer 1", DueDate = DateTime.Now.AddDays(-5), Status = "Sent", TotalAmount = 1000, InvoiceDate = DateTime.Now }
        };
        _mockInvoiceService.Setup(s => s.GetOverdueInvoicesAsync()).ReturnsAsync(overdueInvoices);

        // Act
        var result = await _controller.GetOverdue();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<object>().Subject;
        response.GetType().GetProperty("count")?.GetValue(response).Should().Be(1);
    }

    [Fact]
    public async Task Create_ReturnsCreatedWhenValid()
    {
        // Arrange
        var createDto = new CreateInvoiceDto
        {
            InvoiceNo = "INV001",
            CustomerId = 1,
            InvoiceDate = DateTime.Now,
            Items = new List<CreateInvoiceItemDto>
            {
                new() { ProductId = 1, Quantity = 2, UnitPrice = 100, TaxRate = 0.18m }
            }
        };
        var invoice = new InvoiceDto
        {
            Id = 1,
            InvoiceNo = "INV001",
            CustomerId = 1,
            TotalAmount = 236,
            Status = "Draft"
        };
        _mockInvoiceService.Setup(s => s.CreateInvoiceAsync(createDto)).ReturnsAsync(invoice);

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var data = createdResult.Value.Should().BeOfType<InvoiceDto>().Subject;
        data.InvoiceNo.Should().Be("INV001");
        _mockAuditService.Verify(a => a.LogCreate(
            "Invoice",
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task UpdateStatus_ReturnsOkWhenSuccessful()
    {
        // Arrange
        var statusDto = new UpdateInvoiceStatusDto { Status = "Draft" }; // Valid status
        _mockInvoiceService.Setup(s => s.UpdateInvoiceStatusAsync(1, statusDto)).ReturnsAsync(true);

        // Act
        var result = await _controller.UpdateStatus(1, statusDto);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
        _mockAuditService.Verify(a => a.LogUpdate(
            "Invoice",
            "1",
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task UpdateStatus_ReturnsNotFoundWhenInvoiceDoesNotExist()
    {
        // Arrange
        var statusDto = new UpdateInvoiceStatusDto { Status = "Draft" }; // Valid status
        _mockInvoiceService.Setup(s => s.UpdateInvoiceStatusAsync(99, statusDto)).ReturnsAsync(false);

        // Act
        var result = await _controller.UpdateStatus(99, statusDto);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Delete_ReturnsOkWhenSuccessful()
    {
        // Arrange
        _mockInvoiceService.Setup(s => s.DeleteInvoiceAsync(1)).ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(1);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
        _mockAuditService.Verify(a => a.LogDelete(
            "Invoice",
            "1",
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task GetStatistics_ReturnsOkWithStats()
    {
        // Arrange
        var stats = new InvoiceStatisticsDto
        {
            TotalInvoices = 100,
            TotalAmount = 50000,
            PaidAmount = 30000,
            UnpaidAmount = 20000,
            PaidCount = 60,
            OverdueCount = 10
        };
        _mockInvoiceService.Setup(s => s.GetInvoiceStatisticsAsync()).ReturnsAsync(stats);

        // Act
        var result = await _controller.GetStatistics();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value.Should().BeOfType<InvoiceStatisticsDto>().Subject;
        data.TotalInvoices.Should().Be(100);
        data.TotalAmount.Should().Be(50000);
    }

    [Fact]
    public async Task MarkAsSynced_ReturnsOkWhenSuccessful()
    {
        // Arrange
        _mockInvoiceService.Setup(s => s.MarkAsSyncedAsync(1)).ReturnsAsync(true);

        // Act
        var result = await _controller.MarkAsSynced(1);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }
}
