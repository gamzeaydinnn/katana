using System.Collections.Generic;
using System.Threading.Tasks;
using Katana.Core.Entities;
using Katana.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Katana.API.Controllers.Admin;

[ApiController]
[Route("api/admin/tax-rate-mappings")]
[Authorize(Roles = "Admin,Manager")]
public class TaxRateMappingController : ControllerBase
{
    private readonly ITaxRateMappingService _taxRateMappingService;
    
    public TaxRateMappingController(ITaxRateMappingService taxRateMappingService)
    {
        _taxRateMappingService = taxRateMappingService;
    }
    
    /// <summary>
    /// Gets all tax rate mappings
    /// </summary>
    [HttpGet("mappings")]
    public async Task<ActionResult<List<TaxRateMapping>>> GetAllMappings()
    {
        var mappings = await _taxRateMappingService.GetAllMappingsAsync();
        return Ok(mappings);
    }
    
    /// <summary>
    /// Gets tax rate mapping by Katana tax_rate_id
    /// </summary>
    [HttpGet("by-tax-rate-id/{taxRateId}")]
    public async Task<ActionResult<TaxRateMapping>> GetMappingByTaxRateId(long taxRateId)
    {
        var mapping = await _taxRateMappingService.GetMappingByTaxRateIdAsync(taxRateId);
        
        if (mapping == null)
        {
            return NotFound(new { message = $"No mapping found for tax_rate_id {taxRateId}" });
        }
        
        return Ok(mapping);
    }
    
    /// <summary>
    /// Gets Koza KDV oranı by Katana tax_rate_id
    /// </summary>
    [HttpGet("{taxRateId}/kdv-oran")]
    public async Task<ActionResult<decimal>> GetKdvOranByTaxRateId(long taxRateId, [FromQuery] decimal defaultRate = 0.20m)
    {
        var kdvOran = await _taxRateMappingService.GetKdvOranByTaxRateIdAsync(taxRateId, defaultRate);
        return Ok(new { taxRateId, kdvOran, isDefault = kdvOran == defaultRate });
    }
    
    /// <summary>
    /// Creates or updates tax rate mapping
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<TaxRateMapping>> CreateOrUpdateMapping([FromBody] TaxRateMappingRequest request)
    {
        if (request.KozaKdvOran < 0 || request.KozaKdvOran > 1)
        {
            return BadRequest(new { message = "KDV oranı 0 ile 1 arasında olmalıdır (örn: 0.18 = %18)" });
        }
        
        var mapping = await _taxRateMappingService.CreateOrUpdateMappingAsync(
            request.KatanaTaxRateId,
            request.KozaKdvOran,
            request.Description);
        
        return Ok(mapping);
    }
    
    /// <summary>
    /// Gets dictionary of tax_rate_id → kdvOran for bulk operations
    /// </summary>
    [HttpGet("dictionary")]
    public async Task<ActionResult<Dictionary<long, decimal>>> GetTaxRateDictionary()
    {
        var dictionary = await _taxRateMappingService.GetTaxRateToKdvOranMapAsync();
        return Ok(dictionary);
    }
    
    /// <summary>
    /// Deletes tax rate mapping
    /// </summary>
    [HttpDelete("{taxRateId}")]
    public async Task<ActionResult> DeleteMapping(long taxRateId)
    {
        var deleted = await _taxRateMappingService.DeleteMappingAsync(taxRateId);
        
        if (!deleted)
        {
            return NotFound(new { message = $"No mapping found for tax_rate_id {taxRateId}" });
        }
        
        return NoContent();
    }
}

public class TaxRateMappingRequest
{
    public long KatanaTaxRateId { get; set; }
    public decimal KozaKdvOran { get; set; }
    public string? Description { get; set; }
}
