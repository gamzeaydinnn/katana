using Katana.Core.Entities;
using Katana.Core.Utilities;
using Katana.Data.Models;

namespace Katana.Business.Extensions;

/// <summary>
/// Mapping entity'leri için extension method'lar
/// </summary>
public static class MappingExtensions
{
    /// <summary>
    /// Supplier mapping'i oluştur ve hash'ini hesapla
    /// </summary>
    public static SupplierKozaCariMapping CreateSupplierMapping(
        string katanaSupplierId,
        string kozaCariKodu,
        long? kozaFinansalNesneId,
        string? katanaSupplierName = null,
        string? kozaCariTanim = null)
    {
        var mapping = new SupplierKozaCariMapping
        {
            KatanaSupplierId = katanaSupplierId,
            KozaCariKodu = kozaCariKodu,
            KozaFinansalNesneId = kozaFinansalNesneId,
            KatanaSupplierName = katanaSupplierName,
            KozaCariTanim = kozaCariTanim,
            SyncStatus = "PENDING",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        mapping.LastSyncHash = MappingHashHelper.ComputeSupplierMappingHash(
            katanaSupplierId, kozaCariKodu, kozaFinansalNesneId);

        return mapping;
    }

    /// <summary>
    /// Customer mapping'i oluştur ve hash'ini hesapla
    /// </summary>
    public static CustomerKozaCariMapping CreateCustomerMapping(
        int katanaCustomerId,
        string kozaCariKodu,
        long? kozaFinansalNesneId,
        string? katanaCustomerName = null,
        string? kozaCariTanim = null,
        string? katanaCustomerTaxNo = null)
    {
        var mapping = new CustomerKozaCariMapping
        {
            KatanaCustomerId = katanaCustomerId,
            KozaCariKodu = kozaCariKodu,
            KozaFinansalNesneId = kozaFinansalNesneId,
            KatanaCustomerName = katanaCustomerName,
            KozaCariTanim = kozaCariTanim,
            KatanaCustomerTaxNo = katanaCustomerTaxNo,
            SyncStatus = "PENDING",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        mapping.LastSyncHash = MappingHashHelper.ComputeCustomerMappingHash(
            katanaCustomerId, kozaCariKodu, kozaFinansalNesneId, katanaCustomerTaxNo);

        return mapping;
    }

    /// <summary>
    /// Location mapping'i oluştur ve hash'ini hesapla
    /// </summary>
    public static LocationKozaDepotMapping CreateLocationMapping(
        string katanaLocationId,
        string kozaDepoKodu,
        long? kozaDepoId,
        string? katanaLocationName = null,
        string? kozaDepoTanim = null)
    {
        var mapping = new LocationKozaDepotMapping
        {
            KatanaLocationId = katanaLocationId,
            KozaDepoKodu = kozaDepoKodu,
            KozaDepoId = kozaDepoId,
            KatanaLocationName = katanaLocationName,
            KozaDepoTanim = kozaDepoTanim,
            SyncStatus = "PENDING",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        mapping.LastSyncHash = MappingHashHelper.ComputeLocationMappingHash(
            katanaLocationId, kozaDepoKodu, kozaDepoId);

        return mapping;
    }

    /// <summary>
    /// Product mapping'i oluştur ve hash'ini hesapla
    /// </summary>
    public static ProductLucaMapping CreateProductMapping(
        string katanaProductId,
        string katanaSku,
        string lucaStockCode,
        long? lucaStockId = null)
    {
        var mapping = new ProductLucaMapping
        {
            KatanaProductId = katanaProductId,
            KatanaSku = katanaSku,
            LucaStockCode = lucaStockCode,
            LucaStockId = lucaStockId,
            Version = 1,
            IsActive = true,
            SyncStatus = "PENDING",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        mapping.LastSyncHash = MappingHashHelper.ComputeProductMappingHash(
            katanaProductId, katanaSku, lucaStockCode, lucaStockId);

        return mapping;
    }

    /// <summary>
    /// Order mapping'i oluştur ve hash'ini hesapla
    /// </summary>
    public static OrderMapping CreateOrderMapping(
        int orderId,
        long lucaInvoiceId,
        string entityType,
        string? externalOrderId = null)
    {
        var mapping = new OrderMapping
        {
            OrderId = orderId,
            LucaInvoiceId = lucaInvoiceId,
            EntityType = entityType,
            ExternalOrderId = externalOrderId,
            SyncStatus = "SYNCED",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            LastSyncAt = DateTime.UtcNow
        };

        mapping.LastSyncHash = MappingHashHelper.ComputeOrderMappingHash(
            orderId, lucaInvoiceId, entityType, externalOrderId);

        return mapping;
    }

    /// <summary>
    /// MappingTable'a hash ekle
    /// </summary>
    public static void UpdateHash(this MappingTable mapping)
    {
        mapping.LastSyncHash = MappingHashHelper.ComputeHash(new
        {
            mapping.MappingType,
            mapping.SourceValue,
            mapping.TargetValue
        });
    }

    /// <summary>
    /// Supplier mapping'in hash'ini güncelle
    /// </summary>
    public static void UpdateHash(this SupplierKozaCariMapping mapping)
    {
        mapping.LastSyncHash = MappingHashHelper.ComputeSupplierMappingHash(
            mapping.KatanaSupplierId,
            mapping.KozaCariKodu,
            mapping.KozaFinansalNesneId);
    }

    /// <summary>
    /// Location mapping'in hash'ini güncelle
    /// </summary>
    public static void UpdateHash(this LocationKozaDepotMapping mapping)
    {
        mapping.LastSyncHash = MappingHashHelper.ComputeLocationMappingHash(
            mapping.KatanaLocationId,
            mapping.KozaDepoKodu,
            mapping.KozaDepoId);
    }

    /// <summary>
    /// Product mapping'in hash'ini güncelle
    /// </summary>
    public static void UpdateHash(this ProductLucaMapping mapping)
    {
        mapping.LastSyncHash = MappingHashHelper.ComputeProductMappingHash(
            mapping.KatanaProductId,
            mapping.KatanaSku,
            mapping.LucaStockCode,
            mapping.LucaStockId);
    }

    /// <summary>
    /// Order mapping'in hash'ini güncelle
    /// </summary>
    public static void UpdateHash(this OrderMapping mapping)
    {
        mapping.LastSyncHash = MappingHashHelper.ComputeOrderMappingHash(
            mapping.OrderId,
            mapping.LucaInvoiceId,
            mapping.EntityType,
            mapping.ExternalOrderId);
    }

    /// <summary>
    /// Customer mapping'in hash'ini güncelle
    /// </summary>
    public static void UpdateHash(this CustomerKozaCariMapping mapping)
    {
        mapping.LastSyncHash = MappingHashHelper.ComputeCustomerMappingHash(
            mapping.KatanaCustomerId,
            mapping.KozaCariKodu,
            mapping.KozaFinansalNesneId,
            mapping.KatanaCustomerTaxNo);
    }
}
