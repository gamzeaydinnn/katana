using FluentAssertions;
using Katana.API.Controllers;
using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Katana.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Katana.Tests.Controllers;

public class CustomersControllerTests
{
    private readonly Mock<ICustomerService> _mockCustomerService;
    private readonly Mock<ILoggingService> _mockLoggingService;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly CustomersController _controller;

    public CustomersControllerTests()
    {
        _mockCustomerService = new Mock<ICustomerService>();
        _mockLoggingService = new Mock<ILoggingService>();
        _mockAuditService = new Mock<IAuditService>();
        
        _controller = new CustomersController(
            _mockCustomerService.Object,
            _mockLoggingService.Object,
            _mockAuditService.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithCustomers()
    {
        // Arrange
        var customers = new List<CustomerDto>
        {
            new() { Id = 1, TaxNo = "1234567890", Title = "Customer 1", IsActive = true },
            new() { Id = 2, TaxNo = "0987654321", Title = "Customer 2", IsActive = true }
        };
        _mockCustomerService.Setup(s => s.GetAllCustomersAsync()).ReturnsAsync(customers);

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value.Should().BeAssignableTo<IEnumerable<CustomerDto>>().Subject;
        data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetById_ReturnsOkWhenCustomerExists()
    {
        // Arrange
        var customer = new CustomerDto 
        { 
            Id = 1, 
            TaxNo = "1234567890", 
            Title = "Test Customer",
            Phone = "5551234567",
            Email = "test@test.com"
        };
        _mockCustomerService.Setup(s => s.GetCustomerByIdAsync(1)).ReturnsAsync(customer);

        // Act
        var result = await _controller.GetById(1);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value.Should().BeOfType<CustomerDto>().Subject;
        data.Id.Should().Be(1);
        data.Title.Should().Be("Test Customer");
    }

    [Fact]
    public async Task GetById_ReturnsNotFoundWhenCustomerDoesNotExist()
    {
        // Arrange
        _mockCustomerService.Setup(s => s.GetCustomerByIdAsync(99)).ReturnsAsync((CustomerDto?)null);

        // Act
        var result = await _controller.GetById(99);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetByTaxNo_ReturnsOkWhenCustomerExists()
    {
        // Arrange
        var customer = new CustomerDto { Id = 1, TaxNo = "1234567890", Title = "Test Customer" };
        _mockCustomerService.Setup(s => s.GetCustomerByTaxNoAsync("1234567890")).ReturnsAsync(customer);

        // Act
        var result = await _controller.GetByTaxNo("1234567890");

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value.Should().BeOfType<CustomerDto>().Subject;
        data.TaxNo.Should().Be("1234567890");
    }

    [Fact]
    public async Task Search_ReturnsBadRequestWhenQueryIsEmpty()
    {
        // Act
        var result = await _controller.Search(string.Empty);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Search_ReturnsOkWithMatchingCustomers()
    {
        // Arrange
        var customers = new List<CustomerDto>
        {
            new() { Id = 1, TaxNo = "1234567890", Title = "Test Customer" }
        };
        _mockCustomerService.Setup(s => s.SearchCustomersAsync("Test")).ReturnsAsync(customers);

        // Act
        var result = await _controller.Search("Test");

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value.Should().BeAssignableTo<IEnumerable<CustomerDto>>().Subject;
        data.Should().HaveCount(1);
    }

    [Fact]
    public async Task Create_ReturnsCreatedWhenValid()
    {
        // Arrange
        var createDto = new CreateCustomerDto 
        { 
            TaxNo = "1234567890", 
            Title = "New Customer",
            Phone = "5551234567",
            Email = "new@customer.com"
        };
        var customer = new CustomerDto 
        { 
            Id = 1, 
            TaxNo = "1234567890", 
            Title = "New Customer",
            IsActive = true
        };
        _mockCustomerService.Setup(s => s.CreateCustomerAsync(createDto)).ReturnsAsync(customer);

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var data = createdResult.Value.Should().BeOfType<CustomerDto>().Subject;
        data.TaxNo.Should().Be("1234567890");
        _mockAuditService.Verify(a => a.LogCreate(
            "Customer",
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Update_ReturnsOkWhenValid()
    {
        // Arrange
        var updateDto = new UpdateCustomerDto 
        { 
            TaxNo = "1234567890", 
            Title = "Updated Customer",
            IsActive = true
        };
        var customer = new CustomerDto 
        { 
            Id = 1, 
            TaxNo = "1234567890", 
            Title = "Updated Customer",
            IsActive = true
        };
        _mockCustomerService.Setup(s => s.UpdateCustomerAsync(1, updateDto)).ReturnsAsync(customer);

        // Act
        var result = await _controller.Update(1, updateDto);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value.Should().BeOfType<CustomerDto>().Subject;
        data.Title.Should().Be("Updated Customer");
        _mockAuditService.Verify(a => a.LogUpdate(
            "Customer",
            "1",
            It.IsAny<string>(),
            null,
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Update_ReturnsNotFoundWhenCustomerDoesNotExist()
    {
        // Arrange
        var updateDto = new UpdateCustomerDto { TaxNo = "9999999999", Title = "Unknown", IsActive = true };
        _mockCustomerService.Setup(s => s.UpdateCustomerAsync(99, updateDto))
            .ThrowsAsync(new KeyNotFoundException("Customer not found"));

        // Act
        var result = await _controller.Update(99, updateDto);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Delete_ReturnsNoContentWhenSuccessful()
    {
        // Arrange
        _mockCustomerService.Setup(s => s.DeleteCustomerAsync(1)).ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(1);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _mockAuditService.Verify(a => a.LogDelete(
            "Customer",
            "1",
            It.IsAny<string>(),
            null), Times.Once);
    }

    [Fact]
    public async Task Activate_ReturnsNoContentWhenSuccessful()
    {
        // Arrange
        _mockCustomerService.Setup(s => s.ActivateCustomerAsync(1)).ReturnsAsync(true);

        // Act
        var result = await _controller.Activate(1);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Deactivate_ReturnsNoContentWhenSuccessful()
    {
        // Arrange
        _mockCustomerService.Setup(s => s.DeactivateCustomerAsync(1)).ReturnsAsync(true);

        // Act
        var result = await _controller.Deactivate(1);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task GetStatistics_ReturnsOkWithStats()
    {
        // Arrange
        var stats = new CustomerStatisticsDto
        {
            TotalCustomers = 100,
            ActiveCustomers = 85,
            InactiveCustomers = 15,
            TotalBalance = 50000,
            TotalCreditLimit = 100000
        };
        _mockCustomerService.Setup(s => s.GetCustomerStatisticsAsync()).ReturnsAsync(stats);

        // Act
        var result = await _controller.GetStatistics();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value.Should().BeOfType<CustomerStatisticsDto>().Subject;
        data.TotalCustomers.Should().Be(100);
        data.ActiveCustomers.Should().Be(85);
    }

    [Fact]
    public async Task GetBalance_ReturnsOkWithBalance()
    {
        // Arrange
        _mockCustomerService.Setup(s => s.GetCustomerBalanceAsync(1)).ReturnsAsync(5000.00m);

        // Act
        var result = await _controller.GetBalance(1);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(5000.00m);
    }
}
