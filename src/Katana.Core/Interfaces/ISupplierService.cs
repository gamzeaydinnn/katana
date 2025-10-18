using Katana.Core.DTOs;

namespace Katana.Core.Interfaces;

public interface ISupplierService
{
    Task<IEnumerable<SupplierDto>> GetAllAsync();
    Task<SupplierDto?> GetByIdAsync(int id);
    Task<SupplierDto> CreateAsync(CreateSupplierDto dto);
    Task<SupplierDto> UpdateAsync(int id, CreateSupplierDto dto);
    Task<bool> DeleteAsync(int id);
}

