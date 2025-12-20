using Katana.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace Katana.Business.Services;

/// <summary>
/// Katana belge türlerini Koza belge türlerine eşleştiren servis
/// </summary>
public interface IDocumentMappingService
{
    long GetBelgeTurDetayIdForSalesOrder(bool isFulfilled, bool isReturn);
    long GetBelgeTurDetayIdForPurchaseOrder(bool isReceived, bool isReturn);
    long GetBelgeTurDetayIdForStockTransfer();
    long GetBelgeTurDetayIdForStockAdjustment(string adjustmentType);
    long GetBelgeTurDetayIdForStocktake();
    string GetKozaDocumentType(string katanaDocumentType);
    (string DocumentType, long BelgeTurDetayId) MapKatanaToKoza(string katanaDocumentType, Dictionary<string, object>? metadata = null);
}

public class DocumentMappingService : IDocumentMappingService
{
    private readonly ILogger<DocumentMappingService> _logger;

    public DocumentMappingService(ILogger<DocumentMappingService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Sales Order → Koza belge türü
    /// Fulfillment durumuna göre İrsaliye veya Fatura
    /// </summary>
    public long GetBelgeTurDetayIdForSalesOrder(bool isFulfilled, bool isReturn)
    {
        if (isReturn)
        {
            // İade durumu
            return isFulfilled 
                ? BelgeTuruValidator.Irsaliye.SatisIadeIrsaliyesi 
                : BelgeTuruValidator.Fatura.SatisIadeFaturasi;
        }

        // Normal satış
        return isFulfilled 
            ? BelgeTuruValidator.Irsaliye.SatisIrsaliyesi 
            : BelgeTuruValidator.Fatura.MalSatisFaturasi;
    }

    /// <summary>
    /// Purchase Order → Koza belge türü
    /// Received durumuna göre İrsaliye veya Fatura
    /// </summary>
    public long GetBelgeTurDetayIdForPurchaseOrder(bool isReceived, bool isReturn)
    {
        if (isReturn)
        {
            // İade durumu
            return isReceived 
                ? BelgeTuruValidator.Irsaliye.AlimIadeIrsaliyesi 
                : BelgeTuruValidator.Fatura.AlimIadeFaturasi;
        }

        // Normal alım
        return isReceived 
            ? BelgeTuruValidator.Irsaliye.AlimIrsaliyesi 
            : BelgeTuruValidator.Fatura.MalAlimFaturasi;
    }

    /// <summary>
    /// Stock Transfer → Koza Depo Transferi
    /// </summary>
    public long GetBelgeTurDetayIdForStockTransfer()
    {
        return BelgeTuruValidator.StokHareketi.DepoTransferi;
    }

    /// <summary>
    /// Stock Adjustment → Koza Diğer Stok Hareketi
    /// Adjustment tipine göre Fire, Sarf, Sayım Fazlası vb.
    /// </summary>
    public long GetBelgeTurDetayIdForStockAdjustment(string adjustmentType)
    {
        if (string.IsNullOrWhiteSpace(adjustmentType))
        {
            _logger.LogWarning("Adjustment type is null or empty, using default: DigerGiris");
            return BelgeTuruValidator.StokHareketi.DigerGiris;
        }

        var normalized = adjustmentType.ToUpperInvariant().Trim();

        return normalized switch
        {
            "FIRE" or "WASTAGE" or "DAMAGE" => BelgeTuruValidator.StokHareketi.Fire,
            "SARF" or "CONSUMPTION" or "USE" => BelgeTuruValidator.StokHareketi.Sarf,
            "SAYIM_FAZLASI" or "COUNT_SURPLUS" or "SURPLUS" => BelgeTuruValidator.StokHareketi.SayimFazlasi,
            "SAYIM_EKSIGI" or "COUNT_SHORTAGE" or "SHORTAGE" => BelgeTuruValidator.StokHareketi.SayimEksigi,
            "GIRIS" or "IN" or "INCREASE" => BelgeTuruValidator.StokHareketi.DigerGiris,
            "CIKIS" or "OUT" or "DECREASE" => BelgeTuruValidator.StokHareketi.DigerCikis,
            _ => BelgeTuruValidator.StokHareketi.DigerGiris
        };
    }

    /// <summary>
    /// Stocktake → Koza Sayım Sonuç Fişi
    /// </summary>
    public long GetBelgeTurDetayIdForStocktake()
    {
        return BelgeTuruValidator.Sayim.SayimSonucFisi;
    }

    /// <summary>
    /// Katana belge tipini Koza belge tipine çevir
    /// </summary>
    public string GetKozaDocumentType(string katanaDocumentType)
    {
        if (string.IsNullOrWhiteSpace(katanaDocumentType))
        {
            throw new ArgumentException("Katana document type cannot be null or empty", nameof(katanaDocumentType));
        }

        var normalized = katanaDocumentType.ToUpperInvariant().Trim();

        return normalized switch
        {
            "SALES_ORDER" or "SALESORDER" or "SO" => "FATURA",
            "SALES_ORDER_FULFILLED" or "FULFILLMENT" or "SHIPPING" => "IRSALIYE",
            "PURCHASE_ORDER" or "PURCHASEORDER" or "PO" => "FATURA",
            "PURCHASE_ORDER_RECEIVED" or "RECEIVING" => "IRSALIYE",
            "STOCK_TRANSFER" or "TRANSFER" => "DEPO_TRANSFERI",
            "STOCK_ADJUSTMENT" or "ADJUSTMENT" => "STOK_HAREKETI",
            "STOCKTAKE" or "STOCK_COUNT" or "INVENTORY_COUNT" => "SAYIM",
            "INVOICE" => "FATURA",
            "WAYBILL" => "IRSALIYE",
            _ => throw new ArgumentException($"Unknown Katana document type: {katanaDocumentType}")
        };
    }

    /// <summary>
    /// Katana belge tipini Koza'ya map et (belge tipi + belge tür detay ID)
    /// </summary>
    public (string DocumentType, long BelgeTurDetayId) MapKatanaToKoza(
        string katanaDocumentType, 
        Dictionary<string, object>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(katanaDocumentType))
        {
            throw new ArgumentException("Katana document type cannot be null or empty", nameof(katanaDocumentType));
        }

        var normalized = katanaDocumentType.ToUpperInvariant().Trim();
        metadata ??= new Dictionary<string, object>();

        // Metadata'dan bilgi al
        var isFulfilled = GetMetadataBool(metadata, "IsFulfilled", false);
        var isReceived = GetMetadataBool(metadata, "IsReceived", false);
        var isReturn = GetMetadataBool(metadata, "IsReturn", false);
        var adjustmentType = GetMetadataString(metadata, "AdjustmentType", "");

        return normalized switch
        {
            "SALES_ORDER" or "SALESORDER" or "SO" => 
                ("FATURA", GetBelgeTurDetayIdForSalesOrder(isFulfilled, isReturn)),
            
            "SALES_ORDER_FULFILLED" or "FULFILLMENT" or "SHIPPING" => 
                ("IRSALIYE", GetBelgeTurDetayIdForSalesOrder(true, isReturn)),
            
            "PURCHASE_ORDER" or "PURCHASEORDER" or "PO" => 
                ("FATURA", GetBelgeTurDetayIdForPurchaseOrder(isReceived, isReturn)),
            
            "PURCHASE_ORDER_RECEIVED" or "RECEIVING" => 
                ("IRSALIYE", GetBelgeTurDetayIdForPurchaseOrder(true, isReturn)),
            
            "STOCK_TRANSFER" or "TRANSFER" => 
                ("DEPO_TRANSFERI", GetBelgeTurDetayIdForStockTransfer()),
            
            "STOCK_ADJUSTMENT" or "ADJUSTMENT" => 
                ("STOK_HAREKETI", GetBelgeTurDetayIdForStockAdjustment(adjustmentType)),
            
            "STOCKTAKE" or "STOCK_COUNT" or "INVENTORY_COUNT" => 
                ("SAYIM", GetBelgeTurDetayIdForStocktake()),
            
            _ => throw new ArgumentException($"Unknown Katana document type: {katanaDocumentType}")
        };
    }

    private bool GetMetadataBool(Dictionary<string, object> metadata, string key, bool defaultValue)
    {
        if (metadata.TryGetValue(key, out var value))
        {
            if (value is bool boolValue)
                return boolValue;
            
            if (bool.TryParse(value?.ToString(), out var parsed))
                return parsed;
        }

        return defaultValue;
    }

    private string GetMetadataString(Dictionary<string, object> metadata, string key, string defaultValue)
    {
        if (metadata.TryGetValue(key, out var value))
        {
            return value?.ToString() ?? defaultValue;
        }

        return defaultValue;
    }
}
