using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Katana.API.Controllers;
using Katana.Core.Entities;
using Katana.Data.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Katana.Tests.Controllers;

public class NotificationsControllerTests
{
    private static IntegrationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new IntegrationDbContext(options);
    }

    [Fact]
    public async Task List_ReturnsAll_WhenNoFilter()
    {
        
        await using var db = CreateContext();

        db.Notifications.AddRange(
            new Notification { Type = "Test", Title = "A", IsRead = false, CreatedAt = DateTime.UtcNow.AddMinutes(-2) },
            new Notification { Type = "Test", Title = "B", IsRead = true, CreatedAt = DateTime.UtcNow.AddMinutes(-1) }
        );
        await db.SaveChangesAsync();

        var controller = new NotificationsController(db);

        
        var result = await controller.List(null);

        
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var list = ok.Value.Should().BeAssignableTo<System.Collections.Generic.IEnumerable<Notification>>().Subject;
        list.Count().Should().Be(2);
    }

    [Fact]
    public async Task List_FiltersUnreadTrue()
    {
        
        await using var db = CreateContext();
        db.Notifications.AddRange(
            new Notification { Type = "X", IsRead = false },
            new Notification { Type = "X", IsRead = true }
        );
        await db.SaveChangesAsync();

        var controller = new NotificationsController(db);

        
        var result = await controller.List(true);

        
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var list = ok.Value.Should().BeAssignableTo<System.Collections.Generic.IEnumerable<Notification>>().Subject;
        list.Should().OnlyContain(n => n.IsRead == false);
    }

    [Fact]
    public async Task Get_ReturnsOk_WhenExists()
    {
        
        await using var db = CreateContext();
        var n = new Notification { Type = "Detail", Title = "Hello" };
        db.Notifications.Add(n);
        await db.SaveChangesAsync();

        var controller = new NotificationsController(db);

        
        var result = await controller.Get(n.Id);

        
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Get_ReturnsNotFound_WhenMissing()
    {
        
        await using var db = CreateContext();
        var controller = new NotificationsController(db);

        
        var result = await controller.Get(12345);

        
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task MarkRead_SetsIsReadTrue()
    {
        
        await using var db = CreateContext();
        var n = new Notification { Type = "Mark", IsRead = false };
        db.Notifications.Add(n);
        await db.SaveChangesAsync();

        var controller = new NotificationsController(db);

        
        var result = await controller.MarkRead(n.Id);

        
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var updated = await db.Notifications.FindAsync(n.Id);
        updated!.IsRead.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_RemovesNotification()
    {
        
        await using var db = CreateContext();
        var n = new Notification { Type = "DeleteMe" };
        db.Notifications.Add(n);
        await db.SaveChangesAsync();

        var controller = new NotificationsController(db);

        
        var result = await controller.Delete(n.Id);

        
        result.Should().BeOfType<NoContentResult>();
        (await db.Notifications.FindAsync(n.Id)).Should().BeNull();
    }
}

