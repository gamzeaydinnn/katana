using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Katana.API.Controllers;
using Katana.Core.Interfaces;
using Katana.Core.DTOs;
using Katana.Business.Interfaces;

namespace Katana.Tests.Controllers;

public class SuppliersControllerTests
{
    private readonly Mock<ISupplierService> _mockSupplierService;
    private readonly Mock<ILoggingService> _mockLoggingService;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly SuppliersController _controller;

    public SuppliersControllerTests()
    {
        _mockSupplierService = new Mock<ISupplierService>();
        _mockLoggingService = new Mock<ILoggingService>();
        _mockAuditService = new Mock<IAuditService>();
        _controller = new SuppliersController(
            _mockSupplierService.Object,
            _mockLoggingService.Object,
            _mockAuditService.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithSuppliers()
    {
        
        var suppliers = new List<SupplierDto>
        {
            new() { Id = 1, Name = "Supplier A", Email = "supplierA@test.com", IsActive = true },
            new() { Id = 2, Name = "Supplier B", Email = "supplierB@test.com", IsActive = true }
        };
        _mockSupplierService.Setup(s => s.GetAllAsync()).ReturnsAsync(suppliers);

        
        var result = await _controller.GetAll();

        
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedSuppliers = okResult.Value.Should().BeAssignableTo<List<SupplierDto>>().Subject;
        returnedSuppliers.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetById_ReturnsOkWhenSupplierExists()
    {
        
        var supplier = new SupplierDto { Id = 1, Name = "Supplier A", Email = "supplierA@test.com" };
        _mockSupplierService.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(supplier);

        
        var result = await _controller.GetById(1);

        
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedSupplier = okResult.Value.Should().BeAssignableTo<SupplierDto>().Subject;
        returnedSupplier.Id.Should().Be(1);
    }

    [Fact]
    public async Task GetById_ReturnsNotFoundWhenSupplierDoesNotExist()
    {
        
        _mockSupplierService.Setup(s => s.GetByIdAsync(99)).ReturnsAsync((SupplierDto?)null);

        
        var result = await _controller.GetById(99);

        
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Create_ReturnsCreatedWhenValid()
    {
        
        var createDto = new CreateSupplierDto 
        { 
            Name = "New Supplier", 
            Email = "newsupplier@test.com",
            ContactName = "John Doe"
        };
        var createdSupplier = new SupplierDto 
        { 
            Id = 1, 
            Name = "New Supplier", 
            Email = "newsupplier@test.com" 
        };
        _mockSupplierService.Setup(s => s.CreateAsync(createDto)).ReturnsAsync(createdSupplier);

        
        var result = await _controller.Create(createDto);

        
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var returnedSupplier = createdResult.Value.Should().BeAssignableTo<SupplierDto>().Subject;
        returnedSupplier.Name.Should().Be("New Supplier");
        _mockAuditService.Verify(a => a.LogCreate(
            "Supplier",
            "1",
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Create_ReturnsBadRequestWhenNameIsEmpty()
    {
        
        var createDto = new CreateSupplierDto { Name = "", Email = "test@test.com" };

        
        var result = await _controller.Create(createDto);

        
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_ReturnsBadRequestWhenEmailIsInvalid()
    {
        
        var createDto = new CreateSupplierDto { Name = "Test Supplier", Email = "invalid-email" };

        
        var result = await _controller.Create(createDto);

        
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Update_ReturnsOkWhenValid()
    {
        
        var updateDto = new UpdateSupplierDto 
        { 
            Name = "Updated Supplier", 
            Email = "updated@test.com",
            IsActive = true 
        };
        var updatedSupplier = new SupplierDto 
        { 
            Id = 1, 
            Name = "Updated Supplier", 
            Email = "updated@test.com" 
        };
        _mockSupplierService.Setup(s => s.UpdateAsync(1, updateDto)).ReturnsAsync(updatedSupplier);

        
        var result = await _controller.Update(1, updateDto);

        
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedSupplier = okResult.Value.Should().BeAssignableTo<SupplierDto>().Subject;
        returnedSupplier.Name.Should().Be("Updated Supplier");
        _mockAuditService.Verify(a => a.LogUpdate(
            "Supplier",
            "1",
            It.IsAny<string>(),
            null,
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Update_ReturnsNotFoundWhenSupplierDoesNotExist()
    {
        
        var updateDto = new UpdateSupplierDto { Name = "Updated Supplier" };
        _mockSupplierService.Setup(s => s.UpdateAsync(99, updateDto))
            .ThrowsAsync(new KeyNotFoundException("Supplier not found"));

        
        var result = await _controller.Update(99, updateDto);

        
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Update_ReturnsBadRequestWhenNameIsEmpty()
    {
        
        var updateDto = new UpdateSupplierDto { Name = "", Email = "test@test.com" };

        
        var result = await _controller.Update(1, updateDto);

        
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Delete_ReturnsNoContentWhenSuccessful()
    {
        
        _mockSupplierService.Setup(s => s.DeleteAsync(1)).ReturnsAsync(true);

        
        var result = await _controller.Delete(1);

        
        result.Should().BeOfType<NoContentResult>();
        _mockAuditService.Verify(a => a.LogDelete(
            "Supplier",
            "1",
            It.IsAny<string>(),
            null), Times.Once);
    }

    [Fact]
    public async Task Delete_ReturnsNotFoundWhenSupplierDoesNotExist()
    {
        
        _mockSupplierService.Setup(s => s.DeleteAsync(99)).ReturnsAsync(false);

        
        var result = await _controller.Delete(99);

        
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Delete_ReturnsConflictWhenSupplierHasRelatedData()
    {
        
        _mockSupplierService.Setup(s => s.DeleteAsync(1))
            .ThrowsAsync(new InvalidOperationException("Cannot delete supplier with existing products"));

        
        var result = await _controller.Delete(1);

        
        var conflictResult = result.Should().BeOfType<ConflictObjectResult>().Subject;
        conflictResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Activate_ReturnsNoContentWhenSuccessful()
    {
        
        _mockSupplierService.Setup(s => s.ActivateAsync(1)).ReturnsAsync(true);

        
        var result = await _controller.Activate(1);

        
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Activate_ReturnsNotFoundWhenSupplierDoesNotExist()
    {
        
        _mockSupplierService.Setup(s => s.ActivateAsync(99)).ReturnsAsync(false);

        
        var result = await _controller.Activate(99);

        
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Deactivate_ReturnsNoContentWhenSuccessful()
    {
        
        _mockSupplierService.Setup(s => s.DeactivateAsync(1)).ReturnsAsync(true);

        
        var result = await _controller.Deactivate(1);

        
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Deactivate_ReturnsNotFoundWhenSupplierDoesNotExist()
    {
        
        _mockSupplierService.Setup(s => s.DeactivateAsync(99)).ReturnsAsync(false);

        
        var result = await _controller.Deactivate(99);

        
        result.Should().BeOfType<NotFoundResult>();
    }
}
