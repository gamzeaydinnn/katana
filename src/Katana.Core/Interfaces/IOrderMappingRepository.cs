using System.Threading.Tasks;

namespace Katana.Core.Interfaces;

/// <summary>
/// Katana ID'lerini Luca kodlarına çeviren mapping interface.
/// Veritabanında bu eşleşmeleri tutan tablolar olmalıdır.
/// </summary>
public interface IOrderMappingRepository
{
    /// <summary>
    /// Katana Customer ID'den Luca Cari Kodu'nu getirir
    /// </summary>
    Task<string?> GetLucaCariKoduByCustomerIdAsync(int katanaCustomerId);
    
    /// <summary>
    /// Katana Supplier ID'den Luca Tedarikçi Kodu'nu getirir
    /// </summary>
    Task<string?> GetLucaSupplierKoduBySupplierIdAsync(int katanaSupplierId);
    
    /// <summary>
    /// Katana Product ID'den Luca Stok Kodu'nu getirir
    /// </summary>
    Task<string?> GetLucaStokKoduByProductIdAsync(int katanaProductId);
    
    /// <summary>
    /// Katana Product SKU'dan Luca Stok Kodu'nu getirir
    /// </summary>
    Task<string?> GetLucaStokKoduBySkuAsync(string sku);
    
    /// <summary>
    /// Tax Rate ID'den vergi oranını getirir
    /// </summary>
    Task<double> GetTaxRateByIdAsync(int? taxRateId);
    
    /// <summary>
    /// Luca'ya gönderilmiş fatura ID'sini kaydeder (idem potent)
    /// </summary>
    Task SaveLucaInvoiceIdAsync(int orderId, long lucaFaturaId, string orderType, string? externalOrderId = null);
    
    /// <summary>
    /// Order ID'den daha önce kaydedilmiş Luca Fatura ID'sini getirir
    /// </summary>
    Task<long?> GetLucaInvoiceIdByOrderIdAsync(int orderId, string orderType);
    
    /// <summary>
    /// Mevcut Luca fatura ID mapping'ini günceller
    /// </summary>
    Task UpdateLucaInvoiceIdAsync(int orderId, long lucaFaturaId, string orderType, string? externalOrderId = null);
    
    /// <summary>
    /// Luca Belge Tür Detay ID'sini getirir (Satış/Alım için)
    /// </summary>
    Task<long> GetBelgeTurDetayIdAsync(bool isSalesOrder);
    
    /// <summary>
    /// Luca Ödeme Belge Tür ID'sini getirir (Tahsilat/Tediye için)
    /// </summary>
    Task<long> GetPaymentBelgeTurDetayIdAsync(bool isSalesOrder);
    
    /// <summary>
    /// Varsayılan kasa hesap kodunu getirir
    /// </summary>
    Task<string> GetDefaultCashAccountCodeAsync();
}
