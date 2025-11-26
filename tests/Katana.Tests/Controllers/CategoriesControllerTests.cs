using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Katana.API.Controllers;
using Katana.Core.Interfaces;
using Katana.Core.DTOs;
using Katana.Business.Interfaces;
using Microsoft.Extensions.Logging;

namespace Katana.Tests.Controllers;

public class CategoriesControllerTests
{
    private readonly Mock<ICategoryService> _mockCategoryService;
    private readonly Mock<ILogger<CategoriesController>> _mockLogger;
    private readonly Mock<ILoggingService> _mockLoggingService;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly CategoriesController _controller;

    public CategoriesControllerTests()
    {
        _mockCategoryService = new Mock<ICategoryService>();
        _mockLogger = new Mock<ILogger<CategoriesController>>();
        _mockLoggingService = new Mock<ILoggingService>();
        _mockAuditService = new Mock<IAuditService>();
        _controller = new CategoriesController(
            _mockCategoryService.Object,
            _mockLogger.Object,
            _mockLoggingService.Object,
            _mockAuditService.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithCategories()
    {
        
        var categories = new List<CategoryDto>
        {
            new() { Id = 1, Name = "Electronics", IsActive = true, ProductCount = 10 },
            new() { Id = 2, Name = "Clothing", IsActive = true, ProductCount = 25 }
        };
        _mockCategoryService.Setup(s => s.GetAllAsync()).ReturnsAsync(categories);

        
        var result = await _controller.GetAll();

        
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedCategories = okResult.Value.Should().BeAssignableTo<List<CategoryDto>>().Subject;
        returnedCategories.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetById_ReturnsOkWhenCategoryExists()
    {
        
        var category = new CategoryDto { Id = 1, Name = "Electronics", IsActive = true };
        _mockCategoryService.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(category);

        
        var result = await _controller.GetById(1);

        
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedCategory = okResult.Value.Should().BeAssignableTo<CategoryDto>().Subject;
        returnedCategory.Id.Should().Be(1);
    }

    [Fact]
    public async Task GetById_ReturnsNotFoundWhenCategoryDoesNotExist()
    {
        
        _mockCategoryService.Setup(s => s.GetByIdAsync(99)).ReturnsAsync((CategoryDto?)null);

        
        var result = await _controller.GetById(99);

        
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Create_ReturnsCreatedWhenValid()
    {
        
        var createDto = new CreateCategoryDto { Name = "New Category", IsActive = true };
        var createdCategory = new CategoryDto { Id = 1, Name = "New Category", IsActive = true };
        _mockCategoryService.Setup(s => s.CreateAsync(createDto)).ReturnsAsync(createdCategory);

        
        var result = await _controller.Create(createDto);

        
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var returnedCategory = createdResult.Value.Should().BeAssignableTo<CategoryDto>().Subject;
        returnedCategory.Name.Should().Be("New Category");
        _mockAuditService.Verify(a => a.LogCreate(
            "Category",
            "1",
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Create_ReturnsConflictWhenDuplicateName()
    {
        
        var createDto = new CreateCategoryDto { Name = "Duplicate Category" };
        _mockCategoryService.Setup(s => s.CreateAsync(createDto))
            .ThrowsAsync(new InvalidOperationException("Category with this name already exists"));

        
        var result = await _controller.Create(createDto);

        
        var conflictResult = result.Should().BeOfType<ConflictObjectResult>().Subject;
        conflictResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Update_ReturnsOkWhenValid()
    {
        
        var updateDto = new UpdateCategoryDto { Name = "Updated Category", IsActive = true };
        var updatedCategory = new CategoryDto { Id = 1, Name = "Updated Category", IsActive = true };
        _mockCategoryService.Setup(s => s.UpdateAsync(1, updateDto)).ReturnsAsync(updatedCategory);

        
        var result = await _controller.Update(1, updateDto);

        
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedCategory = okResult.Value.Should().BeAssignableTo<CategoryDto>().Subject;
        returnedCategory.Name.Should().Be("Updated Category");
        _mockAuditService.Verify(a => a.LogUpdate(
            "Category",
            "1",
            It.IsAny<string>(),
            null,
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Update_ReturnsNotFoundWhenCategoryDoesNotExist()
    {
        
        var updateDto = new UpdateCategoryDto { Name = "Updated Category" };
        _mockCategoryService.Setup(s => s.UpdateAsync(99, updateDto))
            .ThrowsAsync(new KeyNotFoundException("Category not found"));

        
        var result = await _controller.Update(99, updateDto);

        
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Update_ReturnsConflictWhenDuplicateName()
    {
        
        var updateDto = new UpdateCategoryDto { Name = "Duplicate Category" };
        _mockCategoryService.Setup(s => s.UpdateAsync(1, updateDto))
            .ThrowsAsync(new InvalidOperationException("Category with this name already exists"));

        
        var result = await _controller.Update(1, updateDto);

        
        var conflictResult = result.Should().BeOfType<ConflictObjectResult>().Subject;
        conflictResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Delete_ReturnsNoContentWhenSuccessful()
    {
        
        _mockCategoryService.Setup(s => s.DeleteAsync(1)).ReturnsAsync(true);

        
        var result = await _controller.Delete(1);

        
        result.Should().BeOfType<NoContentResult>();
        _mockAuditService.Verify(a => a.LogDelete(
            "Category",
            "1",
            It.IsAny<string>(),
            null), Times.Once);
    }

    [Fact]
    public async Task Delete_ReturnsNotFoundWhenCategoryDoesNotExist()
    {
        
        _mockCategoryService.Setup(s => s.DeleteAsync(99)).ReturnsAsync(false);

        
        var result = await _controller.Delete(99);

        
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Delete_ReturnsConflictWhenCategoryHasProducts()
    {
        
        _mockCategoryService.Setup(s => s.DeleteAsync(1))
            .ThrowsAsync(new InvalidOperationException("Cannot delete category with products"));

        
        var result = await _controller.Delete(1);

        
        var conflictResult = result.Should().BeOfType<ConflictObjectResult>().Subject;
        conflictResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Activate_ReturnsNoContentWhenSuccessful()
    {
        
        _mockCategoryService.Setup(s => s.ActivateAsync(1)).ReturnsAsync(true);

        
        var result = await _controller.Activate(1);

        
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Activate_ReturnsNotFoundWhenCategoryDoesNotExist()
    {
        
        _mockCategoryService.Setup(s => s.ActivateAsync(99)).ReturnsAsync(false);

        
        var result = await _controller.Activate(99);

        
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Deactivate_ReturnsNoContentWhenSuccessful()
    {
        
        _mockCategoryService.Setup(s => s.DeactivateAsync(1)).ReturnsAsync(true);

        
        var result = await _controller.Deactivate(1);

        
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Deactivate_ReturnsNotFoundWhenCategoryDoesNotExist()
    {
        
        _mockCategoryService.Setup(s => s.DeactivateAsync(99)).ReturnsAsync(false);

        
        var result = await _controller.Deactivate(99);

        
        result.Should().BeOfType<NotFoundResult>();
    }
}
