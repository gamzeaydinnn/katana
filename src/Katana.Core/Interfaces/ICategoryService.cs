using Katana.Core.DTOs;

namespace Katana.Core.Interfaces;

public interface ICategoryService
{
    Task<IEnumerable<CategoryDto>> GetAllAsync();
    Task<CategoryDto?> GetByIdAsync(int id);
    Task<CategoryDto> CreateAsync(CategoryDto dto);
    Task<CategoryDto> UpdateAsync(int id, CategoryDto dto);
    Task<bool> DeleteAsync(int id);
}
