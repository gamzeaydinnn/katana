using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Katana.API.Controllers;
using Katana.Core.Interfaces;
using Katana.Core.DTOs;
using Katana.Business.Interfaces;

namespace Katana.Tests.Controllers;

public class UsersControllerTests
{
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<ILoggingService> _mockLoggingService;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _mockUserService = new Mock<IUserService>();
        _mockLoggingService = new Mock<ILoggingService>();
        _mockAuditService = new Mock<IAuditService>();
        _controller = new UsersController(
            _mockUserService.Object,
            _mockLoggingService.Object,
            _mockAuditService.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithUsers()
    {
        
        var users = new List<UserDto>
        {
            new() { Id = 1, Username = "admin", Role = "Admin", Email = "admin@test.com", IsActive = true },
            new() { Id = 2, Username = "user", Role = "Staff", Email = "user@test.com", IsActive = true }
        };
        _mockUserService.Setup(s => s.GetAllAsync()).ReturnsAsync(users);

        
        var result = await _controller.GetAll();

        
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedUsers = okResult.Value.Should().BeAssignableTo<List<UserDto>>().Subject;
        returnedUsers.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetById_ReturnsOkWhenUserExists()
    {
        
        var user = new UserDto { Id = 1, Username = "admin", Role = "Admin", IsActive = true };
        _mockUserService.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(user);

        
        var result = await _controller.GetById(1);

        
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedUser = okResult.Value.Should().BeAssignableTo<UserDto>().Subject;
        returnedUser.Id.Should().Be(1);
    }

    [Fact]
    public async Task GetById_ReturnsNotFoundWhenUserDoesNotExist()
    {
        
        _mockUserService.Setup(s => s.GetByIdAsync(99)).ReturnsAsync((UserDto?)null);

        
        var result = await _controller.GetById(99);

        
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_ReturnsCreatedWhenValid()
    {
        
        var createDto = new CreateUserDto 
        { 
            Username = "newuser", 
            Password = "password123",
            Role = "Staff",
            Email = "newuser@test.com"
        };
        var createdUser = new UserDto 
        { 
            Id = 1, 
            Username = "newuser", 
            Role = "Staff",
            Email = "newuser@test.com",
            IsActive = true
        };
        _mockUserService.Setup(s => s.CreateAsync(createDto)).ReturnsAsync(createdUser);

        
        var result = await _controller.Create(createDto);

        
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var returnedUser = createdResult.Value.Should().BeAssignableTo<UserDto>().Subject;
        returnedUser.Username.Should().Be("newuser");
        _mockAuditService.Verify(a => a.LogCreate(
            "User",
            "1",
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Delete_ReturnsNoContentWhenSuccessful()
    {
        
        _mockUserService.Setup(s => s.DeleteAsync(1)).ReturnsAsync(true);

        
        var result = await _controller.Delete(1);

        
        result.Should().BeOfType<NoContentResult>();
        _mockAuditService.Verify(a => a.LogDelete(
            "User",
            "1",
            It.IsAny<string>(),
            null), Times.Once);
    }

    [Fact]
    public async Task Delete_ReturnsNotFoundWhenUserDoesNotExist()
    {
        
        _mockUserService.Setup(s => s.DeleteAsync(99)).ReturnsAsync(false);

        
        var result = await _controller.Delete(99);

        
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateRole_ReturnsNoContentWhenSuccessful()
    {
        
        _mockUserService.Setup(s => s.UpdateRoleAsync(1, "Manager")).ReturnsAsync(true);

        
        var result = await _controller.UpdateRole(1, "Manager");

        
        result.Should().BeOfType<NoContentResult>();
        _mockAuditService.Verify(a => a.LogUpdate(
            "User",
            "1",
            It.IsAny<string>(),
            null,
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task UpdateRole_ReturnsNotFoundWhenUserDoesNotExist()
    {
        
        _mockUserService.Setup(s => s.UpdateRoleAsync(99, "Manager")).ReturnsAsync(false);

        
        var result = await _controller.UpdateRole(99, "Manager");

        
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateRole_ReturnsBadRequestWhenRoleIsEmpty()
    {
        
        var result = await _controller.UpdateRole(1, "");

        
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Update_ReturnsOkWhenValid()
    {
        
        var updateDto = new UpdateUserDto 
        { 
            Username = "updateduser", 
            Role = "Manager",
            IsActive = true,
            Email = "updated@test.com"
        };
        var updatedUser = new UserDto 
        { 
            Id = 1, 
            Username = "updateduser", 
            Role = "Manager",
            IsActive = true,
            Email = "updated@test.com"
        };
        _mockUserService.Setup(s => s.UpdateAsync(1, updateDto)).ReturnsAsync(updatedUser);

        
        var result = await _controller.Update(1, updateDto);

        
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedUser = okResult.Value.Should().BeAssignableTo<UserDto>().Subject;
        returnedUser.Username.Should().Be("updateduser");
        _mockAuditService.Verify(a => a.LogUpdate(
            "User",
            "1",
            It.IsAny<string>(),
            null,
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Update_ReturnsNotFoundWhenUserDoesNotExist()
    {
        
        var updateDto = new UpdateUserDto { Username = "updated", Role = "Staff" };
        _mockUserService.Setup(s => s.UpdateAsync(99, updateDto))
            .ThrowsAsync(new KeyNotFoundException("User not found"));

        
        var result = await _controller.Update(99, updateDto);

        
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Update_ReturnsConflictWhenDuplicateUsername()
    {
        
        var updateDto = new UpdateUserDto { Username = "duplicate", Role = "Staff" };
        _mockUserService.Setup(s => s.UpdateAsync(1, updateDto))
            .ThrowsAsync(new InvalidOperationException("Username already exists"));

        
        var result = await _controller.Update(1, updateDto);

        
        var conflictResult = result.Should().BeOfType<ConflictObjectResult>().Subject;
        conflictResult.Value.Should().NotBeNull();
    }
}
