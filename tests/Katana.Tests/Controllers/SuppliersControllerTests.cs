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
        // Arrange
        var suppliers = new List<SupplierDto>
        {
            new() { Id = 1, Name = "Supplier A", Email = "supplierA@test.com", IsActive = true },
            new() { Id = 2, Name = "Supplier B", Email = "supplierB@test.com", IsActive = true }
        };
        _mockSupplierService.Setup(s => s.GetAllAsync()).ReturnsAsync(suppliers);

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedSuppliers = okResult.Value.Should().BeAssignableTo<List<SupplierDto>>().Subject;
        returnedSuppliers.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetById_ReturnsOkWhenSupplierExists()
    {
        // Arrange
        var supplier = new SupplierDto { Id = 1, Name = "Supplier A", Email = "supplierA@test.com" };
        _mockSupplierService.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(supplier);

        // Act
        var result = await _controller.GetById(1);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedSupplier = okResult.Value.Should().BeAssignableTo<SupplierDto>().Subject;
        returnedSupplier.Id.Should().Be(1);
    }

    [Fact]
    public async Task GetById_ReturnsNotFoundWhenSupplierDoesNotExist()
    {
        // Arrange
        _mockSupplierService.Setup(s => s.GetByIdAsync(99)).ReturnsAsync((SupplierDto?)null);

        // Act
        var result = await _controller.GetById(99);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Create_ReturnsCreatedWhenValid()
    {
        // Arrange
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

        // Act
        var result = await _controller.Create(createDto);

        // Assert
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
        // Arrange
        var createDto = new CreateSupplierDto { Name = "", Email = "test@test.com" };

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_ReturnsBadRequestWhenEmailIsInvalid()
    {
        // Arrange
        var createDto = new CreateSupplierDto { Name = "Test Supplier", Email = "invalid-email" };

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Update_ReturnsOkWhenValid()
    {
        // Arrange
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

        // Act
        var result = await _controller.Update(1, updateDto);

        // Assert
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
        // Arrange
        var updateDto = new UpdateSupplierDto { Name = "Updated Supplier" };
        _mockSupplierService.Setup(s => s.UpdateAsync(99, updateDto))
            .ThrowsAsync(new KeyNotFoundException("Supplier not found"));

        // Act
        var result = await _controller.Update(99, updateDto);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Update_ReturnsBadRequestWhenNameIsEmpty()
    {
        // Arrange
        var updateDto = new UpdateSupplierDto { Name = "", Email = "test@test.com" };

        // Act
        var result = await _controller.Update(1, updateDto);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Delete_ReturnsNoContentWhenSuccessful()
    {
        // Arrange
        _mockSupplierService.Setup(s => s.DeleteAsync(1)).ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(1);

        // Assert
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
        // Arrange
        _mockSupplierService.Setup(s => s.DeleteAsync(99)).ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(99);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Delete_ReturnsConflictWhenSupplierHasRelatedData()
    {
        // Arrange
        _mockSupplierService.Setup(s => s.DeleteAsync(1))
            .ThrowsAsync(new InvalidOperationException("Cannot delete supplier with existing products"));

        // Act
        var result = await _controller.Delete(1);

        // Assert
        var conflictResult = result.Should().BeOfType<ConflictObjectResult>().Subject;
        conflictResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Activate_ReturnsNoContentWhenSuccessful()
    {
        // Arrange
        _mockSupplierService.Setup(s => s.ActivateAsync(1)).ReturnsAsync(true);

        // Act
        var result = await _controller.Activate(1);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Activate_ReturnsNotFoundWhenSupplierDoesNotExist()
    {
        // Arrange
        _mockSupplierService.Setup(s => s.ActivateAsync(99)).ReturnsAsync(false);

        // Act
        var result = await _controller.Activate(99);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Deactivate_ReturnsNoContentWhenSuccessful()
    {
        // Arrange
        _mockSupplierService.Setup(s => s.DeactivateAsync(1)).ReturnsAsync(true);

        // Act
        var result = await _controller.Deactivate(1);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Deactivate_ReturnsNotFoundWhenSupplierDoesNotExist()
    {
        // Arrange
        _mockSupplierService.Setup(s => s.DeactivateAsync(99)).ReturnsAsync(false);

        // Act
        var result = await _controller.Deactivate(99);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }
}
