using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Katana.Core.Entities;
using Katana.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Katana.Business.Services;

/// <summary>
/// Katana → Luca Append-Only Senkronizasyon için mapping servisi.
/// Luca'da güncelleme endpoint'i olmadığından her değişiklikte yeni versiyonlu SKU oluşturulur.
/// </summary>
public class ProductMappingService : IProductMappingService
{
    private readonly IProductMappingRepository _repository;
    private readonly ILogger<ProductMappingService> _logger;

    public ProductMappingService(
        IProductMappingRepository repository,
        ILogger<ProductMappingService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<ProductUpdateResult> HandleProductUpdateAsync(KatanaProductDto product)
    {
        try
        {
            // 1. Aktif mapping var mı?
            var activeMapping = await _repository.GetActiveMappingByProductIdAsync(product.Id);

            // 2. Hiç mapping yoksa → İlk kez oluştur
            if (activeMapping == null)
            {
                var newMapping = await CreateFirstMappingAsync(product);

                _logger.LogInformation(
                    "İlk versiyon oluşturuldu. KatanaProductId: {ProductId}, LucaStockCode: {StockCode}",
                    product.Id, newMapping.LucaStockCode);

                return new ProductUpdateResult
                {
                    Success = true,
                    IsNewVersion = true,
                    LucaStockCode = newMapping.LucaStockCode,
                    Version = 1,
                    MappingId = newMapping.Id,
                    ShouldSendToLuca = true,
                    Message = "İlk versiyon oluşturuldu"
                };
            }

            // 3. Değişiklik kontrolü
            bool hasChanged = await HasProductChangedAsync(activeMapping, product);

            if (!hasChanged)
            {
                _logger.LogDebug(
                    "Ürün değişmemiş, Luca'ya gönderilmeyecek. KatanaProductId: {ProductId}",
                    product.Id);

                return new ProductUpdateResult
                {
                    Success = true,
                    IsNewVersion = false,
                    LucaStockCode = activeMapping.LucaStockCode,
                    Version = activeMapping.Version,
                    MappingId = activeMapping.Id,
                    ShouldSendToLuca = false,
                    Message = "Ürün değişmemiş"
                };
            }

            // 4. Değişiklik var → Yeni versiyon oluştur
            var newVersion = await CreateNewVersionAsync(product, activeMapping.Version);

            // 5. Eski versiyonları pasif yap
            await _repository.DeactivateOldVersionsAsync(product.Id, newVersion.Id);

            _logger.LogInformation(
                "Yeni versiyon oluşturuldu. KatanaProductId: {ProductId}, Version: {Version}, LucaStockCode: {StockCode}",
                product.Id, newVersion.Version, newVersion.LucaStockCode);

            return new ProductUpdateResult
            {
                Success = true,
                IsNewVersion = true,
                LucaStockCode = newVersion.LucaStockCode,
                Version = newVersion.Version,
                MappingId = newVersion.Id,
                ShouldSendToLuca = true,
                Message = $"V{newVersion.Version} oluşturuldu"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HandleProductUpdateAsync hatası. ProductId: {ProductId}", product.Id);

            return new ProductUpdateResult
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public Task<bool> HasProductChangedAsync(ProductLucaMapping activeMapping, KatanaProductDto product)
    {
        // İsim değişti mi?
        if (!string.Equals(activeMapping.SyncedProductName, product.Name, StringComparison.Ordinal))
        {
            _logger.LogDebug("İsim değişti: '{Old}' → '{New}'",
                activeMapping.SyncedProductName, product.Name);
            return Task.FromResult(true);
        }

        // Fiyat değişti mi?
        var currentPrice = product.SalesPrice ?? product.Price;
        if (activeMapping.SyncedPrice != currentPrice)
        {
            _logger.LogDebug("Fiyat değişti: {Old} → {New}",
                activeMapping.SyncedPrice, currentPrice);
            return Task.FromResult(true);
        }

        // KDV değişti mi?
        if (activeMapping.SyncedVatRate != product.VatRate)
        {
            _logger.LogDebug("KDV değişti: {Old} → {New}",
                activeMapping.SyncedVatRate, product.VatRate);
            return Task.FromResult(true);
        }

        // Barkod değişti mi?
        if (!string.Equals(activeMapping.SyncedBarcode, product.Barcode, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("Barkod değişti: '{Old}' → '{New}'",
                activeMapping.SyncedBarcode, product.Barcode);
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    public Task<string> GenerateVersionedSkuAsync(string originalSku, int version)
    {
        if (version == 1)
            return Task.FromResult(originalSku);

        return Task.FromResult($"{originalSku}-V{version}");
    }

    private async Task<ProductLucaMapping> CreateFirstMappingAsync(KatanaProductDto product)
    {
        var mapping = new ProductLucaMapping
        {
            KatanaProductId = product.Id,
            KatanaSku = product.SKU,
            LucaStockCode = product.SKU, // V1 için orijinal SKU
            Version = 1,
            IsActive = true,
            SyncStatus = "PENDING",
            SyncedProductName = product.Name,
            SyncedPrice = product.SalesPrice ?? product.Price,
            SyncedVatRate = product.VatRate,
            SyncedBarcode = product.Barcode
        };

        return await _repository.CreateAsync(mapping);
    }

    private async Task<ProductLucaMapping> CreateNewVersionAsync(KatanaProductDto product, int previousVersion)
    {
        int newVersion = previousVersion + 1;
        string newLucaStockCode = await GenerateVersionedSkuAsync(product.SKU, newVersion);

        var mapping = new ProductLucaMapping
        {
            KatanaProductId = product.Id,
            KatanaSku = product.SKU,
            LucaStockCode = newLucaStockCode,
            Version = newVersion,
            IsActive = true,
            SyncStatus = "PENDING",
            SyncedProductName = product.Name,
            SyncedPrice = product.SalesPrice ?? product.Price,
            SyncedVatRate = product.VatRate,
            SyncedBarcode = product.Barcode
        };

        return await _repository.CreateAsync(mapping);
    }

    public async Task MarkAsSyncedAsync(int mappingId, long lucaStockId)
    {
        await _repository.MarkAsSyncedAsync(mappingId, lucaStockId);
        _logger.LogDebug("Mapping {MappingId} SYNCED olarak işaretlendi. LucaStockId: {LucaStockId}",
            mappingId, lucaStockId);
    }

    public async Task MarkAsSyncFailedAsync(int mappingId, string errorMessage)
    {
        await _repository.MarkAsSyncFailedAsync(mappingId, errorMessage);
        _logger.LogWarning("Mapping {MappingId} FAILED olarak işaretlendi. Hata: {Error}",
            mappingId, errorMessage);
    }

    public async Task<ProductLucaMapping?> GetActiveMappingAsync(string katanaProductId)
    {
        return await _repository.GetActiveMappingByProductIdAsync(katanaProductId);
    }

    public async Task<List<ProductLucaMapping>> GetPendingMappingsAsync()
    {
        return await _repository.GetPendingMappingsAsync();
    }

    public async Task<List<ProductLucaMapping>> GetFailedMappingsAsync()
    {
        return await _repository.GetFailedMappingsAsync();
    }
}
