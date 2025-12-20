using Katana.Core.Entities;
using Katana.Core.Interfaces;
using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Katana.Data.Repositories;

/// <summary>
/// Katana ürünleri ile Luca stok kartları arasındaki eşleştirme repository'si
/// </summary>
public class ProductMappingRepository : IProductMappingRepository
{
    private readonly IntegrationDbContext _context;

    public ProductMappingRepository(IntegrationDbContext context)
    {
        _context = context;
    }

    public async Task<ProductLucaMapping?> GetActiveMappingByProductIdAsync(string katanaProductId)
    {
        return await _context.ProductLucaMappings
            .Where(m => m.KatanaProductId == katanaProductId && m.IsActive)
            .OrderByDescending(m => m.Version)
            .FirstOrDefaultAsync();
    }

    public async Task<List<ProductLucaMapping>> GetAllVersionsByProductIdAsync(string katanaProductId)
    {
        return await _context.ProductLucaMappings
            .Where(m => m.KatanaProductId == katanaProductId)
            .OrderByDescending(m => m.Version)
            .ToListAsync();
    }

    public async Task<ProductLucaMapping?> GetByLucaStockCodeAsync(string lucaStockCode)
    {
        return await _context.ProductLucaMappings
            .FirstOrDefaultAsync(m => m.LucaStockCode == lucaStockCode);
    }

    public async Task<ProductLucaMapping> CreateAsync(ProductLucaMapping mapping)
    {
        mapping.CreatedAt = DateTime.UtcNow;
        mapping.UpdatedAt = DateTime.UtcNow;

        _context.ProductLucaMappings.Add(mapping);
        await _context.SaveChangesAsync();

        return mapping;
    }

    public async Task UpdateAsync(ProductLucaMapping mapping)
    {
        mapping.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task DeactivateOldVersionsAsync(string katanaProductId, int currentMappingId)
    {
        var oldMappings = await _context.ProductLucaMappings
            .Where(m => m.KatanaProductId == katanaProductId && m.Id != currentMappingId)
            .ToListAsync();

        foreach (var mapping in oldMappings)
        {
            mapping.IsActive = false;
            mapping.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    public async Task MarkAsSyncedAsync(int mappingId, long lucaStockId)
    {
        var mapping = await _context.ProductLucaMappings.FindAsync(mappingId);
        if (mapping == null) return;

        mapping.SyncStatus = "SYNCED";
        mapping.LucaStockId = lucaStockId;
        mapping.SyncedAt = DateTime.UtcNow;
        mapping.LastSyncError = null;
        mapping.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task MarkAsSyncFailedAsync(int mappingId, string errorMessage)
    {
        var mapping = await _context.ProductLucaMappings.FindAsync(mappingId);
        if (mapping == null) return;

        mapping.SyncStatus = "FAILED";
        mapping.LastSyncError = errorMessage;
        mapping.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task<List<ProductLucaMapping>> GetPendingMappingsAsync()
    {
        return await _context.ProductLucaMappings
            .Where(m => m.SyncStatus == "PENDING" && m.IsActive)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<ProductLucaMapping>> GetFailedMappingsAsync()
    {
        return await _context.ProductLucaMappings
            .Where(m => m.SyncStatus == "FAILED" && m.IsActive)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<ProductLucaMapping>> GetAllActiveMappingsAsync()
    {
        return await _context.ProductLucaMappings
            .Where(m => m.IsActive)
            .OrderBy(m => m.KatanaProductId)
            .ThenByDescending(m => m.Version)
            .ToListAsync();
    }
}
