using Katana.Business.Interfaces;
using Katana.Data.Context;
using Katana.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Katana.Infrastructure.Repositories;

/// <summary>
/// Stok hareketi mapping repository implementasyonu
/// Location ID → Depo Kodu, Variant/Product ID → Stok Kodu eşleşmeleri
/// </summary>
public class StockMovementMappingRepository : IStockMovementMappingRepository
{
    private readonly IntegrationDbContext _context;
    private readonly ILogger<StockMovementMappingRepository> _logger;

    // Varsayılan depo kodu prefixleri
    private const string DefaultWarehousePrefix = "DPO-";
    private const string DefaultStockPrefix = "STK-";

    public StockMovementMappingRepository(
        IntegrationDbContext context,
        ILogger<StockMovementMappingRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Katana Location ID'sini Luca Depo Kodu'na çevirir
    /// LocationKozaDepotMapping tablosunu kullanır (KatanaLocationId string olarak tutuluyor)
    /// </summary>
    public async Task<string?> GetLucaDepoKoduByLocationIdAsync(int locationId)
    {
        try
        {
            var locationIdStr = locationId.ToString();
            
            // LocationKozaDepotMapping tablosunda ara
            var mapping = await _context.LocationKozaDepotMappings
                .FirstOrDefaultAsync(m => m.KatanaLocationId == locationIdStr);

            if (mapping != null && !string.IsNullOrEmpty(mapping.KozaDepoKodu))
            {
                return mapping.KozaDepoKodu;
            }

            // Fallback: Prefix + ID
            _logger.LogWarning("Location {LocationId} için Luca depo kodu bulunamadı, fallback kullanılıyor", locationId);
            return $"{DefaultWarehousePrefix}{locationId}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Location {LocationId} için Luca depo kodu alınırken hata", locationId);
            return $"{DefaultWarehousePrefix}{locationId}";
        }
    }

    /// <summary>
    /// Katana Variant ID'sini Luca Stok Kodu'na çevirir
    /// </summary>
    public async Task<string?> GetLucaStokKoduByVariantIdAsync(int variantId)
    {
        try
        {
            // Variant → Product → SKU zinciri
            var variant = await _context.ProductVariants
                .Include(v => v.Product)
                .FirstOrDefaultAsync(v => v.Id == variantId);

            if (variant != null)
            {
                // Variant'ın kendi SKU'su varsa onu kullan
                if (!string.IsNullOrEmpty(variant.SKU))
                    return variant.SKU;

                // Yoksa Product SKU kullan
                if (variant.Product != null && !string.IsNullOrEmpty(variant.Product.SKU))
                    return variant.Product.SKU;
            }

            // Fallback
            _logger.LogWarning("Variant {VariantId} için Luca stok kodu bulunamadı", variantId);
            return $"{DefaultStockPrefix}{variantId}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Variant {VariantId} için Luca stok kodu alınırken hata", variantId);
            return $"{DefaultStockPrefix}{variantId}";
        }
    }

    /// <summary>
    /// Katana Product ID'sini Luca Stok Kodu'na çevirir
    /// </summary>
    public async Task<string?> GetLucaStokKoduByProductIdAsync(int productId)
    {
        try
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product != null && !string.IsNullOrEmpty(product.SKU))
            {
                return product.SKU;
            }

            _logger.LogWarning("Product {ProductId} için Luca stok kodu bulunamadı", productId);
            return $"{DefaultStockPrefix}{productId}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Product {ProductId} için Luca stok kodu alınırken hata", productId);
            return $"{DefaultStockPrefix}{productId}";
        }
    }

    /// <summary>
    /// Luca Transfer ID'sini kaydeder
    /// </summary>
    public async Task SaveLucaTransferIdAsync(int katanaTransferId, long lucaTransferId)
    {
        try
        {
            var transfer = await _context.StockTransfers
                .FirstOrDefaultAsync(t => t.Id == katanaTransferId);

            if (transfer != null)
            {
                // Status'u synced olarak işaretle
                transfer.Status = "Synced";
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Transfer {TransferId} Luca'ya aktarıldı: {LucaId}", 
                    katanaTransferId, lucaTransferId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transfer {TransferId} Luca ID kaydedilirken hata", katanaTransferId);
            throw;
        }
    }

    /// <summary>
    /// Luca DSH (Adjustment) ID'sini kaydeder
    /// </summary>
    public async Task SaveLucaAdjustmentIdAsync(int katanaAdjustmentId, long lucaDshId)
    {
        try
        {
            var adjustment = await _context.PendingStockAdjustments
                .FirstOrDefaultAsync(a => a.Id == katanaAdjustmentId);

            if (adjustment != null)
            {
                // Status'u synced olarak işaretle
                adjustment.Status = "Synced";
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Adjustment {AdjustmentId} Luca'ya aktarıldı: {LucaId}", 
                    katanaAdjustmentId, lucaDshId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Adjustment {AdjustmentId} Luca ID kaydedilirken hata", katanaAdjustmentId);
            throw;
        }
    }

    /// <summary>
    /// Tüm depo mapping'lerini getirir
    /// </summary>
    public async Task<Dictionary<int, string>> GetAllLocationMappingsAsync()
    {
        try
        {
            var mappings = await _context.LocationKozaDepotMappings
                .ToDictionaryAsync(
                    m => int.TryParse(m.KatanaLocationId, out var id) ? id : 0, 
                    m => m.KozaDepoKodu);

            // 0 key'li varsa kaldır (parse edilemeyenler)
            mappings.Remove(0);
            
            return mappings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Depo mapping'leri alınırken hata");
            return new Dictionary<int, string>();
        }
    }
}
