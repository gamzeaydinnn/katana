using Katana.Core.DTOs;
using Katana.Core.Entities;
using Katana.Core.Interfaces;
using Microsoft.EntityFrameworkCore;


namespace Katana.Business.Services;

public class CategoryService : ICategoryService
{
    private readonly IRepository<Category> _repository;

    public CategoryService(IRepository<Category> repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<CategoryDto>> GetAllAsync()
    {
        var categories = await _repository.GetAllAsync();
        return categories.Select(c => new CategoryDto
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            ParentId = c.ParentId,
            IsActive = c.IsActive,
            ProductCount = c.Products.Count
        });
    }

    public async Task<CategoryDto?> GetByIdAsync(int id)
    {
        var category = await _repository.GetByIdAsync(id);
        if (category == null) return null;

        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            ParentId = category.ParentId,
            IsActive = category.IsActive,
            ProductCount = category.Products.Count
        };
    }

    public async Task<CategoryDto> CreateAsync(CategoryDto dto)
    {
        var entity = new Category
        {
            Name = dto.Name,
            Description = dto.Description,
            ParentId = dto.ParentId,
            IsActive = dto.IsActive
        };

        await _repository.AddAsync(entity);
        await _repository.SaveChangesAsync();

        dto.Id = entity.Id;
        return dto;
    }

    public async Task<CategoryDto> UpdateAsync(int id, CategoryDto dto)
    {
        var category = await _repository.GetByIdAsync(id);
        if (category == null)
            throw new Exception("Category not found");

        category.Name = dto.Name;
        category.Description = dto.Description;
        category.ParentId = dto.ParentId;
        category.IsActive = dto.IsActive;
        category.UpdatedAt = DateTime.UtcNow;

        _repository.Update(category);
        await _repository.SaveChangesAsync();

        return dto;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var category = await _repository.GetByIdAsync(id);
        if (category == null)
            return false;

        _repository.Delete(category);
        await _repository.SaveChangesAsync();
        return true;
    }
}
