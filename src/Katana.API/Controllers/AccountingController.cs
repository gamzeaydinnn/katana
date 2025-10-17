using Katana.Core.DTOs;
using Katana.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Katana.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AccountingController : ControllerBase
{
    private readonly IAccountingService _accountingService;

    public AccountingController(IAccountingService accountingService)
    {
        _accountingService = accountingService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AccountingRecordDto>>> GetAll()
    {
        var records = await _accountingService.GetAllRecordsAsync();
        return Ok(records);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AccountingRecordDto>> GetById(int id)
    {
        var record = await _accountingService.GetRecordByIdAsync(id);
        if (record == null)
            return NotFound($"Muhasebe kaydı bulunamadı: {id}");

        return Ok(record);
    }

    [HttpGet("by-transaction/{transactionNo}")]
    public async Task<ActionResult<AccountingRecordDto>> GetByTransactionNo(string transactionNo)
    {
        var record = await _accountingService.GetRecordByTransactionNoAsync(transactionNo);
        if (record == null)
            return NotFound($"İşlem numarası bulunamadı: {transactionNo}");

        return Ok(record);
    }

    [HttpGet("type/{type}")]
    public async Task<ActionResult<IEnumerable<AccountingRecordDto>>> GetByType(string type)
    {
        if (type.ToUpper() != "INCOME" && type.ToUpper() != "EXPENSE")
            return BadRequest("Tip INCOME veya EXPENSE olmalıdır");

        var records = await _accountingService.GetRecordsByTypeAsync(type);
        return Ok(records);
    }

    [HttpGet("category/{category}")]
    public async Task<ActionResult<IEnumerable<AccountingRecordDto>>> GetByCategory(string category)
    {
        var records = await _accountingService.GetRecordsByCategoryAsync(category);
        return Ok(records);
    }

    [HttpGet("customer/{customerId}")]
    public async Task<ActionResult<IEnumerable<AccountingRecordDto>>> GetByCustomer(int customerId)
    {
        var records = await _accountingService.GetRecordsByCustomerAsync(customerId);
        return Ok(records);
    }

    [HttpGet("invoice/{invoiceId}")]
    public async Task<ActionResult<IEnumerable<AccountingRecordDto>>> GetByInvoice(int invoiceId)
    {
        var records = await _accountingService.GetRecordsByInvoiceAsync(invoiceId);
        return Ok(records);
    }

    [HttpGet("range")]
    public async Task<ActionResult<IEnumerable<AccountingRecordDto>>> GetByDateRange(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        var validationErrors = Katana.Business.Validators.AccountingValidator.ValidateDateRange(startDate, endDate);
        if (validationErrors.Any())
            return BadRequest(new { errors = validationErrors });

        var records = await _accountingService.GetRecordsByDateRangeAsync(startDate, endDate);
        return Ok(records);
    }

    [HttpGet("unsynced")]
    public async Task<ActionResult<IEnumerable<AccountingRecordDto>>> GetUnsynced()
    {
        var records = await _accountingService.GetUnsyncedRecordsAsync();
        return Ok(records);
    }

    [HttpPost]
    public async Task<ActionResult<AccountingRecordDto>> Create([FromBody] CreateAccountingRecordDto dto)
    {
        var validationErrors = Katana.Business.Validators.AccountingValidator.ValidateCreate(dto);
        if (validationErrors.Any())
            return BadRequest(new { errors = validationErrors });

        var record = await _accountingService.CreateRecordAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = record.Id }, record);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<AccountingRecordDto>> Update(int id, [FromBody] UpdateAccountingRecordDto dto)
    {
        var validationErrors = Katana.Business.Validators.AccountingValidator.ValidateUpdate(dto);
        if (validationErrors.Any())
            return BadRequest(new { errors = validationErrors });

        try
        {
            var record = await _accountingService.UpdateRecordAsync(id, dto);
            return Ok(record);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var result = await _accountingService.DeleteRecordAsync(id);
        if (!result)
            return NotFound($"Muhasebe kaydı bulunamadı: {id}");

        return NoContent();
    }

    [HttpPut("{id}/sync")]
    public async Task<ActionResult> MarkAsSynced(int id)
    {
        var result = await _accountingService.MarkAsSyncedAsync(id);
        if (!result)
            return NotFound($"Muhasebe kaydı bulunamadı: {id}");

        return NoContent();
    }

    [HttpGet("statistics")]
    public async Task<ActionResult<AccountingStatisticsDto>> GetStatistics()
    {
        var stats = await _accountingService.GetStatisticsAsync();
        return Ok(stats);
    }

    [HttpGet("statistics/range")]
    public async Task<ActionResult<AccountingStatisticsDto>> GetStatisticsByRange(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        var validationErrors = Katana.Business.Validators.AccountingValidator.ValidateDateRange(startDate, endDate);
        if (validationErrors.Any())
            return BadRequest(new { errors = validationErrors });

        var stats = await _accountingService.GetStatisticsByDateRangeAsync(startDate, endDate);
        return Ok(stats);
    }

    [HttpGet("report")]
    public async Task<ActionResult<FinancialReportDto>> GetFinancialReport(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        var validationErrors = Katana.Business.Validators.AccountingValidator.ValidateDateRange(startDate, endDate);
        if (validationErrors.Any())
            return BadRequest(new { errors = validationErrors });

        var report = await _accountingService.GetFinancialReportAsync(startDate, endDate);
        return Ok(report);
    }
}
