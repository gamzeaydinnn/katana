using Katana.Business.Services;
using System.Threading.Tasks;

namespace Katana.Business.Interfaces;

/// <summary>
/// Katana Order'larını Luca'ya Fatura olarak senkronize eden servis interface'i
/// </summary>
public interface IOrderInvoiceSyncService
{
    /// <summary>
    /// Katana Sales Order'ı Luca'ya Satış Faturası olarak gönderir
    /// Luca Belge Türü: 18 (Satış Faturası)
    /// </summary>
    Task<OrderSyncResultDto> SyncSalesOrderToLucaAsync(int orderId);
    
    /// <summary>
    /// Katana Purchase Order'ı Luca'ya Alım Faturası olarak gönderir
    /// Luca Belge Türü: 16 (Alım Faturası)
    /// </summary>
    Task<OrderSyncResultDto> SyncPurchaseOrderToLucaAsync(int purchaseOrderId);
    
    /// <summary>
    /// Luca'daki faturayı kapatır (ödeme/tahsilat)
    /// Sales Order: Tahsilat işlemi
    /// Purchase Order: Tediye işlemi
    /// </summary>
    Task<OrderSyncResultDto> CloseInvoiceAsync(int orderId, string orderType, decimal amount);
    
    /// <summary>
    /// Luca'daki faturayı siler (sipariş iptali durumunda)
    /// </summary>
    Task<OrderSyncResultDto> DeleteInvoiceAsync(int orderId, string orderType);
    
    /// <summary>
    /// Bekleyen tüm Sales Order'ları toplu olarak Luca'ya gönderir
    /// </summary>
    Task<OrderBatchSyncResultDto> SyncPendingSalesOrdersAsync();
}
