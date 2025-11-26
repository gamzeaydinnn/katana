using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Katana.Core.Enums;
using Katana.Core.Interfaces;
using Katana.Business.Validators;
using Microsoft.AspNetCore.Mvc;

namespace Katana.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;
    private readonly ILogger<InvoicesController> _logger;
    private readonly ILoggingService _loggingService;
    private readonly IAuditService _auditService;

    public InvoicesController(IInvoiceService invoiceService, ILogger<InvoicesController> logger,
        ILoggingService loggingService, IAuditService auditService)
    {
        _invoiceService = invoiceService;
        _logger = logger;
        _loggingService = loggingService;
        _auditService = auditService;
    }

    
    
    
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            _loggingService.LogInfo("Retrieving all invoices", User?.Identity?.Name, null, LogCategory.UserAction);
            var invoices = await _invoiceService.GetAllInvoicesAsync();
            return Ok(new { data = invoices, count = invoices.Count });
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error retrieving invoices", ex, User?.Identity?.Name, null, LogCategory.Business);
            throw;
        }
    }

    
    
    
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            _loggingService.LogInfo($"Retrieving invoice {id}", User?.Identity?.Name, $"InvoiceId: {id}", LogCategory.UserAction);
            var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
            if (invoice == null)
            {
                _loggingService.LogWarning($"Invoice {id} not found", User?.Identity?.Name, null, LogCategory.Business);
                return NotFound(new { error = $"Invoice with ID {id} not found" });
            }

            return Ok(invoice);
        }
        catch (Exception ex)
        {
            _loggingService.LogError($"Error retrieving invoice {id}", ex, User?.Identity?.Name, null, LogCategory.Business);
            throw;
        }
    }

    
    
    
    [HttpGet("by-number/{invoiceNo}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByNumber(string invoiceNo)
    {
        var invoice = await _invoiceService.GetInvoiceByNumberAsync(invoiceNo);
        if (invoice == null)
            return NotFound(new { error = $"Invoice {invoiceNo} not found" });

        return Ok(invoice);
    }

    
    
    
    [HttpGet("customer/{customerId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByCustomer(int customerId)
    {
        var invoices = await _invoiceService.GetInvoicesByCustomerIdAsync(customerId);
        return Ok(new { data = invoices, count = invoices.Count });
    }

    
    
    
    [HttpGet("status/{status}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByStatus(string status)
    {
        var invoices = await _invoiceService.GetInvoicesByStatusAsync(status);
        return Ok(new { data = invoices, count = invoices.Count });
    }

    
    
    
    [HttpGet("range")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByDateRange([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        var invoices = await _invoiceService.GetInvoicesByDateRangeAsync(startDate, endDate);
        return Ok(new { data = invoices, count = invoices.Count });
    }

    
    
    
    [HttpGet("overdue")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOverdue()
    {
        var invoices = await _invoiceService.GetOverdueInvoicesAsync();
        return Ok(new { data = invoices, count = invoices.Count });
    }

    
    
    
    [HttpGet("unsynced")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnsynced()
    {
        var invoices = await _invoiceService.GetUnsyncedInvoicesAsync();
        return Ok(new { data = invoices, count = invoices.Count });
    }

    
    
    
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateInvoiceDto dto)
    {
        var validation = InvoiceValidator.ValidateCreate(dto);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors });

        try
        {
            _loggingService.LogInfo("Creating new invoice", User?.Identity?.Name, $"Customer: {dto.CustomerId}", LogCategory.Business);
            var invoice = await _invoiceService.CreateInvoiceAsync(dto);
            
            _auditService.LogCreate("Invoice", invoice.Id.ToString(), User?.Identity?.Name ?? "System", 
                $"Invoice {invoice.InvoiceNo} created for customer {dto.CustomerId}");
            
            return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, invoice);
        }
        catch (ArgumentException ex)
        {
            _loggingService.LogWarning("Invalid invoice data", User?.Identity?.Name, ex.Message, LogCategory.Business);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error creating invoice", ex, User?.Identity?.Name, null, LogCategory.Business);
            return StatusCode(500, new { error = "Failed to create invoice" });
        }
    }

    
    
    
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateInvoiceDto dto)
    {
        try
        {
            _loggingService.LogInfo($"Updating invoice {id}", User?.Identity?.Name, $"InvoiceId: {id}", LogCategory.Business);
            var invoice = await _invoiceService.UpdateInvoiceAsync(id, dto);
            if (invoice == null)
            {
                _loggingService.LogWarning($"Invoice {id} not found for update", User?.Identity?.Name, null, LogCategory.Business);
                return NotFound(new { error = $"Invoice with ID {id} not found" });
            }

            _auditService.LogUpdate("Invoice", id.ToString(), User?.Identity?.Name ?? "System", 
                null, $"Invoice {id} updated");

            return Ok(invoice);
        }
        catch (Exception ex)
        {
            _loggingService.LogError($"Error updating invoice {id}", ex, User?.Identity?.Name, null, LogCategory.Business);
            return StatusCode(500, new { error = "Failed to update invoice" });
        }
    }

    
    
    
    [HttpPut("{id}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateInvoiceStatusDto dto)
    {
        var validation = InvoiceValidator.ValidateStatus(dto.Status);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors });

        try
        {
            _loggingService.LogInfo($"Updating invoice {id} status to {dto.Status}", User?.Identity?.Name, $"InvoiceId: {id}, Status: {dto.Status}", LogCategory.Business);
            var result = await _invoiceService.UpdateInvoiceStatusAsync(id, dto);
            if (!result)
            {
                _loggingService.LogWarning($"Invoice {id} not found for status update", User?.Identity?.Name, null, LogCategory.Business);
                return NotFound(new { error = $"Invoice with ID {id} not found" });
            }

            _auditService.LogUpdate("Invoice", id.ToString(), User?.Identity?.Name ?? "System", 
                $"Status: {dto.Status}", $"Invoice {id} status changed to {dto.Status}");

            return Ok(new { message = "Invoice status updated successfully" });
        }
        catch (Exception ex)
        {
            _loggingService.LogError($"Error updating invoice {id} status", ex, User?.Identity?.Name, null, LogCategory.Business);
            return StatusCode(500, new { error = "Failed to update status" });
        }
    }

    
    
    
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            _loggingService.LogInfo($"Deleting invoice {id}", User?.Identity?.Name, $"InvoiceId: {id}", LogCategory.Business);
            var result = await _invoiceService.DeleteInvoiceAsync(id);
            if (!result)
            {
                _loggingService.LogWarning($"Invoice {id} not found for deletion", User?.Identity?.Name, null, LogCategory.Business);
                return NotFound(new { error = $"Invoice with ID {id} not found" });
            }

            _auditService.LogDelete("Invoice", id.ToString(), User?.Identity?.Name ?? "System", 
                $"Invoice {id} deleted");

            return Ok(new { message = "Invoice deleted successfully" });
        }
        catch (Exception ex)
        {
            _loggingService.LogError($"Error deleting invoice {id}", ex, User?.Identity?.Name, null, LogCategory.Business);
            return StatusCode(500, new { error = "Failed to delete invoice" });
        }
    }

    
    
    
    [HttpPut("{id}/sync")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsSynced(int id)
    {
        try
        {
            var result = await _invoiceService.MarkAsSyncedAsync(id);
            if (!result)
                return NotFound(new { error = $"Invoice with ID {id} not found" });

            return Ok(new { message = "Invoice marked as synced" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking invoice {Id} as synced", id);
            return StatusCode(500, new { error = "Failed to mark as synced" });
        }
    }

    
    
    
    [HttpGet("statistics")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatistics()
    {
        var statistics = await _invoiceService.GetInvoiceStatisticsAsync();
        return Ok(statistics);
    }

    
    
    
    [HttpGet("statistics/range")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatisticsByDateRange([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        var statistics = await _invoiceService.GetInvoiceStatisticsByDateRangeAsync(startDate, endDate);
        return Ok(statistics);
    }
}
