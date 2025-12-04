using Katana.API.Controllers.DTOs;
using Katana.Business.Interfaces;
using Katana.Core.Constants;
using Katana.Data.Context;
using Katana.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Katana.API.Controllers;





[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MappingController : ControllerBase
{
    private readonly IMappingService _mappingService;
    private readonly IntegrationDbContext _context;
    private readonly ILogger<MappingController> _logger;
    private readonly IAuditLoggerService _auditLogger;

    public MappingController(
        IMappingService mappingService,
        IntegrationDbContext context,
        ILogger<MappingController> logger,
        IAuditLoggerService auditLogger)
    {
        _mappingService = mappingService;
        _context = context;
        _logger = logger;
        _auditLogger = auditLogger;
    }
    
    
    
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<object>> GetMappings(
        [FromQuery] string? mappingType = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var query = _context.MappingTables.AsQueryable();

            if (!string.IsNullOrEmpty(mappingType))
            {
                query = query.Where(m => m.MappingType == mappingType.ToUpper());
            }

            var totalCount = await query.CountAsync();
            var mappings = await query
                .OrderBy(m => m.MappingType)
                .ThenBy(m => m.SourceValue)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new
                {
                    m.Id,
                    m.MappingType,
                    m.SourceValue,
                    m.TargetValue,
                    m.Description,
                    m.IsActive,
                    m.CreatedAt,
                    m.UpdatedAt,
                    m.CreatedBy,
                    m.UpdatedBy
                })
                .ToListAsync();

            return Ok(new
            {
                mappings,
                totalCount,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving mappings");
            return StatusCode(500, new { error = "Internal server error retrieving mappings" });
        }
    }

    
    
    
    [HttpGet("sku-accounts")]
    public async Task<ActionResult<Dictionary<string, string>>> GetSkuAccountMappings()
    {
        try
        {
            var mappings = await _mappingService.GetSkuToAccountMappingAsync();
            return Ok(mappings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving SKU account mappings");
            return StatusCode(500, new { error = "Internal server error retrieving SKU mappings" });
        }
    }
    
    
    
    [HttpGet("locations")]
    public async Task<ActionResult<Dictionary<string, string>>> GetLocationMappings()
    {
        try
        {
            var mappings = await _mappingService.GetLocationMappingAsync();
            return Ok(mappings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving location mappings");
            return StatusCode(500, new { error = "Internal server error retrieving location mappings" });
        }
    }
    
    
    
    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<object>> CreateMapping([FromBody] CreateMappingRequest request)
    {
        try
        {
            var normalizedType = (request.MappingType ?? string.Empty).Trim().ToUpperInvariant();
            var normalizedSource = (request.SourceValue ?? string.Empty).Trim().ToUpperInvariant();
            var normalizedTarget = (request.TargetValue ?? string.Empty).Trim();

            var existingMapping = await _context.MappingTables
                .FirstOrDefaultAsync(m => m.MappingType == normalizedType && m.SourceValue == normalizedSource);

            if (existingMapping != null)
            {
                return BadRequest(new { error = "Mapping already exists for this source value" });
            }

            var mapping = new MappingTable
            {
                MappingType = normalizedType,
                SourceValue = normalizedSource,
                TargetValue = normalizedTarget,
                Description = request.Description,
                IsActive = request.IsActive ?? true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = User.Identity?.Name ?? "API"
            };

            _context.MappingTables.Add(mapping);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                
                return Conflict(new { error = "Mapping for the given type and source already exists" });
            }
            await _auditLogger.LogAsync("CREATE", "MappingTable", mapping.Id, 
            $"Created mapping {mapping.SourceValue} -> {mapping.TargetValue}", 
            User.Identity?.Name ?? "API");


            _logger.LogInformation("Created new mapping: {MappingType} {SourceValue} -> {TargetValue}", 
                mapping.MappingType, mapping.SourceValue, mapping.TargetValue);

            return CreatedAtAction(nameof(GetMappingById), new { id = mapping.Id }, new
            {
                mapping.Id,
                mapping.MappingType,
                mapping.SourceValue,
                mapping.TargetValue,
                mapping.Description,
                mapping.IsActive,
                mapping.CreatedAt,
                mapping.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating mapping");
            return StatusCode(500, new { error = "Internal server error creating mapping" });
        }
    }
    
    
    
    [HttpPut("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> UpdateMapping(int id, [FromBody] UpdateMappingRequest request)
    {
        try
        {
            var mapping = await _context.MappingTables.FindAsync(id);
            if (mapping == null)
            {
                return NotFound(new { error = "Mapping not found" });
            }

            mapping.TargetValue = request.TargetValue ?? mapping.TargetValue;
            mapping.Description = request.Description ?? mapping.Description;
            mapping.IsActive = request.IsActive ?? mapping.IsActive;
            mapping.UpdatedAt = DateTime.UtcNow;
            mapping.UpdatedBy = User.Identity?.Name ?? "API";

            await _context.SaveChangesAsync();
            await _auditLogger.LogAsync("UPDATE", "MappingTable", mapping.Id, 
           $"Updated mapping to {mapping.TargetValue}", 
           User.Identity?.Name ?? "API");


            _logger.LogInformation("Updated mapping {Id}: {SourceValue} -> {TargetValue}", 
                mapping.Id, mapping.SourceValue, mapping.TargetValue);

            return Ok(new
            {
                mapping.Id,
                mapping.MappingType,
                mapping.SourceValue,
                mapping.TargetValue,
                mapping.Description,
                mapping.IsActive,
                mapping.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating mapping {Id}", id);
            return StatusCode(500, new { error = "Internal server error updating mapping" });
        }
    }

    
    
    
    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetMappingById(int id)
    {
        try
        {
            var mapping = await _context.MappingTables.FindAsync(id);
            if (mapping == null)
            {
                return NotFound(new { error = "Mapping not found" });
            }

            return Ok(new
            {
                mapping.Id,
                mapping.MappingType,
                mapping.SourceValue,
                mapping.TargetValue,
                mapping.Description,
                mapping.IsActive,
                mapping.CreatedAt,
                mapping.UpdatedAt,
                mapping.CreatedBy,
                mapping.UpdatedBy
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving mapping {Id}", id);
            return StatusCode(500, new { error = "Internal server error retrieving mapping" });
        }
    }

    
    
    
    [HttpDelete("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult> DeleteMapping(int id)
    {
        try
        {
            var mapping = await _context.MappingTables.FindAsync(id);
            if (mapping == null)
            {
                return NotFound(new { error = "Mapping not found" });
            }

            _context.MappingTables.Remove(mapping);
            await _context.SaveChangesAsync();
            await _auditLogger.LogAsync("DELETE", "MappingTable", mapping.Id,
                $"Deleted mapping {mapping.SourceValue}",
                User.Identity?.Name ?? "API");


            _logger.LogInformation("Deleted mapping {Id}: {SourceValue} -> {TargetValue}",
                id, mapping.SourceValue, mapping.TargetValue);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting mapping {Id}", id);
            return StatusCode(500, new { error = "Internal server error deleting mapping" });
        }
    }

    /// <summary>
    /// Luca sistemindeki mevcut kategori kodlarını ve açıklamalarını getirir
    /// </summary>
    [HttpGet("luca-categories")]
    [AllowAnonymous] // Test için
    public ActionResult<object> GetLucaCategories()
    {
        try
        {
            var categories = LucaConstants.LUCA_CATEGORIES
                .Select(kv => new
                {
                    code = kv.Key,
                    description = kv.Value
                })
                .OrderBy(c => c.code)
                .ToList();

            _logger.LogInformation("Retrieved {Count} Luca categories", categories.Count);

            return Ok(new
            {
                categories,
                totalCount = categories.Count,
                message = "Luca kategori listesi başarıyla getirildi"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Luca categories");
            return StatusCode(500, new { error = "Luca kategorileri getirilirken hata oluştu" });
        }
    }

    /// <summary>
    /// Katana kategori -> Luca kategori mapping'lerini getirir
    /// </summary>
    [HttpGet("category-mappings")]
    [AllowAnonymous] // Test için
    public async Task<ActionResult<object>> GetCategoryMappings()
    {
        try
        {
            var mappingsData = await _context.MappingTables
                .Where(m => m.MappingType == "PRODUCT_CATEGORY" && m.IsActive)
                .Select(m => new
                {
                    m.Id,
                    m.SourceValue,
                    m.TargetValue,
                    m.Description,
                    m.IsActive,
                    m.CreatedAt,
                    m.UpdatedAt
                })
                .OrderBy(m => m.SourceValue)
                .ToListAsync();

            var mappings = mappingsData.Select(m => new
            {
                m.Id,
                katanaCategory = m.SourceValue,
                lucaCategoryCode = m.TargetValue,
                lucaCategoryDescription = LucaConstants.LUCA_CATEGORIES.ContainsKey(m.TargetValue)
                    ? LucaConstants.LUCA_CATEGORIES[m.TargetValue]
                    : "Bilinmeyen Kategori",
                m.Description,
                m.IsActive,
                m.CreatedAt,
                m.UpdatedAt
            }).ToList();

            _logger.LogInformation("Retrieved {Count} category mappings", mappings.Count);

            return Ok(new
            {
                mappings,
                totalCount = mappings.Count,
                message = "Kategori mapping'leri başarıyla getirildi"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving category mappings");
            return StatusCode(500, new { error = "Kategori mapping'leri getirilirken hata oluştu" });
        }
    }
}
