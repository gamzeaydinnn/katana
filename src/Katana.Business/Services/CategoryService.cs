using Katana.Core.DTOs;
using Katana.Core.Entities;
using Katana.Core.Interfaces;
using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;


namespace Katana.Business.Services;

public class CategoryService : ICategoryService
{
    private readonly IRepository<Category> _repository;
    private readonly IntegrationDbContext _context;

    public CategoryService(IRepository<Category> repository, IntegrationDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    public async Task<IEnumerable<CategoryDto>> GetAllAsync()
    {
        var categories = await _context.Categories
            .AsNoTracking()
            .Include(c => c.Children)
            .OrderBy(c => c.Name)
            .ToListAsync();

        var productCounts = await _context.Products
            .AsNoTracking()
            .GroupBy(p => p.CategoryId)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Count);

        return categories.Select(c => new CategoryDto
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            ParentId = c.ParentId,
            IsActive = c.IsActive,
            ProductCount = productCounts.TryGetValue(c.Id, out var cnt) ? cnt : 0,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        });
    }

    public async Task<CategoryDto?> GetByIdAsync(int id)
    {
        var category = await _context.Categories
            .AsNoTracking()
            .Include(c => c.Children)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (category == null) return null;

        var productCount = await _context.Products.CountAsync(p => p.CategoryId == id);

        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            ParentId = category.ParentId,
            IsActive = category.IsActive,
            ProductCount = productCount,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        };
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryDto dto)
    {
        // uniqueness within the same parent
        var exists = await _context.Categories.AnyAsync(c => c.ParentId == dto.ParentId && c.Name == dto.Name);
        if (exists)
            throw new InvalidOperationException("Aynı seviyede aynı isimde kategori mevcut.");

        var entity = new Category
        {
            Name = dto.Name,
            Description = dto.Description,
            ParentId = dto.ParentId,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Categories.Add(entity);
        await _context.SaveChangesAsync();

        return new CategoryDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            ParentId = entity.ParentId,
            IsActive = entity.IsActive,
            ProductCount = 0,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    public async Task<CategoryDto> UpdateAsync(int id, UpdateCategoryDto dto)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null)
            throw new KeyNotFoundException("Category not found");

        // check uniqueness within same parent (excluding current)
        var exists = await _context.Categories.AnyAsync(c => c.ParentId == dto.ParentId && c.Name == dto.Name && c.Id != id);
        if (exists)
            throw new InvalidOperationException("Aynı seviyede aynı isimde kategori mevcut.");

        category.Name = dto.Name;
        category.Description = dto.Description;
        category.ParentId = dto.ParentId;
        category.IsActive = dto.IsActive;
        category.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var productCount = await _context.Products.CountAsync(p => p.CategoryId == id);
        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            ParentId = category.ParentId,
            IsActive = category.IsActive,
            ProductCount = productCount,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        };
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null) return false;

        var hasChildren = await _context.Categories.AnyAsync(c => c.ParentId == id);
        var hasProducts = await _context.Products.AnyAsync(p => p.CategoryId == id);

        if (hasChildren || hasProducts)
            throw new InvalidOperationException("Alt kategori veya ürün bağlı olduğu için kategori silinemez.");

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ActivateAsync(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null) return false;
        if (!category.IsActive)
        {
            category.IsActive = true;
            category.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
        return true;
    }

    public async Task<bool> DeactivateAsync(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null) return false;
        if (category.IsActive)
        {
            category.IsActive = false;
            category.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
        return true;
    }
}
