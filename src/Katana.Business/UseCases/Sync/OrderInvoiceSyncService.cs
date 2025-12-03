using Katana.Business.Interfaces;
using Katana.Core.Interfaces;
using Katana.Core.DTOs;
using Katana.Core.Entities;
using Katana.Core.Enums;
using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Katana.Business.UseCases.Sync;

/// <summary>
/// Katana Sales Order & Purchase Order'ları Luca'ya Fatura olarak aktaran,
/// gerektiğinde ödemesini kapatan ve silen tam entegrasyon servisi.
/// 
/// Akış:
/// 1. Katana Order → LucaCreateInvoiceHeaderRequest mapping
/// 2. Luca API'ye fatura gönderimi
/// 3. Fatura ID kaydetme
/// 4. Ödeme kapama (opsiyonel)
/// 5. Silme (iptal durumunda)
/// </summary>
public class OrderInvoiceSyncService : IOrderInvoiceSyncService
{
    private readonly IntegrationDbContext _context;
    private readonly ILucaService _lucaService;
    private readonly IOrderMappingRepository _mappingRepo;
    private readonly ILogger<OrderInvoiceSyncService> _logger;

    // Luca Belge Türleri
    private const int LUCA_SATIS_FATURASI = 18;      // Satış Faturası
    private const int LUCA_ALIM_FATURASI = 16;       // Alım Faturası
    private const int MUSTERI = 1;                   // Müşteri
    private const int TEDARIKCI = 2;                 // Tedarikçi
    private const int MAL_HIZMET = 1;                // Mal/Hizmet faturası
    private const int STOK_KARTI = 1;                // Stok kartı türü

    public OrderInvoiceSyncService(
        IntegrationDbContext context,
        ILucaService lucaService,
        IOrderMappingRepository mappingRepo,
        ILogger<OrderInvoiceSyncService> logger)
    {
        _context = context;
        _lucaService = lucaService;
        _mappingRepo = mappingRepo;
        _logger = logger;
    }

    #region Sales Order → Luca Satış Faturası

    /// <summary>
    /// Katana Sales Order'ı Luca'ya Satış Faturası olarak gönderir.
    /// </summary>
    public async Task<OrderSyncResultDto> SyncSalesOrderToLucaAsync(int orderId)
    {
        var result = new OrderSyncResultDto { OrderId = orderId, OrderType = "SalesOrder" };

        try
        {
            // 1. Order'ı getir
            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                result.Success = false;
                result.Message = $"Order bulunamadı: {orderId}";
                return result;
            }

            // 2. Daha önce gönderilmiş mi kontrol et
            var existingLucaId = await _mappingRepo.GetLucaInvoiceIdByOrderIdAsync(orderId, "SalesOrder");
            if (existingLucaId.HasValue)
            {
                result.Success = true;
                result.LucaFaturaId = existingLucaId.Value;
                result.Message = $"Order zaten Luca'ya gönderilmiş. Fatura ID: {existingLucaId.Value}";
                return result;
            }

            // 3. Luca request'i oluştur
            var lucaRequest = await BuildSalesInvoiceRequestAsync(order);
            if (lucaRequest == null)
            {
                result.Success = false;
                result.Message = "Luca fatura request'i oluşturulamadı - mapping eksik olabilir";
                return result;
            }

            // 4. Luca'ya gönder
            var syncResult = await _lucaService.SendInvoiceAsync(lucaRequest);

            if (syncResult.IsSuccess)
            {
                // 5. Başarılı - Luca ID'yi kaydet
                // Not: SyncResultDto'dan faturaId almak gerekebilir, 
                // veya CreateInvoiceRawAsync kullanılarak JsonElement'ten parse edilebilir
                result.Success = true;
                result.Message = $"Satış faturası Luca'ya başarıyla gönderildi. Order: {order.OrderNo}";
                
                // Order'ı synced olarak işaretle
                order.IsSynced = true;
                order.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Sales Order {OrderNo} successfully synced to Luca", order.OrderNo);
            }
            else
            {
                result.Success = false;
                result.Message = $"Luca API hatası: {syncResult.Message}";
                _logger.LogWarning("Failed to sync Sales Order {OrderNo} to Luca: {Message}", order.OrderNo, syncResult.Message);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing Sales Order {OrderId} to Luca", orderId);
            result.Success = false;
            result.Message = $"Hata: {ex.Message}";
            return result;
        }
    }

    /// <summary>
    /// Sales Order'ı Luca fatura formatına çevirir
    /// </summary>
    private async Task<LucaCreateInvoiceHeaderRequest?> BuildSalesInvoiceRequestAsync(Order order)
    {
        // Müşteri Luca kodu
        var cariKodu = await _mappingRepo.GetLucaCariKoduByCustomerIdAsync(order.CustomerId);
        if (string.IsNullOrEmpty(cariKodu))
        {
            _logger.LogWarning("Customer {CustomerId} için Luca cari kodu bulunamadı", order.CustomerId);
            // Fallback: Customer entity'den oluştur
            cariKodu = $"MUS-{order.CustomerId:D5}";
        }

        var belgeTurDetayId = await _mappingRepo.GetBelgeTurDetayIdAsync(isSalesOrder: true);

        var request = new LucaCreateInvoiceHeaderRequest
        {
            BelgeSeri = "A", // Fatura serisi
            BelgeNo = TryParseOrderNo(order.OrderNo),
            BelgeTarihi = order.OrderDate,
            VadeTarihi = order.OrderDate.AddDays(30), // 30 gün vade
            BelgeAciklama = $"Katana Sales Order #{order.OrderNo}",
            BelgeTurDetayId = belgeTurDetayId,
            FaturaTur = MAL_HIZMET,
            ParaBirimKod = order.Currency ?? "TRY",
            KdvFlag = false, // KDV hariç (genelde ERP'ler hariç çalışır)
            MusteriTedarikci = MUSTERI,
            CariKodu = cariKodu,
            CariTanim = order.Customer?.Title,
            VergiNo = order.Customer?.TaxNo,
            SiparisNo = order.OrderNo,
            SiparisTarihi = order.OrderDate,
            DetayList = new List<LucaCreateInvoiceDetailRequest>()
        };

        // Satırları dönüştür
        foreach (var item in order.Items)
        {
            var kartKodu = await _mappingRepo.GetLucaStokKoduByProductIdAsync(item.ProductId);
            if (string.IsNullOrEmpty(kartKodu))
            {
                // Fallback: Product SKU kullan
                kartKodu = item.Product?.SKU ?? $"PRD-{item.ProductId:D5}";
            }

            var taxRate = await _mappingRepo.GetTaxRateByIdAsync(null); // Default KDV

            request.DetayList.Add(new LucaCreateInvoiceDetailRequest
            {
                KartTuru = STOK_KARTI,
                KartKodu = kartKodu,
                KartAdi = item.Product?.Name,
                Miktar = item.Quantity,
                BirimFiyat = (double)item.UnitPrice,
                KdvOran = taxRate,
                Aciklama = item.Product?.Name
            });
        }

        return request;
    }

    #endregion

    #region Purchase Order → Luca Alım Faturası

    /// <summary>
    /// Katana Purchase Order'ı Luca'ya Alım Faturası olarak gönderir.
    /// Not: PurchaseOrder entity'si projede yoksa, Order entity'si supplier_id ile kullanılabilir
    /// veya ayrı bir PurchaseOrder entity'si oluşturulmalıdır.
    /// </summary>
    public async Task<OrderSyncResultDto> SyncPurchaseOrderToLucaAsync(int purchaseOrderId)
    {
        var result = new OrderSyncResultDto { OrderId = purchaseOrderId, OrderType = "PurchaseOrder" };

        try
        {
            // PurchaseOrder entity'si varsa kullan, yoksa Order entity'si supplier modunda
            // Bu örnekte genel bir yaklaşım gösteriyoruz

            var existingLucaId = await _mappingRepo.GetLucaInvoiceIdByOrderIdAsync(purchaseOrderId, "PurchaseOrder");
            if (existingLucaId.HasValue)
            {
                result.Success = true;
                result.LucaFaturaId = existingLucaId.Value;
                result.Message = $"Purchase Order zaten Luca'ya gönderilmiş. Fatura ID: {existingLucaId.Value}";
                return result;
            }

            // TODO: Gerçek PurchaseOrder entity'si eklendiğinde bu method güncellenmeli
            _logger.LogWarning("PurchaseOrder sync not fully implemented yet. PO ID: {POId}", purchaseOrderId);
            
            result.Success = false;
            result.Message = "PurchaseOrder entity henüz implemente edilmedi";
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing Purchase Order {POId} to Luca", purchaseOrderId);
            result.Success = false;
            result.Message = $"Hata: {ex.Message}";
            return result;
        }
    }

    #endregion

    #region Fatura Kapama (Ödeme/Tahsilat)

    /// <summary>
    /// Luca'daki faturayı kapatır (ödeme/tahsilat işler)
    /// Sales Order için: Tahsilat
    /// Purchase Order için: Tediye
    /// </summary>
    public async Task<OrderSyncResultDto> CloseInvoiceAsync(int orderId, string orderType, decimal amount)
    {
        var result = new OrderSyncResultDto { OrderId = orderId, OrderType = orderType };

        try
        {
            var isSalesOrder = orderType.Equals("SalesOrder", StringComparison.OrdinalIgnoreCase);
            
            // Luca fatura ID'sini bul
            var lucaFaturaId = await _mappingRepo.GetLucaInvoiceIdByOrderIdAsync(orderId, orderType);
            if (!lucaFaturaId.HasValue)
            {
                result.Success = false;
                result.Message = $"Order için Luca fatura ID bulunamadı: {orderId}";
                return result;
            }

            var belgeTurDetayId = await _mappingRepo.GetPaymentBelgeTurDetayIdAsync(isSalesOrder);
            var kasaKodu = await _mappingRepo.GetDefaultCashAccountCodeAsync();

            var closeRequest = new LucaCloseInvoiceRequest
            {
                FaturaId = lucaFaturaId.Value,
                Tutar = (double)amount,
                BelgeTurDetayId = belgeTurDetayId,
                CariKod = kasaKodu
            };

            var response = await _lucaService.CloseInvoiceAsync(closeRequest);

            // Response kontrol
            if (response.TryGetProperty("basarili", out var basariliProp) && basariliProp.GetBoolean())
            {
                result.Success = true;
                result.LucaFaturaId = lucaFaturaId.Value;
                result.Message = $"Fatura başarıyla kapatıldı. Fatura ID: {lucaFaturaId.Value}";
                _logger.LogInformation("Invoice {FaturaId} closed successfully for Order {OrderId}", lucaFaturaId.Value, orderId);
            }
            else
            {
                var mesaj = response.TryGetProperty("mesaj", out var mesajProp) ? mesajProp.GetString() : "Bilinmeyen hata";
                result.Success = false;
                result.Message = $"Fatura kapama hatası: {mesaj}";
                _logger.LogWarning("Failed to close invoice {FaturaId}: {Message}", lucaFaturaId.Value, mesaj);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing invoice for Order {OrderId}", orderId);
            result.Success = false;
            result.Message = $"Hata: {ex.Message}";
            return result;
        }
    }

    #endregion

    #region Fatura Silme

    /// <summary>
    /// Luca'daki faturayı siler (iptal durumunda)
    /// </summary>
    public async Task<OrderSyncResultDto> DeleteInvoiceAsync(int orderId, string orderType)
    {
        var result = new OrderSyncResultDto { OrderId = orderId, OrderType = orderType };

        try
        {
            var lucaFaturaId = await _mappingRepo.GetLucaInvoiceIdByOrderIdAsync(orderId, orderType);
            if (!lucaFaturaId.HasValue)
            {
                result.Success = false;
                result.Message = $"Order için Luca fatura ID bulunamadı: {orderId}";
                return result;
            }

            var deleteRequest = new LucaDeleteInvoiceRequest
            {
                SsFaturaBaslikId = lucaFaturaId.Value
            };

            var response = await _lucaService.DeleteInvoiceAsync(deleteRequest);

            if (response.TryGetProperty("basarili", out var basariliProp) && basariliProp.GetBoolean())
            {
                result.Success = true;
                result.LucaFaturaId = lucaFaturaId.Value;
                result.Message = $"Fatura başarıyla silindi. Fatura ID: {lucaFaturaId.Value}";
                _logger.LogInformation("Invoice {FaturaId} deleted successfully for Order {OrderId}", lucaFaturaId.Value, orderId);
            }
            else
            {
                var mesaj = response.TryGetProperty("mesaj", out var mesajProp) ? mesajProp.GetString() : "Bilinmeyen hata";
                result.Success = false;
                result.Message = $"Fatura silme hatası: {mesaj}";
                _logger.LogWarning("Failed to delete invoice {FaturaId}: {Message}", lucaFaturaId.Value, mesaj);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting invoice for Order {OrderId}", orderId);
            result.Success = false;
            result.Message = $"Hata: {ex.Message}";
            return result;
        }
    }

    #endregion

    #region Batch Sync

    /// <summary>
    /// Bekleyen (synced olmayan) tüm Sales Order'ları Luca'ya gönderir
    /// </summary>
    public async Task<OrderBatchSyncResultDto> SyncPendingSalesOrdersAsync()
    {
        var result = new OrderBatchSyncResultDto();

        try
        {
            var pendingOrders = await _context.Orders
                .Where(o => !o.IsSynced && o.Status == OrderStatus.Delivered)
                .OrderBy(o => o.OrderDate)
                .Take(50) // Batch limit
                .ToListAsync();

            result.TotalCount = pendingOrders.Count;

            foreach (var order in pendingOrders)
            {
                var syncResult = await SyncSalesOrderToLucaAsync(order.Id);
                if (syncResult.Success)
                {
                    result.SuccessCount++;
                }
                else
                {
                    result.FailCount++;
                    result.FailedOrderIds.Add(order.Id);
                }
            }

            result.Message = $"Batch sync tamamlandı. Başarılı: {result.SuccessCount}, Başarısız: {result.FailCount}";
            _logger.LogInformation("Batch sales order sync completed. Success: {Success}, Failed: {Failed}", result.SuccessCount, result.FailCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in batch sales order sync");
            result.Message = $"Batch sync hatası: {ex.Message}";
        }

        return result;
    }

    #endregion

    #region Helpers

    private int? TryParseOrderNo(string orderNo)
    {
        // "SO-1001" gibi formatları handle et
        if (string.IsNullOrEmpty(orderNo))
            return null;

        // Sadece rakamları al
        var digits = new string(orderNo.Where(char.IsDigit).ToArray());
        if (int.TryParse(digits, out int result))
            return result;

        return null; // Luca kendi numara verebilir
    }

    #endregion
}

#region DTOs

/// <summary>
/// Tekil order sync sonucu
/// </summary>
public class OrderSyncResultDto
{
    public int OrderId { get; set; }
    public string OrderType { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public long? LucaFaturaId { get; set; }
}

/// <summary>
/// Toplu order sync sonucu
/// </summary>
public class OrderBatchSyncResultDto
{
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailCount { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<int> FailedOrderIds { get; set; } = new();
}

#endregion
