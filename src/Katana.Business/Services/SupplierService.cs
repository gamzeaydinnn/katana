using Katana.Core.DTOs;
using Katana.Core.Entities;
using Katana.Core.Helpers;
using Katana.Business.Interfaces;
using Katana.Core.Interfaces;
using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Katana.Business.Services;

public class SupplierService : ISupplierService
{
    private readonly IntegrationDbContext _context;
    private readonly IKatanaService _katanaService;

    public SupplierService(IntegrationDbContext context, IKatanaService katanaService)
    {
        _context = context;
        _katanaService = katanaService;
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
                    string.Equals(s.Code, katanaCode, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrWhiteSpace(ks.TaxNo) && !string.IsNullOrWhiteSpace(s.TaxNo) && s.TaxNo == ks.TaxNo));

                if (existing == null)
                {
                    var entity = MappingHelper.MapToSupplier(ks);
                    entity.CreatedAt = DateTime.UtcNow;
                    entity.UpdatedAt = DateTime.UtcNow;
                    _context.Suppliers.Add(entity);
                    existingSuppliers.Add(entity);
                    result.Created++;
                }
                else
                {
                    existing.Code = katanaCode;
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
                result.Errors.Add($"{ks.Id}: {ex.Message}");
                result.Skipped++;
            }
        }

        await _context.SaveChangesAsync(ct);
        return result;
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
