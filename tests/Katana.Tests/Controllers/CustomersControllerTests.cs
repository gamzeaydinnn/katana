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
        
        var customers = new List<CustomerDto>
        {
            new() { Id = 1, TaxNo = "1234567890", Title = "Customer 1", IsActive = true },
            new() { Id = 2, TaxNo = "0987654321", Title = "Customer 2", IsActive = true }
        };
        _mockCustomerService.Setup(s => s.GetAllCustomersAsync()).ReturnsAsync(customers);

        
        var result = await _controller.GetAll();

        
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value.Should().BeAssignableTo<IEnumerable<CustomerDto>>().Subject;
        data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetById_ReturnsOkWhenCustomerExists()
    {
        
        var customer = new CustomerDto 
        { 
            Id = 1, 
            TaxNo = "1234567890", 
            Title = "Test Customer",
            Phone = "5551234567",
            Email = "test@test.com"
        };
        _mockCustomerService.Setup(s => s.GetCustomerByIdAsync(1)).ReturnsAsync(customer);

        
        var result = await _controller.GetById(1);

        
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value.Should().BeOfType<CustomerDto>().Subject;
        data.Id.Should().Be(1);
        data.Title.Should().Be("Test Customer");
    }

    [Fact]
    public async Task GetById_ReturnsNotFoundWhenCustomerDoesNotExist()
    {
        
        _mockCustomerService.Setup(s => s.GetCustomerByIdAsync(99)).ReturnsAsync((CustomerDto?)null);

        
        var result = await _controller.GetById(99);

        
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetByTaxNo_ReturnsOkWhenCustomerExists()
    {
        
        var customer = new CustomerDto { Id = 1, TaxNo = "1234567890", Title = "Test Customer" };
        _mockCustomerService.Setup(s => s.GetCustomerByTaxNoAsync("1234567890")).ReturnsAsync(customer);

        
        var result = await _controller.GetByTaxNo("1234567890");

        
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value.Should().BeOfType<CustomerDto>().Subject;
        data.TaxNo.Should().Be("1234567890");
    }

    [Fact]
    public async Task Search_ReturnsBadRequestWhenQueryIsEmpty()
    {
        
        var result = await _controller.Search(string.Empty);

        
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Search_ReturnsOkWithMatchingCustomers()
    {
        
        var customers = new List<CustomerDto>
        {
            new() { Id = 1, TaxNo = "1234567890", Title = "Test Customer" }
        };
        _mockCustomerService.Setup(s => s.SearchCustomersAsync("Test")).ReturnsAsync(customers);

        
        var result = await _controller.Search("Test");

        
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value.Should().BeAssignableTo<IEnumerable<CustomerDto>>().Subject;
        data.Should().HaveCount(1);
    }

    [Fact]
    public async Task Create_ReturnsCreatedWhenValid()
    {
        
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

        
        var result = await _controller.Create(createDto);

        
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

        
        var result = await _controller.Update(1, updateDto);

        
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
        
        var updateDto = new UpdateCustomerDto { TaxNo = "9999999999", Title = "Unknown", IsActive = true };
        _mockCustomerService.Setup(s => s.UpdateCustomerAsync(99, updateDto))
            .ThrowsAsync(new KeyNotFoundException("Customer not found"));

        
        var result = await _controller.Update(99, updateDto);

        
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Delete_ReturnsNoContentWhenSuccessful()
    {
        
        _mockCustomerService.Setup(s => s.DeleteCustomerAsync(1)).ReturnsAsync(true);

        
        var result = await _controller.Delete(1);

        
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
        
        _mockCustomerService.Setup(s => s.ActivateCustomerAsync(1)).ReturnsAsync(true);

        
        var result = await _controller.Activate(1);

        
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Deactivate_ReturnsNoContentWhenSuccessful()
    {
        
        _mockCustomerService.Setup(s => s.DeactivateCustomerAsync(1)).ReturnsAsync(true);

        
        var result = await _controller.Deactivate(1);

        
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task GetStatistics_ReturnsOkWithStats()
    {
        
        var stats = new CustomerStatisticsDto
        {
            TotalCustomers = 100,
            ActiveCustomers = 85,
            InactiveCustomers = 15,
            TotalBalance = 50000,
            TotalCreditLimit = 100000
        };
        _mockCustomerService.Setup(s => s.GetCustomerStatisticsAsync()).ReturnsAsync(stats);

        
        var result = await _controller.GetStatistics();

        
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value.Should().BeOfType<CustomerStatisticsDto>().Subject;
        data.TotalCustomers.Should().Be(100);
        data.ActiveCustomers.Should().Be(85);
    }

    [Fact]
    public async Task GetBalance_ReturnsOkWithBalance()
    {
        
        _mockCustomerService.Setup(s => s.GetCustomerBalanceAsync(1)).ReturnsAsync(5000.00m);

        
        var result = await _controller.GetBalance(1);

        
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(5000.00m);
    }
}
