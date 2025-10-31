using Katana.Data.Context;
using Katana.Core.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Katana.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class NotificationsController : ControllerBase
{
    private readonly IntegrationDbContext _db;

    public NotificationsController(IntegrationDbContext db)
    {
        _db = db;
    }

    // GET: /api/notifications?unread=true
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool? unread)
    {
        var q = _db.Notifications.AsQueryable();
        if (unread.HasValue)
        {
            if (unread.Value) q = q.Where(n => n.IsRead == false);
            else q = q.Where(n => n.IsRead == true);
        }
        var list = await q.OrderByDescending(n => n.CreatedAt).Take(200).ToListAsync();
        return Ok(list);
    }

    // GET: /api/notifications/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var n = await _db.Notifications.FindAsync(id);
        if (n == null) return NotFound();
        return Ok(n);
    }

    // POST: /api/notifications/{id}/mark-read
    [HttpPost("{id}/mark-read")]
    public async Task<IActionResult> MarkRead(int id)
    {
        var n = await _db.Notifications.FindAsync(id);
        if (n == null) return NotFound();
        n.IsRead = true;
        await _db.SaveChangesAsync();
        return Ok(n);
    }

    // DELETE: /api/notifications/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var n = await _db.Notifications.FindAsync(id);
        if (n == null) return NotFound();
        _db.Notifications.Remove(n);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
