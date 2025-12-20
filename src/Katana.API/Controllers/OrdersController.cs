using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Katana.Core.Enums;
using Katana.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Katana.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ILoggingService _loggingService;
    private readonly IAuditService _auditService;

    public OrdersController(IOrderService orderService, ILoggingService loggingService, IAuditService auditService)
    {
        _orderService = orderService;
        _loggingService = loggingService;
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            _loggingService.LogInfo("Orders listed", User?.Identity?.Name, null, LogCategory.UserAction);
            var orders = await _orderService.GetAllAsync();
            return Ok(orders);
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Orders list failed", ex, User?.Identity?.Name, null, LogCategory.Business);
            return StatusCode(500, new { message = "Siparişler yüklenirken hata oluştu", error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var order = await _orderService.GetByIdAsync(id);
        return order == null ? NotFound() : Ok(order);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateOrderDto dto)
    {
        try
        {
            var order = await _orderService.CreateAsync(dto);
            _auditService.LogCreate("Order", order.Id.ToString(), User?.Identity?.Name ?? "system", 
                $"Customer: {order.CustomerId}, Items: {order.Items?.Count ?? 0}");
            _loggingService.LogInfo($"Order created: {order.Id}", User?.Identity?.Name, null, LogCategory.UserAction);
            return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Order creation failed", ex, User?.Identity?.Name, null, LogCategory.Business);
            return StatusCode(500, "Order creation failed");
        }
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] OrderStatus status)
    {
        var success = await _orderService.UpdateStatusAsync(id, status);
        if (success)
        {
            _auditService.LogUpdate("Order", id.ToString(), User?.Identity?.Name ?? "system", null, 
                $"Status: {status}");
            _loggingService.LogInfo($"Order status updated: {id}", User?.Identity?.Name, null, LogCategory.UserAction);
        }
        return success ? NoContent() : NotFound();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await _orderService.DeleteAsync(id);
        if (success)
        {
            _auditService.LogDelete("Order", id.ToString(), User?.Identity?.Name ?? "system", null);
            _loggingService.LogInfo($"Order deleted: {id}", User?.Identity?.Name, null, LogCategory.UserAction);
        }
        return success ? NoContent() : NotFound();
    }
}
