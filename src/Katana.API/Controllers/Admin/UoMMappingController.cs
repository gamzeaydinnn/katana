using System.Collections.Generic;
using System.Threading.Tasks;
using Katana.Core.Entities;
using Katana.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Katana.API.Controllers.Admin;

[ApiController]
[Route("api/admin/uom-mappings")]
[Authorize(Roles = "Admin,Manager")]
public class UoMMappingController : ControllerBase
{
    private readonly IUoMMappingService _uomMappingService;
    
    public UoMMappingController(IUoMMappingService uomMappingService)
    {
        _uomMappingService = uomMappingService;
    }
    
    /// <summary>
    /// Gets all UoM mappings
    /// </summary>
    [HttpGet("mappings")]
    public async Task<ActionResult<List<UoMMapping>>> GetAllMappings()
    {
        var mappings = await _uomMappingService.GetAllMappingsAsync();
        return Ok(mappings);
    }
    
    /// <summary>
    /// Gets UoM mapping by Katana UoM string
    /// </summary>
    [HttpGet("by-uom/{uomString}")]
    public async Task<ActionResult<UoMMapping>> GetMappingByUoMString(string uomString)
    {
        var mapping = await _uomMappingService.GetMappingByUoMStringAsync(uomString);
        
        if (mapping == null)
        {
            return NotFound(new { message = $"No mapping found for UoM '{uomString}'" });
        }
        
        return Ok(mapping);
    }
    
    /// <summary>
    /// Gets Koza olcumBirimiId by Katana UoM string
    /// </summary>
    [HttpGet("{uomString}/olcum-birimi-id")]
    public async Task<ActionResult<long>> GetOlcumBirimiIdByUoMString(string uomString, [FromQuery] long defaultId = 5)
    {
        var olcumBirimiId = await _uomMappingService.GetOlcumBirimiIdByUoMStringAsync(uomString, defaultId);
        return Ok(new { uomString, olcumBirimiId, isDefault = olcumBirimiId == defaultId });
    }
    
    /// <summary>
    /// Creates or updates UoM mapping
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<UoMMapping>> CreateOrUpdateMapping([FromBody] UoMMappingRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.KatanaUoMString))
        {
            return BadRequest(new { message = "UoM string cannot be empty" });
        }
        
        if (request.KozaOlcumBirimiId <= 0)
        {
            return BadRequest(new { message = "olcumBirimiId must be greater than 0" });
        }
        
        var mapping = await _uomMappingService.CreateOrUpdateMappingAsync(
            request.KatanaUoMString,
            request.KozaOlcumBirimiId,
            request.Description);
        
        return Ok(mapping);
    }
    
    /// <summary>
    /// Gets dictionary of UoM string → olcumBirimiId for bulk operations
    /// </summary>
    [HttpGet("dictionary")]
    public async Task<ActionResult<Dictionary<string, long>>> GetUoMDictionary()
    {
        var dictionary = await _uomMappingService.GetUoMToOlcumBirimiIdMapAsync();
        return Ok(dictionary);
    }
    
    /// <summary>
    /// Deletes UoM mapping
    /// </summary>
    [HttpDelete("{uomString}")]
    public async Task<ActionResult> DeleteMapping(string uomString)
    {
        var deleted = await _uomMappingService.DeleteMappingAsync(uomString);
        
        if (!deleted)
        {
            return NotFound(new { message = $"No mapping found for UoM '{uomString}'" });
        }
        
        return NoContent();
    }
}

public class UoMMappingRequest
{
    public string KatanaUoMString { get; set; } = string.Empty;
    public long KozaOlcumBirimiId { get; set; }
    public string? Description { get; set; }
}
