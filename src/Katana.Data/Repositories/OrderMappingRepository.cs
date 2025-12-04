using Katana.Core.Interfaces;
using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Katana.Data.Repositories;

/// <summary>
/// Katana Order/Invoice ID'lerini Luca kodlarına çeviren repository
/// </summary>
public class OrderMappingRepository : IOrderMappingRepository
{
    private readonly IntegrationDbContext _context;

    public OrderMappingRepository(IntegrationDbContext context)
    {
        _context = context;
    }

    public async Task<string?> GetLucaCariKoduByCustomerIdAsync(int katanaCustomerId)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == katanaCustomerId);
        
        // LucaCode varsa onu kullan, yoksa TaxNo kullan
        return customer?.LucaCode ?? customer?.TaxNo;
    }

    public async Task<string?> GetLucaSupplierKoduBySupplierIdAsync(int katanaSupplierId)
    {
        var supplier = await _context.Suppliers
            .FirstOrDefaultAsync(s => s.Id == katanaSupplierId);
        
        // Supplier'da da benzer mantık: LucaCode veya TaxNo
        return supplier?.TaxNo;
    }

    public async Task<string?> GetLucaStokKoduByProductIdAsync(int katanaProductId)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == katanaProductId);
        
        return product?.SKU;
    }

    public async Task<string?> GetLucaStokKoduBySkuAsync(string sku)
    {
        // SKU zaten Luca stok kodu ile eşleşiyor varsayımı
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.SKU == sku);
        
        return product?.SKU;
    }

    public Task<double> GetTaxRateByIdAsync(int? taxRateId)
    {
        // TaxRate tablosu yoksa varsayılan değer
        return Task.FromResult(0.18); // Varsayılan KDV %18
    }

    public async Task SaveLucaInvoiceIdAsync(int orderId, long lucaFaturaId, string orderType, string? externalOrderId = null)
    {
        // OrderMappings tablosuna kaydet (idempotent - aynı sipariş için sadece bir kez kaydeder)
        var existing = await _context.OrderMappings
            .FirstOrDefaultAsync(m => m.OrderId == orderId && m.EntityType == orderType);
        
        if (existing == null)
        {
            _context.OrderMappings.Add(new Katana.Data.Models.OrderMapping
            {
                OrderId = orderId,
                LucaInvoiceId = lucaFaturaId,
                EntityType = orderType,
                ExternalOrderId = externalOrderId,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }
        // Zaten varsa hiçbir şey yapma (idempotency)
    }

    public async Task<long?> GetLucaInvoiceIdByOrderIdAsync(int orderId, string orderType)
    {
        // OrderMappings tablosundan çek
        var mapping = await _context.OrderMappings
            .FirstOrDefaultAsync(m => m.OrderId == orderId && m.EntityType == orderType);
        
        return mapping?.LucaInvoiceId;
    }

    public async Task UpdateLucaInvoiceIdAsync(int orderId, long lucaFaturaId, string orderType, string? externalOrderId = null)
    {
        // Mevcut mapping'i güncelle (sipariş Luca'da güncellenmiş se)
        var existing = await _context.OrderMappings
            .FirstOrDefaultAsync(m => m.OrderId == orderId && m.EntityType == orderType);
        
        if (existing != null)
        {
            existing.LucaInvoiceId = lucaFaturaId;
            if (!string.IsNullOrEmpty(externalOrderId))
            {
                existing.ExternalOrderId = externalOrderId;
            }
            existing.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
        else
        {
            // Yoksa yeni oluştur
            await SaveLucaInvoiceIdAsync(orderId, lucaFaturaId, orderType, externalOrderId);
        }
    }

    public async Task<long> GetBelgeTurDetayIdAsync(bool isSalesOrder)
    {
        // Luca Belge Tür ID'leri sabit
        // Satış Faturası: 21
        // Alım Faturası: 41
        return await Task.FromResult(isSalesOrder ? 21L : 41L);
    }

    public async Task<long> GetPaymentBelgeTurDetayIdAsync(bool isSalesOrder)
    {
        // Tahsilat: 11
        // Tediye: 31
        return await Task.FromResult(isSalesOrder ? 11L : 31L);
    }

    public async Task<string> GetDefaultCashAccountCodeAsync()
    {
        return await Task.FromResult("100"); // Varsayılan kasa hesap kodu
    }
}
