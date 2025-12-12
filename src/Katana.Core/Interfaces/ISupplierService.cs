using Katana.Core.DTOs;

namespace Katana.Core.Interfaces;

public interface ISupplierService
{
    Task<IEnumerable<SupplierDto>> GetAllAsync();
    Task<SupplierDto?> GetByIdAsync(int id);
    Task<SupplierDto> CreateAsync(CreateSupplierDto dto);
    Task<SupplierDto> UpdateAsync(int id, UpdateSupplierDto dto);
    Task<bool> DeleteAsync(int id);
    Task<bool> ActivateAsync(int id);
    Task<bool> DeactivateAsync(int id);
    
    // Katana sync operations
    Task<int> SyncFromKatanaAsync();
    Task<SupplierDto?> GetOrCreateFromKatanaIdAsync(string katanaId);
}
