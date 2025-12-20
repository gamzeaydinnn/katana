using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Katana.Core.Enums;
using Katana.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Katana.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly ILoggingService _loggingService;
    private readonly IAuditService _auditService;
    private readonly ILucaService _lucaService;

    public CustomersController(ICustomerService customerService, ILoggingService loggingService, IAuditService auditService, ILucaService lucaService)
    {
        _customerService = customerService;
        _loggingService = loggingService;
        _auditService = auditService;
        _lucaService = lucaService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CustomerDto>>> GetAll()
    {
        _loggingService.LogInfo("Customers listed", User?.Identity?.Name, null, LogCategory.UserAction);
        var customers = await _customerService.GetAllCustomersAsync();
        return Ok(customers);
    }

    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<CustomerSummaryDto>>> GetActive()
    {
        var customers = await _customerService.GetActiveCustomersAsync();
        return Ok(customers);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CustomerDto>> GetById(int id)
    {
        var customer = await _customerService.GetCustomerByIdAsync(id);
        if (customer == null)
            return NotFound($"Müşteri bulunamadı: {id}");

        return Ok(customer);
    }

    [HttpGet("by-taxno/{taxNo}")]
    public async Task<ActionResult<CustomerDto>> GetByTaxNo(string taxNo)
    {
        var customer = await _customerService.GetCustomerByTaxNoAsync(taxNo);
        if (customer == null)
            return NotFound($"Vergi numaralı müşteri bulunamadı: {taxNo}");

        return Ok(customer);
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<CustomerDto>>> Search([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest("Arama terimi boş olamaz");

        var customers = await _customerService.SearchCustomersAsync(q);
        return Ok(customers);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CustomerDto>> Create([FromBody] CreateCustomerDto dto)
    {
        var validationErrors = Katana.Business.Validators.CustomerValidator.ValidateCreate(dto);
        if (validationErrors.Any())
            return BadRequest(new { errors = validationErrors });

        try
        {
            var customer = await _customerService.CreateCustomerAsync(dto);
            _auditService.LogCreate("Customer", customer.Id.ToString(), User?.Identity?.Name ?? "system", 
                $"Title: {customer.Title}, TaxNo: {customer.TaxNo}");
            _loggingService.LogInfo($"Customer created: {customer.Title}", User?.Identity?.Name, null, LogCategory.UserAction);
            
            // Luca'ya cari kart olarak gönder
            try
            {
                var customerEntity = await _customerService.GetCustomerEntityByIdAsync(customer.Id);
                if (customerEntity != null)
                {
                    var lucaResult = await _lucaService.UpsertCariCardAsync(customerEntity);
                    if (lucaResult.IsSuccess)
                    {
                        _loggingService.LogInfo($"Customer {customer.Id} synced to Luca: {lucaResult.Message}", 
                            User?.Identity?.Name, null, LogCategory.Business);
                    }
                    else
                    {
                        _loggingService.LogWarning($"Luca sync warning for customer {customer.Id}: {lucaResult.Message}", 
                            User?.Identity?.Name, null, LogCategory.Business);
                    }
                }
            }
            catch (Exception lucaEx)
            {
                // Luca hatası müşteri oluşturmayı engellemez
                _loggingService.LogError($"Luca sync failed for customer {customer.Id}", lucaEx, 
                    User?.Identity?.Name, null, LogCategory.Business);
            }
            
            return CreatedAtAction(nameof(GetById), new { id = customer.Id }, customer);
        }
        catch (InvalidOperationException ex)
        {
            _loggingService.LogError("Customer creation failed", ex, User?.Identity?.Name, null, LogCategory.Business);
            return Conflict(ex.Message);
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CustomerDto>> Update(int id, [FromBody] UpdateCustomerDto dto)
    {
        var validationErrors = Katana.Business.Validators.CustomerValidator.ValidateUpdate(dto);
        if (validationErrors.Any())
            return BadRequest(new { errors = validationErrors });

        try
        {
            var customer = await _customerService.UpdateCustomerAsync(id, dto);
            _auditService.LogUpdate("Customer", id.ToString(), User?.Identity?.Name ?? "system", null, 
                $"Updated: {customer.Title}");
            _loggingService.LogInfo($"Customer updated: {id}", User?.Identity?.Name, null, LogCategory.UserAction);
            
            // Luca'ya cari kart güncelle (UPSERT - varsa duplicate, yoksa ekle)
            try
            {
                var customerEntity = await _customerService.GetCustomerEntityByIdAsync(id);
                if (customerEntity != null)
                {
                    var lucaResult = await _lucaService.UpsertCariCardAsync(customerEntity);
                    if (lucaResult.IsSuccess)
                    {
                        _loggingService.LogInfo($"Customer {id} synced to Luca: {lucaResult.Message}", 
                            User?.Identity?.Name, null, LogCategory.Business);
                    }
                    else
                    {
                        _loggingService.LogWarning($"Luca sync warning for customer {id}: {lucaResult.Message}", 
                            User?.Identity?.Name, null, LogCategory.Business);
                    }
                }
            }
            catch (Exception lucaEx)
            {
                // Luca hatası müşteri güncellemeyi engellemez
                _loggingService.LogError($"Luca sync failed for customer {id}", lucaEx, 
                    User?.Identity?.Name, null, LogCategory.Business);
            }
            
            return Ok(customer);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _loggingService.LogError("Customer update failed", ex, User?.Identity?.Name, null, LogCategory.Business);
            return Conflict(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            var result = await _customerService.DeleteCustomerAsync(id);
            if (!result)
                return NotFound($"Müşteri bulunamadı: {id}");

            _auditService.LogDelete("Customer", id.ToString(), User?.Identity?.Name ?? "system", null);
            _loggingService.LogInfo($"Customer deleted: {id}", User?.Identity?.Name, null, LogCategory.UserAction);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _loggingService.LogError("Customer deletion failed", ex, User?.Identity?.Name, null, LogCategory.Business);
            return Conflict(ex.Message);
        }
    }

    [HttpPut("{id}/activate")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Activate(int id)
    {
        var result = await _customerService.ActivateCustomerAsync(id);
        if (!result)
            return NotFound($"Müşteri bulunamadı: {id}");

        return NoContent();
    }

    [HttpPut("{id}/deactivate")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Deactivate(int id)
    {
        var result = await _customerService.DeactivateCustomerAsync(id);
        if (!result)
            return NotFound($"Müşteri bulunamadı: {id}");

        return NoContent();
    }

    [HttpGet("statistics")]
    public async Task<ActionResult<CustomerStatisticsDto>> GetStatistics()
    {
        var stats = await _customerService.GetCustomerStatisticsAsync();
        return Ok(stats);
    }

    [HttpGet("{id}/balance")]
    public async Task<ActionResult<decimal>> GetBalance(int id)
    {
        var balance = await _customerService.GetCustomerBalanceAsync(id);
        return Ok(balance);
    }

    /// <summary>
    /// Manuel olarak müşteriyi Luca'ya senkronize eder
    /// </summary>
    [HttpPost("{id}/sync")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SyncResultDto>> SyncToLuca(int id)
    {
        try
        {
            var customer = await _customerService.GetCustomerEntityByIdAsync(id);
            if (customer == null)
                return NotFound($"Müşteri bulunamadı: {id}");

            var result = await _lucaService.UpsertCariCardAsync(customer);
            
            // LastSyncError güncelle
            await _customerService.UpdateLastSyncErrorAsync(id, 
                result.IsSuccess ? null : result.Message,
                result.IsSuccess && result.Details.Count > 0 ? 
                    long.TryParse(result.Details[0].Replace("finansalNesneId=", ""), out var finId) ? finId : null 
                    : null);
            
            _loggingService.LogInfo($"Customer {id} manual sync to Luca: {result.Message}", 
                User?.Identity?.Name, null, LogCategory.Business);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _loggingService.LogError($"Manual Luca sync failed for customer {id}", ex, 
                User?.Identity?.Name, null, LogCategory.Business);
            
            // Hata kaydı
            await _customerService.UpdateLastSyncErrorAsync(id, ex.Message, null);
            
            return StatusCode(500, new SyncResultDto 
            { 
                IsSuccess = false, 
                Message = ex.Message,
                SyncType = "MANUAL_CARI_SYNC"
            });
        }
    }

    /// <summary>
    /// Müşterinin Luca senkronizasyon durumunu döner
    /// </summary>
    [HttpGet("{id}/luca-info")]
    public async Task<ActionResult<CustomerLucaSyncInfo>> GetLucaInfo(int id)
    {
        var customer = await _customerService.GetCustomerEntityByIdAsync(id);
        if (customer == null)
            return NotFound($"Müşteri bulunamadı: {id}");

        return Ok(new CustomerLucaSyncInfo
        {
            LucaCode = customer.LucaCode ?? customer.GenerateLucaCode(),
            LucaFinansalNesneId = customer.LucaFinansalNesneId,
            LastSyncError = customer.LastSyncError,
            LastSyncAt = customer.UpdatedAt
        });
    }
}
