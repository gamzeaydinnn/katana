using Katana.Business.Interfaces;
using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Katana.Infrastructure.Repositories;

/// <summary>
/// Katana ID'lerini Luca kodlarına çeviren mapping repository.
/// Bu implementasyon şu an basit fallback mantığı kullanıyor.
/// Gerçek projede:
/// 1. Ayrı bir mapping tablosu oluşturulmalı (CustomerLucaMapping, ProductLucaMapping, vs.)
/// 2. Bu tablo üzerinden ID/Kod eşleşmeleri çekilmeli
/// </summary>
public class OrderMappingRepository : IOrderMappingRepository
{
    private readonly IntegrationDbContext _context;
    private readonly ILogger<OrderMappingRepository> _logger;

    // Varsayılan değerler - Gerçek projede config veya DB'den gelmeli
    private const double DEFAULT_KDV_ORANI = 20.0;
    private const long DEFAULT_SATIS_BELGE_TUR_DETAY_ID = 1;   // Satış Faturası
    private const long DEFAULT_ALIM_BELGE_TUR_DETAY_ID = 2;    // Alım Faturası
    private const long DEFAULT_TAHSILAT_BELGE_TUR_ID = 100;    // Tahsilat
    private const long DEFAULT_TEDIYE_BELGE_TUR_ID = 200;      // Tediye
    private const string DEFAULT_KASA_HESAP_KODU = "100.01.001"; // Merkez Kasa

    public OrderMappingRepository(IntegrationDbContext context, ILogger<OrderMappingRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<string?> GetLucaCariKoduByCustomerIdAsync(int katanaCustomerId)
    {
        try
        {
            // Önce Customer tablosundan direkt al (eğer LucaCode alanı varsa)
            var customer = await _context.Customers.FindAsync(katanaCustomerId);
            if (customer != null)
            {
                // Eğer Customer entity'sinde LucaCode gibi bir alan varsa onu kullan
                // Yoksa fallback pattern kullan
                return $"120.01.{katanaCustomerId:D5}"; // Luca muhasebe hesap formatı
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting Luca cari kodu for Customer {CustomerId}", katanaCustomerId);
            return null;
        }
    }

    public async Task<string?> GetLucaSupplierKoduBySupplierIdAsync(int katanaSupplierId)
    {
        try
        {
            var supplier = await _context.Suppliers.FindAsync(katanaSupplierId);
            if (supplier != null)
            {
                return $"320.01.{katanaSupplierId:D5}"; // Tedarikçi hesap formatı
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting Luca supplier kodu for Supplier {SupplierId}", katanaSupplierId);
            return null;
        }
    }

    public async Task<string?> GetLucaStokKoduByProductIdAsync(int katanaProductId)
    {
        try
        {
            var product = await _context.Products.FindAsync(katanaProductId);
            if (product != null)
            {
                // Ürün SKU'su varsa onu kullan, yoksa ID bazlı kod oluştur
                return !string.IsNullOrEmpty(product.SKU) 
                    ? product.SKU 
                    : $"STK-{katanaProductId:D6}";
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting Luca stok kodu for Product {ProductId}", katanaProductId);
            return null;
        }
    }

    public async Task<string?> GetLucaStokKoduBySkuAsync(string sku)
    {
        // SKU direkt olarak Luca stok kodu olarak kullanılabilir
        // Eğer dönüşüm gerekiyorsa burada yapılabilir
        return await Task.FromResult(sku);
    }

    public Task<double> GetTaxRateByIdAsync(int? taxRateId)
    {
        // Gerçek projede: Tax tablosundan oran çekilmeli
        // Şimdilik varsayılan KDV oranı
        if (taxRateId == null)
            return Task.FromResult(DEFAULT_KDV_ORANI);

        // Örnek tax rate mapping
        var rate = taxRateId switch
        {
            1 => 0.0,    // KDV'siz
            2 => 1.0,    // %1
            3 => 10.0,   // %10
            4 => 20.0,   // %20
            _ => DEFAULT_KDV_ORANI
        };

        return Task.FromResult(rate);
    }

    public async Task SaveLucaInvoiceIdAsync(int orderId, long lucaFaturaId, string orderType)
    {
        try
        {
            // OrderLucaMapping gibi bir tablo oluşturulmalı
            // Şimdilik log
            _logger.LogInformation("Saving Luca Invoice mapping: OrderId={OrderId}, LucaFaturaId={FaturaId}, Type={Type}",
                orderId, lucaFaturaId, orderType);

            // TODO: Gerçek implementasyon için OrderLucaMapping entity ve DbSet oluşturulmalı
            // var mapping = new OrderLucaMapping
            // {
            //     OrderId = orderId,
            //     OrderType = orderType,
            //     LucaFaturaId = lucaFaturaId,
            //     CreatedAt = DateTime.UtcNow
            // };
            // _context.OrderLucaMappings.Add(mapping);
            // await _context.SaveChangesAsync();

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving Luca invoice mapping for Order {OrderId}", orderId);
            throw;
        }
    }

    public async Task<long?> GetLucaInvoiceIdByOrderIdAsync(int orderId, string orderType)
    {
        try
        {
            // TODO: Gerçek implementasyon
            // var mapping = await _context.OrderLucaMappings
            //     .FirstOrDefaultAsync(m => m.OrderId == orderId && m.OrderType == orderType);
            // return mapping?.LucaFaturaId;

            return await Task.FromResult<long?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting Luca invoice ID for Order {OrderId}", orderId);
            return null;
        }
    }

    public Task<long> GetBelgeTurDetayIdAsync(bool isSalesOrder)
    {
        // Luca'dan alınan Belge Tür Detay ID'leri
        // Gerçek projede: Luca API'den çekilen veya config'den okunan değerler
        return Task.FromResult(isSalesOrder 
            ? DEFAULT_SATIS_BELGE_TUR_DETAY_ID 
            : DEFAULT_ALIM_BELGE_TUR_DETAY_ID);
    }

    public Task<long> GetPaymentBelgeTurDetayIdAsync(bool isSalesOrder)
    {
        // Satış için Tahsilat, Alım için Tediye belge türü
        return Task.FromResult(isSalesOrder 
            ? DEFAULT_TAHSILAT_BELGE_TUR_ID 
            : DEFAULT_TEDIYE_BELGE_TUR_ID);
    }

    public Task<string> GetDefaultCashAccountCodeAsync()
    {
        // Varsayılan kasa hesap kodu
        // Gerçek projede: Config veya settings'den gelmeli
        return Task.FromResult(DEFAULT_KASA_HESAP_KODU);
    }
}
