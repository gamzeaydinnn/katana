using Katana.Core.Interfaces;
using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Katana.Data.Repositories;

/// <summary>
/// Katana stok hareketi ID'lerini Luca kodlarına eşleştiren repository
/// </summary>
public class StockMovementMappingRepository : IStockMovementMappingRepository
{
    private readonly IntegrationDbContext _context;

    public StockMovementMappingRepository(IntegrationDbContext context)
    {
        _context = context;
    }

    public async Task<string?> GetLucaDepoKoduByLocationIdAsync(int locationId)
    {
        // Location tablosu yoksa varsayılan depo kodu
        // Gerçek implementasyonda Location tablosu kullanılmalı
        return await Task.FromResult("001"); // Varsayılan depo
    }

    public async Task<string?> GetLucaStokKoduByVariantIdAsync(int variantId)
    {
        // Variant tablosu yoksa Product SKU kullan
        // Gerçek implementasyonda ProductVariant tablosu gerekir
        return await Task.FromResult<string?>(null);
    }

    public async Task<string?> GetLucaStokKoduByProductIdAsync(int productId)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == productId);
        
        return product?.SKU;
    }

    public async Task SaveLucaTransferIdAsync(int katanaTransferId, long lucaTransferId)
    {
        // Transfer mapping tablosuna kaydet
        // Şimdilik basit bir çözüm
        await Task.CompletedTask;
    }

    public async Task SaveLucaAdjustmentIdAsync(int katanaAdjustmentId, long lucaDshId)
    {
        // Adjustment mapping tablosuna kaydet
        await Task.CompletedTask;
    }

    public async Task<Dictionary<int, string>> GetAllLocationMappingsAsync()
    {
        // Tüm Location -> Depo mapping'lerini dön
        // Şimdilik boş dictionary
        return await Task.FromResult(new Dictionary<int, string>());
    }
}
