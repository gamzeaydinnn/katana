using Katana.Core.DTOs;
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

    public InvoicesController(IInvoiceService invoiceService, ILogger<InvoicesController> logger)
    {
        _invoiceService = invoiceService;
        _logger = logger;
    }

    /// <summary>
    /// Tüm faturaları getirir
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var invoices = await _invoiceService.GetAllInvoicesAsync();
        return Ok(new { data = invoices, count = invoices.Count });
    }

    /// <summary>
    /// ID'ye göre fatura getirir
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
        if (invoice == null)
            return NotFound(new { error = $"Invoice with ID {id} not found" });

        return Ok(invoice);
    }

    /// <summary>
    /// Fatura numarasına göre fatura getirir
    /// </summary>
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

    /// <summary>
    /// Müşteriye göre faturaları getirir
    /// </summary>
    [HttpGet("customer/{customerId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByCustomer(int customerId)
    {
        var invoices = await _invoiceService.GetInvoicesByCustomerIdAsync(customerId);
        return Ok(new { data = invoices, count = invoices.Count });
    }

    /// <summary>
    /// Duruma göre faturaları getirir
    /// </summary>
    [HttpGet("status/{status}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByStatus(string status)
    {
        var invoices = await _invoiceService.GetInvoicesByStatusAsync(status);
        return Ok(new { data = invoices, count = invoices.Count });
    }

    /// <summary>
    /// Tarih aralığına göre faturaları getirir
    /// </summary>
    [HttpGet("range")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByDateRange([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        var invoices = await _invoiceService.GetInvoicesByDateRangeAsync(startDate, endDate);
        return Ok(new { data = invoices, count = invoices.Count });
    }

    /// <summary>
    /// Vadesi geçmiş faturaları getirir
    /// </summary>
    [HttpGet("overdue")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOverdue()
    {
        var invoices = await _invoiceService.GetOverdueInvoicesAsync();
        return Ok(new { data = invoices, count = invoices.Count });
    }

    /// <summary>
    /// Senkronize edilmemiş faturaları getirir
    /// </summary>
    [HttpGet("unsynced")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnsynced()
    {
        var invoices = await _invoiceService.GetUnsyncedInvoicesAsync();
        return Ok(new { data = invoices, count = invoices.Count });
    }

    /// <summary>
    /// Yeni fatura oluşturur
    /// </summary>
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
            var invoice = await _invoiceService.CreateInvoiceAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, invoice);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid invoice data");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating invoice");
            return StatusCode(500, new { error = "Failed to create invoice" });
        }
    }

    /// <summary>
    /// Fatura günceller
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateInvoiceDto dto)
    {
        try
        {
            var invoice = await _invoiceService.UpdateInvoiceAsync(id, dto);
            if (invoice == null)
                return NotFound(new { error = $"Invoice with ID {id} not found" });

            return Ok(invoice);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating invoice {Id}", id);
            return StatusCode(500, new { error = "Failed to update invoice" });
        }
    }

    /// <summary>
    /// Fatura durumunu günceller
    /// </summary>
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
            var result = await _invoiceService.UpdateInvoiceStatusAsync(id, dto);
            if (!result)
                return NotFound(new { error = $"Invoice with ID {id} not found" });

            return Ok(new { message = "Invoice status updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating invoice {Id} status", id);
            return StatusCode(500, new { error = "Failed to update status" });
        }
    }

    /// <summary>
    /// Fatura siler
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var result = await _invoiceService.DeleteInvoiceAsync(id);
            if (!result)
                return NotFound(new { error = $"Invoice with ID {id} not found" });

            return Ok(new { message = "Invoice deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting invoice {Id}", id);
            return StatusCode(500, new { error = "Failed to delete invoice" });
        }
    }

    /// <summary>
    /// Faturayı senkronize edildi olarak işaretler
    /// </summary>
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

    /// <summary>
    /// Fatura istatistiklerini getirir
    /// </summary>
    [HttpGet("statistics")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatistics()
    {
        var statistics = await _invoiceService.GetInvoiceStatisticsAsync();
        return Ok(statistics);
    }

    /// <summary>
    /// Tarih aralığına göre fatura istatistiklerini getirir
    /// </summary>
    [HttpGet("statistics/range")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatisticsByDateRange([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        var statistics = await _invoiceService.GetInvoiceStatisticsByDateRangeAsync(startDate, endDate);
        return Ok(statistics);
    }
}
