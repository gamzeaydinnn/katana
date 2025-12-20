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

public class UoMMappingService : IUoMMappingService
{
    private readonly IntegrationDbContext _context;
    private readonly ILogger<UoMMappingService> _logger;
    
    public UoMMappingService(
        IntegrationDbContext context,
        ILogger<UoMMappingService> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    public async Task<long> GetOlcumBirimiIdByUoMStringAsync(string? katanaUoMString, long defaultId = 5)
    {
        if (string.IsNullOrWhiteSpace(katanaUoMString))
        {
            _logger.LogDebug("Empty UoM string, using default olcumBirimiId {DefaultId}", defaultId);
            return defaultId;
        }
        
        try
        {
            var normalized = NormalizeUoMString(katanaUoMString);
            
            var mapping = await _context.UoMMappings
                .Where(m => m.KatanaUoMString == normalized && m.IsActive)
                .FirstOrDefaultAsync();
            
            if (mapping != null)
            {
                _logger.LogDebug("Found UoM mapping: '{UoMString}' → {OlcumBirimiId}", 
                    katanaUoMString, mapping.KozaOlcumBirimiId);
                return mapping.KozaOlcumBirimiId;
            }
            
            _logger.LogWarning("No UoM mapping found for '{UoMString}', using default {DefaultId}", 
                katanaUoMString, defaultId);
            return defaultId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting olcumBirimiId for UoM '{UoMString}', using default {DefaultId}", 
                katanaUoMString, defaultId);
            return defaultId;
        }
    }
    
    public async Task<UoMMapping?> GetMappingByUoMStringAsync(string katanaUoMString)
    {
        if (string.IsNullOrWhiteSpace(katanaUoMString))
        {
            return null;
        }
        
        var normalized = NormalizeUoMString(katanaUoMString);
        
        return await _context.UoMMappings
            .Where(m => m.KatanaUoMString == normalized)
            .FirstOrDefaultAsync();
    }
    
    public async Task<UoMMapping> CreateOrUpdateMappingAsync(
        string katanaUoMString, 
        long kozaOlcumBirimiId, 
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(katanaUoMString))
        {
            throw new ArgumentException("UoM string cannot be empty", nameof(katanaUoMString));
        }
        
        var normalized = NormalizeUoMString(katanaUoMString);
        var existing = await GetMappingByUoMStringAsync(normalized);
        
        if (existing != null)
        {
            existing.KozaOlcumBirimiId = kozaOlcumBirimiId;
            existing.Description = description ?? existing.Description;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.IsActive = true;
            
            _logger.LogInformation("Updated UoM mapping: '{UoMString}' → {OlcumBirimiId}", 
                normalized, kozaOlcumBirimiId);
        }
        else
        {
            existing = new UoMMapping
            {
                KatanaUoMString = normalized,
                KozaOlcumBirimiId = kozaOlcumBirimiId,
                Description = description,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            _context.UoMMappings.Add(existing);
            
            _logger.LogInformation("Created UoM mapping: '{UoMString}' → {OlcumBirimiId}", 
                normalized, kozaOlcumBirimiId);
        }
        
        await _context.SaveChangesAsync();
        return existing;
    }
    
    public async Task<List<UoMMapping>> GetAllMappingsAsync()
    {
        return await _context.UoMMappings
            .Where(m => m.IsActive)
            .OrderBy(m => m.KatanaUoMString)
            .ToListAsync();
    }
    
    public async Task<Dictionary<string, long>> GetUoMToOlcumBirimiIdMapAsync()
    {
        return await _context.UoMMappings
            .Where(m => m.IsActive)
            .ToDictionaryAsync(m => m.KatanaUoMString, m => m.KozaOlcumBirimiId);
    }
    
    public async Task<bool> DeleteMappingAsync(string katanaUoMString)
    {
        if (string.IsNullOrWhiteSpace(katanaUoMString))
        {
            return false;
        }
        
        var normalized = NormalizeUoMString(katanaUoMString);
        var mapping = await GetMappingByUoMStringAsync(normalized);
        
        if (mapping == null)
        {
            return false;
        }
        
        mapping.IsActive = false;
        mapping.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Deleted UoM mapping for '{UoMString}'", normalized);
        return true;
    }
    
    /// <summary>
    /// Normalizes UoM string to uppercase for case-insensitive matching
    /// </summary>
    private static string NormalizeUoMString(string uomString)
    {
        return uomString.Trim().ToUpperInvariant();
    }
}
