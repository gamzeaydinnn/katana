using Katana.Core.DTOs;
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

    public async Task<string?> GetLucaCariKoduByCustomerIdAsync(string katanaCustomerId)
    {
        if (long.TryParse(katanaCustomerId, out var customerIdLong))
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == customerIdLong);
            
            // LucaCode varsa onu kullan, yoksa TaxNo kullan
            return customer?.LucaCode ?? customer?.TaxNo;
        }
        
        return null;
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

    public async Task SaveLucaInvoiceIdAsync(
        int orderId,
        long lucaFaturaId,
        string orderType,
        string? externalOrderId = null,
        string? belgeSeri = null,
        string? belgeNo = null,
        string? belgeTakipNo = null)
    {
        var existing = await GetEntityMappingAsync(orderId, orderType);

        if (existing == null)
        {
            _context.OrderMappings.Add(new Katana.Data.Models.OrderMapping
            {
                OrderId = orderId,
                LucaInvoiceId = lucaFaturaId,
                EntityType = orderType,
                ExternalOrderId = externalOrderId,
                BelgeSeri = belgeSeri,
                BelgeNo = belgeNo,
                BelgeTakipNo = belgeTakipNo,
                CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            existing.LucaInvoiceId = lucaFaturaId;
            existing.ExternalOrderId = externalOrderId ?? existing.ExternalOrderId;
            existing.BelgeSeri = belgeSeri ?? existing.BelgeSeri;
            existing.BelgeNo = belgeNo ?? existing.BelgeNo;
            existing.BelgeTakipNo = belgeTakipNo ?? existing.BelgeTakipNo;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<long?> GetLucaInvoiceIdByOrderIdAsync(int orderId, string orderType)
    {
        var mapping = await GetEntityMappingAsync(orderId, orderType);
        return mapping?.LucaInvoiceId;
    }

    public async Task UpdateLucaInvoiceIdAsync(int orderId, long lucaFaturaId, string orderType, string? externalOrderId = null)
    {
        // Mevcut mapping'i güncelle (sipariş Luca'da güncellenmiş se)
        var existing = await GetEntityMappingAsync(orderId, orderType);
        
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

    public async Task<OrderMappingInfo?> GetMappingInfoAsync(int orderId, string orderType)
    {
        var mapping = await GetEntityMappingAsync(orderId, orderType);
        if (mapping == null)
        {
            return null;
        }

        return new OrderMappingInfo
        {
            BelgeSeri = mapping.BelgeSeri,
            BelgeNo = mapping.BelgeNo,
            BelgeTakipNo = mapping.BelgeTakipNo
        };
    }

    public async Task UpsertMappingInfoAsync(
        int orderId,
        string entityType,
        string? externalOrderId,
        string belgeSeri,
        string belgeNo,
        string belgeTakipNo,
        CancellationToken ct)
    {
        var existing = await _context.OrderMappings
            .FirstOrDefaultAsync(m => m.OrderId == orderId && m.EntityType == entityType, ct);

        if (existing == null)
        {
            _context.OrderMappings.Add(new Katana.Data.Models.OrderMapping
            {
                OrderId = orderId,
                EntityType = entityType,
                ExternalOrderId = externalOrderId,
                BelgeSeri = belgeSeri,
                BelgeNo = belgeNo,
                BelgeTakipNo = belgeTakipNo,
                LucaInvoiceId = 0, // Henüz senkronlanmadı
                SyncStatus = "PENDING",
                CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            // ⚠️ KRİTİK: SYNCED kayıtlara asla dokunma!
            if (existing.SyncStatus == "SYNCED")
            {
                return; // SYNCED kayıt → güncelleme yapma
            }

            // Sadece boş alanları güncelle (override etme)
            if (string.IsNullOrWhiteSpace(existing.BelgeSeri))
                existing.BelgeSeri = belgeSeri;
            
            if (string.IsNullOrWhiteSpace(existing.BelgeNo))
                existing.BelgeNo = belgeNo;
            
            if (string.IsNullOrWhiteSpace(existing.BelgeTakipNo))
                existing.BelgeTakipNo = belgeTakipNo;
            
            if (!string.IsNullOrWhiteSpace(externalOrderId))
                existing.ExternalOrderId = externalOrderId;

            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(ct);
    }

    private async Task<Katana.Data.Models.OrderMapping?> GetEntityMappingAsync(int orderId, string orderType)
    {
        return await _context.OrderMappings
            .FirstOrDefaultAsync(m => m.OrderId == orderId && m.EntityType == orderType);
    }
    
    public async Task<OrderMappingInfo?> GetByExternalOrderIdAsync(string externalOrderId, string orderType = "SalesOrder")
    {
        if (string.IsNullOrWhiteSpace(externalOrderId))
            return null;
            
        var mapping = await _context.OrderMappings
            .FirstOrDefaultAsync(m => m.ExternalOrderId == externalOrderId && m.EntityType == orderType);
            
        if (mapping == null)
            return null;
            
        return new OrderMappingInfo
        {
            BelgeSeri = mapping.BelgeSeri,
            BelgeNo = mapping.BelgeNo,
            BelgeTakipNo = mapping.BelgeTakipNo,
            LucaInvoiceId = mapping.LucaInvoiceId
        };
    }
}
