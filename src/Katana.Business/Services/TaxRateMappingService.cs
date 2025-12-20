using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Katana.Core.Entities;
using Katana.Core.Interfaces;
using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Katana.Business.Services;

public class TaxRateMappingService : ITaxRateMappingService
{
    private readonly IntegrationDbContext _context;
    private readonly ILogger<TaxRateMappingService> _logger;
    
    public TaxRateMappingService(
        IntegrationDbContext context,
        ILogger<TaxRateMappingService> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    public async Task<decimal> GetKdvOranByTaxRateIdAsync(long katanaTaxRateId, decimal defaultRate = 0.20m)
    {
        try
        {
            var mapping = await _context.TaxRateMappings
                .Where(m => m.KatanaTaxRateId == katanaTaxRateId && m.IsActive)
                .FirstOrDefaultAsync();
            
            if (mapping != null)
            {
                _logger.LogDebug("Found tax rate mapping: Katana {TaxRateId} → Koza {KdvOran}", 
                    katanaTaxRateId, mapping.KozaKdvOran);
                return mapping.KozaKdvOran;
            }
            
            _logger.LogWarning("No tax rate mapping found for Katana tax_rate_id {TaxRateId}, using default {DefaultRate}", 
                katanaTaxRateId, defaultRate);
            return defaultRate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting KDV oran for tax_rate_id {TaxRateId}, using default {DefaultRate}", 
                katanaTaxRateId, defaultRate);
            return defaultRate;
        }
    }
    
    public async Task<TaxRateMapping?> GetMappingByTaxRateIdAsync(long katanaTaxRateId)
    {
        return await _context.TaxRateMappings
            .Where(m => m.KatanaTaxRateId == katanaTaxRateId)
            .FirstOrDefaultAsync();
    }
    
    public async Task<TaxRateMapping> CreateOrUpdateMappingAsync(
        long katanaTaxRateId, 
        decimal kozaKdvOran, 
        string? description = null)
    {
        var existing = await GetMappingByTaxRateIdAsync(katanaTaxRateId);
        
        if (existing != null)
        {
            existing.KozaKdvOran = kozaKdvOran;
            existing.Description = description ?? existing.Description;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.IsActive = true;
            
            _logger.LogInformation("Updated tax rate mapping: Katana {TaxRateId} → Koza {KdvOran}", 
                katanaTaxRateId, kozaKdvOran);
        }
        else
        {
            existing = new TaxRateMapping
            {
                KatanaTaxRateId = katanaTaxRateId,
                KozaKdvOran = kozaKdvOran,
                Description = description,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            _context.TaxRateMappings.Add(existing);
            
            _logger.LogInformation("Created tax rate mapping: Katana {TaxRateId} → Koza {KdvOran}", 
                katanaTaxRateId, kozaKdvOran);
        }
        
        await _context.SaveChangesAsync();
        return existing;
    }
    
    public async Task<List<TaxRateMapping>> GetAllMappingsAsync()
    {
        return await _context.TaxRateMappings
            .Where(m => m.IsActive)
            .OrderBy(m => m.KatanaTaxRateId)
            .ToListAsync();
    }
    
    public async Task<Dictionary<long, decimal>> GetTaxRateToKdvOranMapAsync()
    {
        return await _context.TaxRateMappings
            .Where(m => m.IsActive)
            .ToDictionaryAsync(m => m.KatanaTaxRateId, m => m.KozaKdvOran);
    }
    
    public async Task<bool> DeleteMappingAsync(long katanaTaxRateId)
    {
        var mapping = await GetMappingByTaxRateIdAsync(katanaTaxRateId);
        
        if (mapping == null)
        {
            return false;
        }
        
        mapping.IsActive = false;
        mapping.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Deleted tax rate mapping for Katana tax_rate_id {TaxRateId}", katanaTaxRateId);
        return true;
    }
}
