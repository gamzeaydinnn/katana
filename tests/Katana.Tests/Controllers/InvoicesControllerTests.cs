using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Katana.API.Controllers;
using Katana.Business.Interfaces;
using Katana.Core.Interfaces;
using Katana.Core.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

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
            _mockAuditService.Object
        );
    }

    [Fact]
    public async Task GetAll_ReturnsOkResult_WithInvoices()
    {
        // Arrange
        var invoices = new List<InvoiceDto>
        {
            new InvoiceDto { Id = 1, InvoiceNo = "INV-001", TotalAmount = 1000 },
            new InvoiceDto { Id = 2, InvoiceNo = "INV-002", TotalAmount = 2000 }
        };
        
        _mockInvoiceService
            .Setup(s => s.GetAllInvoicesAsync())
            .ReturnsAsync(invoices);

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        dynamic value = okResult.Value;
        Assert.Equal(2, value.count);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenInvoiceDoesNotExist()
    {
        // Arrange
        _mockInvoiceService
            .Setup(s => s.GetInvoiceByIdAsync(999))
            .ReturnsAsync((InvoiceDto)null);

        // Act
        var result = await _controller.GetById(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetById_ReturnsOkResult_WithInvoice()
    {
        // Arrange
        var invoice = new InvoiceDto 
        { 
            Id = 1, 
            InvoiceNo = "INV-001", 
            TotalAmount = 1000 
        };
        
        _mockInvoiceService
            .Setup(s => s.GetInvoiceByIdAsync(1))
            .ReturnsAsync(invoice);

        // Act
        var result = await _controller.GetById(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedInvoice = Assert.IsType<InvoiceDto>(okResult.Value);
        Assert.Equal("INV-001", returnedInvoice.InvoiceNo);
    }

    [Fact]
    public async Task Create_ReturnsCreatedResult_WithValidData()
    {
        // Arrange
        var createDto = new CreateInvoiceDto
        {
            CustomerId = 1,
            InvoiceDate = System.DateTime.UtcNow,
            DueDate = System.DateTime.UtcNow.AddDays(30),
            Items = new List<CreateInvoiceItemDto>
            {
                new CreateInvoiceItemDto
                {
                    ProductId = 1,
                    Quantity = 10,
                    UnitPrice = 100
                }
            }
        };

        var createdInvoice = new InvoiceDto
        {
            Id = 1,
            InvoiceNo = "INV-001",
            CustomerId = 1,
            TotalAmount = 1000
        };

        _mockInvoiceService
            .Setup(s => s.CreateInvoiceAsync(createDto))
            .ReturnsAsync(createdInvoice);

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var invoice = Assert.IsType<InvoiceDto>(createdResult.Value);
        Assert.Equal("INV-001", invoice.InvoiceNo);
    }

    [Fact]
    public async Task Update_ReturnsOkResult_WhenInvoiceExists()
    {
        // Arrange
        var updateDto = new UpdateInvoiceDto
        {
            DueDate = System.DateTime.UtcNow.AddDays(45)
        };

        var updatedInvoice = new InvoiceDto
        {
            Id = 1,
            InvoiceNo = "INV-001",
            TotalAmount = 1000
        };

        _mockInvoiceService
            .Setup(s => s.UpdateInvoiceAsync(1, updateDto))
            .ReturnsAsync(updatedInvoice);

        // Act
        var result = await _controller.Update(1, updateDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task Update_ReturnsNotFound_WhenInvoiceDoesNotExist()
    {
        // Arrange
        var updateDto = new UpdateInvoiceDto();

        _mockInvoiceService
            .Setup(s => s.UpdateInvoiceAsync(999, updateDto))
            .ReturnsAsync((InvoiceDto)null);

        // Act
        var result = await _controller.Update(999, updateDto);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Delete_ReturnsOk_WhenInvoiceExists()
    {
        // Arrange
        _mockInvoiceService
            .Setup(s => s.DeleteInvoiceAsync(1))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(1);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenInvoiceDoesNotExist()
    {
        // Arrange
        _mockInvoiceService
            .Setup(s => s.DeleteInvoiceAsync(999))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task UpdateStatus_ReturnsOk_WhenStatusIsValid()
    {
        // Arrange
        var statusDto = new UpdateInvoiceStatusDto
        {
            Status = "Paid"
        };

        _mockInvoiceService
            .Setup(s => s.UpdateInvoiceStatusAsync(1, statusDto))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.UpdateStatus(1, statusDto);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetByCustomer_ReturnsInvoices_ForCustomer()
    {
        // Arrange
        var invoices = new List<InvoiceDto>
        {
            new InvoiceDto { Id = 1, InvoiceNo = "INV-001", CustomerId = 1 },
            new InvoiceDto { Id = 2, InvoiceNo = "INV-002", CustomerId = 1 }
        };

        _mockInvoiceService
            .Setup(s => s.GetInvoicesByCustomerIdAsync(1))
            .ReturnsAsync(invoices);

        // Act
        var result = await _controller.GetByCustomer(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        dynamic value = okResult.Value;
        Assert.Equal(2, value.count);
    }
}
