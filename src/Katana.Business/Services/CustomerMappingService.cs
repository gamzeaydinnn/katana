using Katana.Core.Entities;
using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Katana.Business.Services;

/// <summary>
/// Customer → Koza Cari mapping servisi
/// Duplicate customer önleme ve cari kodu yönetimi
/// </summary>
public interface ICustomerMappingService
{
    Task<string> GetCariKoduByCustomerIdAsync(int customerId);
    Task<long?> GetFinansalNesneIdByCustomerIdAsync(int customerId);
    Task<CustomerKozaCariMapping?> GetMappingByCustomerIdAsync(int customerId);
    Task<CustomerKozaCariMapping?> GetMappingByTaxNoAsync(string taxNo);
    Task<CustomerKozaCariMapping?> GetMappingByCariKoduAsync(string cariKodu);
    Task<CustomerKozaCariMapping> CreateOrUpdateMappingAsync(
        int katanaCustomerId,
        string kozaCariKodu,
        long? kozaFinansalNesneId = null,
        string? katanaCustomerName = null,
        string? kozaCariTanim = null,
        string? katanaCustomerTaxNo = null);
    Task<bool> IsDuplicateCustomerAsync(int customerId, string taxNo);
    Task<List<CustomerKozaCariMapping>> GetAllMappingsAsync();
    Task<Dictionary<int, string>> GetCustomerToCariKoduMapAsync();
}

public class CustomerMappingService : ICustomerMappingService
{
    private readonly IntegrationDbContext _context;
    private readonly ILogger<CustomerMappingService> _logger;

    public CustomerMappingService(
        IntegrationDbContext context,
        ILogger<CustomerMappingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Customer ID'ye göre cari kodu al
    /// </summary>
    public async Task<string> GetCariKoduByCustomerIdAsync(int customerId)
    {
        try
        {
            var mapping = await _context.CustomerKozaCariMappings
                .Where(m => m.KatanaCustomerId == customerId)
                .FirstOrDefaultAsync();

            if (mapping != null && !string.IsNullOrWhiteSpace(mapping.KozaCariKodu))
            {
                return mapping.KozaCariKodu;
            }

            // Mapping bulunamadı - Customer entity'den kontrol et
            var customer = await _context.Customers.FindAsync(customerId);
            if (customer != null && !string.IsNullOrWhiteSpace(customer.LucaCode))
            {
                _logger.LogInformation(
                    "Using LucaCode from Customer entity for Customer {CustomerId}: {CariKodu}",
                    customerId, customer.LucaCode);
                return customer.LucaCode;
            }

            // Hiçbir kod bulunamadı - default oluştur
            var defaultCode = $"CK-{customerId}";
            _logger.LogWarning(
                "No mapping found for Customer {CustomerId}. Using default: {CariKodu}",
                customerId, defaultCode);

            return defaultCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cari kodu for Customer {CustomerId}", customerId);
            return $"CK-{customerId}";
        }
    }

    /// <summary>
    /// Customer ID'ye göre finansal nesne ID al
    /// </summary>
    public async Task<long?> GetFinansalNesneIdByCustomerIdAsync(int customerId)
    {
        try
        {
            var mapping = await _context.CustomerKozaCariMappings
                .Where(m => m.KatanaCustomerId == customerId)
                .FirstOrDefaultAsync();

            if (mapping?.KozaFinansalNesneId != null)
            {
                return mapping.KozaFinansalNesneId;
            }

            // Mapping'de yoksa Customer entity'den kontrol et
            var customer = await _context.Customers.FindAsync(customerId);
            return customer?.LucaFinansalNesneId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting finansal nesne ID for Customer {CustomerId}", customerId);
            return null;
        }
    }

    /// <summary>
    /// Customer ID'ye göre mapping al
    /// </summary>
    public async Task<CustomerKozaCariMapping?> GetMappingByCustomerIdAsync(int customerId)
    {
        return await _context.CustomerKozaCariMappings
            .FirstOrDefaultAsync(m => m.KatanaCustomerId == customerId);
    }

    /// <summary>
    /// Vergi numarasına göre mapping al (duplicate kontrolü için)
    /// </summary>
    public async Task<CustomerKozaCariMapping?> GetMappingByTaxNoAsync(string taxNo)
    {
        if (string.IsNullOrWhiteSpace(taxNo))
        {
            return null;
        }

        return await _context.CustomerKozaCariMappings
            .FirstOrDefaultAsync(m => m.KatanaCustomerTaxNo == taxNo);
    }

    /// <summary>
    /// Cari koduna göre mapping al
    /// </summary>
    public async Task<CustomerKozaCariMapping?> GetMappingByCariKoduAsync(string cariKodu)
    {
        if (string.IsNullOrWhiteSpace(cariKodu))
        {
            return null;
        }

        return await _context.CustomerKozaCariMappings
            .FirstOrDefaultAsync(m => m.KozaCariKodu == cariKodu);
    }

    /// <summary>
    /// Mapping oluştur veya güncelle
    /// </summary>
    public async Task<CustomerKozaCariMapping> CreateOrUpdateMappingAsync(
        int katanaCustomerId,
        string kozaCariKodu,
        long? kozaFinansalNesneId = null,
        string? katanaCustomerName = null,
        string? kozaCariTanim = null,
        string? katanaCustomerTaxNo = null)
    {
        if (katanaCustomerId <= 0)
        {
            throw new ArgumentException("Katana Customer ID must be greater than 0", nameof(katanaCustomerId));
        }

        if (string.IsNullOrWhiteSpace(kozaCariKodu))
        {
            throw new ArgumentException("Koza Cari Kodu cannot be null or empty", nameof(kozaCariKodu));
        }

        var existing = await _context.CustomerKozaCariMappings
            .FirstOrDefaultAsync(m => m.KatanaCustomerId == katanaCustomerId);

        if (existing != null)
        {
            // Güncelle
            existing.KozaCariKodu = kozaCariKodu;
            existing.KozaFinansalNesneId = kozaFinansalNesneId;
            existing.KatanaCustomerName = katanaCustomerName ?? existing.KatanaCustomerName;
            existing.KozaCariTanim = kozaCariTanim ?? existing.KozaCariTanim;
            existing.KatanaCustomerTaxNo = katanaCustomerTaxNo ?? existing.KatanaCustomerTaxNo;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.SyncStatus = "PENDING";

            _context.CustomerKozaCariMappings.Update(existing);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated customer mapping: {CustomerId} → {CariKodu} (FinansalNesneId: {FinansalNesneId})",
                katanaCustomerId, kozaCariKodu, kozaFinansalNesneId);

            return existing;
        }
        else
        {
            // Yeni oluştur
            var newMapping = new CustomerKozaCariMapping
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

            _context.CustomerKozaCariMappings.Add(newMapping);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created customer mapping: {CustomerId} → {CariKodu} (FinansalNesneId: {FinansalNesneId})",
                katanaCustomerId, kozaCariKodu, kozaFinansalNesneId);

            return newMapping;
        }
    }

    /// <summary>
    /// Duplicate customer kontrolü (vergi numarasına göre)
    /// </summary>
    public async Task<bool> IsDuplicateCustomerAsync(int customerId, string taxNo)
    {
        if (string.IsNullOrWhiteSpace(taxNo))
        {
            return false;
        }

        var existingMapping = await _context.CustomerKozaCariMappings
            .Where(m => m.KatanaCustomerTaxNo == taxNo && m.KatanaCustomerId != customerId)
            .FirstOrDefaultAsync();

        if (existingMapping != null)
        {
            _logger.LogWarning(
                "Duplicate customer detected! Customer {CustomerId} has same tax no as Customer {ExistingCustomerId}: {TaxNo}",
                customerId, existingMapping.KatanaCustomerId, taxNo);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Tüm mapping'leri getir
    /// </summary>
    public async Task<List<CustomerKozaCariMapping>> GetAllMappingsAsync()
    {
        return await _context.CustomerKozaCariMappings
            .OrderBy(m => m.KatanaCustomerId)
            .ToListAsync();
    }

    /// <summary>
    /// Customer ID → Cari Kodu dictionary'si al
    /// </summary>
    public async Task<Dictionary<int, string>> GetCustomerToCariKoduMapAsync()
    {
        return await _context.CustomerKozaCariMappings
            .Where(m => !string.IsNullOrWhiteSpace(m.KozaCariKodu))
            .ToDictionaryAsync(
                m => m.KatanaCustomerId,
                m => m.KozaCariKodu);
    }
}
