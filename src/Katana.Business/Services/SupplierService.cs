using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Katana.Core.Entities;
using Katana.Core.Helpers;
using Katana.Core.Interfaces;
using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Katana.Business.Services;

public class SupplierService : ISupplierService
{
    private readonly IntegrationDbContext _context;
    private readonly IKatanaService _katanaService;
    private readonly ILogger<SupplierService> _logger;

    public SupplierService(
        IntegrationDbContext context,
        IKatanaService katanaService,
        ILogger<SupplierService> logger)
    {
        _context = context;
        _katanaService = katanaService;
        _logger = logger;
    }

    public async Task<IEnumerable<SupplierDto>> GetAllAsync()
    {
        var suppliers = await _context.Suppliers
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .ToListAsync();
        return suppliers.Select(MapToDto);
    }

    public async Task<SupplierDto?> GetByIdAsync(int id)
    {
        var supplier = await _context.Suppliers.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
        if (supplier == null) return null;
        return MapToDto(supplier);
    }

    public async Task<SupplierDto> CreateAsync(CreateSupplierDto dto)
    {
        var entity = new Supplier
        {
            Name = dto.Name,
            ContactName = dto.ContactName,
            Email = dto.Email,
            Phone = dto.Phone,
            Address = dto.Address,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Suppliers.Add(entity);
        await _context.SaveChangesAsync();

        return MapToDto(entity);
    }

    public async Task<SupplierDto> UpdateAsync(int id, UpdateSupplierDto dto)
    {
        var supplier = await _context.Suppliers.FindAsync(id);
        if (supplier == null)
            throw new KeyNotFoundException("Supplier not found");

        supplier.Name = dto.Name;
        supplier.ContactName = dto.ContactName;
        supplier.Email = dto.Email;
        supplier.Phone = dto.Phone;
        supplier.Address = dto.Address;
        supplier.IsActive = dto.IsActive;
        supplier.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToDto(supplier);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var supplier = await _context.Suppliers.FindAsync(id);
        if (supplier == null)
            return false;

        var hasOrders = await _context.PurchaseOrders.AnyAsync(po => po.SupplierId == id);
        if (hasOrders)
            throw new InvalidOperationException("Supplier has related purchase orders and cannot be deleted.");

        _context.Suppliers.Remove(supplier);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ActivateAsync(int id)
    {
        var supplier = await _context.Suppliers.FindAsync(id);
        if (supplier == null) return false;
        if (!supplier.IsActive)
        {
            supplier.IsActive = true;
            supplier.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
        return true;
    }

    public async Task<bool> DeactivateAsync(int id)
    {
        var supplier = await _context.Suppliers.FindAsync(id);
        if (supplier == null) return false;
        if (supplier.IsActive)
        {
            supplier.IsActive = false;
            supplier.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
        return true;
    }

    public async Task<SupplierImportResultDto> ImportFromKatanaAsync(CancellationToken ct = default)
    {
        var result = new SupplierImportResultDto();

        var katanaSuppliers = await _katanaService.GetSuppliersAsync();
        result.TotalFromKatana = katanaSuppliers.Count;

        if (katanaSuppliers.Count == 0)
        {
            return result;
        }

        var existingSuppliers = await _context.Suppliers.ToListAsync(ct);

        foreach (var ks in katanaSuppliers)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                var katanaCode = ks.Id > 0 ? ks.Id.ToString() : null;
                if (string.IsNullOrWhiteSpace(katanaCode))
                {
                    result.Skipped++;
                    continue;
                }

                var existing = existingSuppliers.FirstOrDefault(s =>
                    string.Equals(s.KatanaId, katanaCode, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(s.Code, katanaCode, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrWhiteSpace(ks.TaxNo) && !string.IsNullOrWhiteSpace(s.TaxNo) && s.TaxNo == ks.TaxNo));

                if (existing == null)
                {
                    var entity = MappingHelper.MapToSupplier(ks);
                    entity.KatanaId = katanaCode;
                    entity.Code ??= katanaCode;
                    entity.CreatedAt = DateTime.UtcNow;
                    entity.UpdatedAt = DateTime.UtcNow;
                    _context.Suppliers.Add(entity);
                    existingSuppliers.Add(entity);
                    result.Created++;
                }
                else
                {
                    existing.KatanaId = katanaCode;
                    existing.Code = string.IsNullOrWhiteSpace(existing.Code) ? katanaCode : existing.Code;
                    existing.Name = ks.Name ?? existing.Name;
                    existing.Email = ks.Email;
                    existing.Phone = ks.Phone;
                    existing.TaxNo = ks.TaxNo;
                    existing.Address = ks.Addresses?.FirstOrDefault()?.Line1;
                    existing.City = ks.Addresses?.FirstOrDefault()?.City;
                    existing.IsActive = true;
                    existing.UpdatedAt = DateTime.UtcNow;
                    result.Updated++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Supplier import error for KatanaId {KatanaId}", ks.Id);
                result.Errors.Add($"{ks.Id}: {ex.Message}");
                result.Skipped++;
            }
        }

        await _context.SaveChangesAsync(ct);
        return result;
    }

    // ===== KATANA SYNC OPERATIONS =====
    public async Task<int> SyncFromKatanaAsync()
    {
        try
        {
            _logger.LogInformation("🔄 Katana'dan supplier senkronizasyonu başlatılıyor...");
            
            var katanaSuppliers = await _katanaService.GetSuppliersAsync();
            int importedCount = 0;
            int updatedCount = 0;
            
            foreach (var katanaSupplier in katanaSuppliers)
            {
                var katanaIdStr = katanaSupplier.Id.ToString();
                
                var existing = await _context.Suppliers
                    .FirstOrDefaultAsync(s => s.KatanaId == katanaIdStr);
                
                if (existing == null)
                {
                    var firstAddress = katanaSupplier.Addresses?.FirstOrDefault();
                    var addressStr = firstAddress != null 
                        ? $"{firstAddress.Line1}, {firstAddress.City}, {firstAddress.State} {firstAddress.Zip}".Trim()
                        : null;
                    
                    var newSupplier = new Supplier
                    {
                        KatanaId = katanaIdStr,
                        Name = katanaSupplier.Name ?? "Unknown Supplier",
                        TaxNo = katanaSupplier.TaxNo,
                        Email = katanaSupplier.Email,
                        Phone = katanaSupplier.Phone,
                        Address = addressStr,
                        City = firstAddress?.City,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.Suppliers.Add(newSupplier);
                    importedCount++;
                }
                else
                {
                    existing.Name = katanaSupplier.Name ?? existing.Name;
                    existing.TaxNo = katanaSupplier.TaxNo ?? existing.TaxNo;
                    existing.Email = katanaSupplier.Email ?? existing.Email;
                    existing.Phone = katanaSupplier.Phone ?? existing.Phone;
                    
                    var firstAddress = katanaSupplier.Addresses?.FirstOrDefault();
                    if (firstAddress != null)
                    {
                        existing.Address = $"{firstAddress.Line1}, {firstAddress.City}, {firstAddress.State} {firstAddress.Zip}".Trim();
                        existing.City = firstAddress.City;
                    }
                    
                    existing.UpdatedAt = DateTime.UtcNow;
                    updatedCount++;
                }
            }
            
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("✅ Supplier sync tamamlandı: {Imported} yeni, {Updated} güncellendi", 
                importedCount, updatedCount);
            
            return importedCount + updatedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Supplier sync hatası: {Message}", ex.Message);
            throw;
        }
    }
    
    public async Task<SupplierDto?> GetOrCreateFromKatanaIdAsync(string katanaId)
    {
        try
        {
            var existing = await _context.Suppliers
                .FirstOrDefaultAsync(s => s.KatanaId == katanaId);
            
            if (existing != null)
            {
                return MapToDto(existing);
            }
            
            _logger.LogInformation("🔍 Katana'dan supplier çekiliyor: {KatanaId}", katanaId);
            var katanaSupplier = await _katanaService.GetSupplierByIdAsync(katanaId);
            
            if (katanaSupplier == null)
            {
                _logger.LogWarning("⚠️ Katana'da supplier bulunamadı: {KatanaId}", katanaId);
                return null;
            }
            
            var firstAddress = katanaSupplier.Addresses?.FirstOrDefault();
            var addressStr = firstAddress != null 
                ? $"{firstAddress.Line1}, {firstAddress.City}, {firstAddress.State} {firstAddress.Zip}".Trim()
                : null;
            
            var newSupplier = new Supplier
            {
                KatanaId = katanaSupplier.Id.ToString(),
                Name = katanaSupplier.Name ?? "Unknown Supplier",
                TaxNo = katanaSupplier.TaxNo,
                Email = katanaSupplier.Email,
                Phone = katanaSupplier.Phone,
                Address = addressStr,
                City = firstAddress?.City,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            _context.Suppliers.Add(newSupplier);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("✅ Yeni supplier oluşturuldu: {Name} (KatanaId: {KatanaId})", 
                newSupplier.Name, katanaId);
            
            return MapToDto(newSupplier);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ GetOrCreateFromKatanaId hatası: {KatanaId}", katanaId);
            return null;
        }
    }

    private SupplierDto MapToDto(Supplier s)
    {
        return new SupplierDto
        {
            Id = s.Id,
            Name = s.Name,
            Code = s.Code,
            TaxNo = s.TaxNo,
            LucaCode = s.LucaCode,
            ContactName = s.ContactName,
            Email = s.Email,
            Phone = s.Phone,
            Address = s.Address,
            IsActive = s.IsActive,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt
        };
    }
}
