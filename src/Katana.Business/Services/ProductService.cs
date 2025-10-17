using Katana.Core.DTOs;
using Katana.Core.Entities;
using Katana.Core.Interfaces;
using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Katana.Business.Services;

public class ProductService : IProductService
{
    private readonly IntegrationDbContext _context;

    public ProductService(IntegrationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ProductDto>> GetAllProductsAsync()
    {
        var products = await _context.Products
            .OrderBy(p => p.Name)
            .ToListAsync();

        return products.Select(MapToDto);
    }

    public async Task<IEnumerable<ProductSummaryDto>> GetActiveProductsAsync()
    {
        var products = await _context.Products
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync();

        return products.Select(MapToSummaryDto);
    }

    public async Task<ProductDto?> GetProductByIdAsync(int id)
    {
        var product = await _context.Products.FindAsync(id);
        return product == null ? null : MapToDto(product);
    }

    public async Task<ProductDto?> GetProductBySkuAsync(string sku)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.SKU == sku);
        return product == null ? null : MapToDto(product);
    }

    public async Task<IEnumerable<ProductDto>> GetProductsByCategoryAsync(int categoryId)
    {
        var products = await _context.Products
            .Where(p => p.CategoryId == categoryId)
            .OrderBy(p => p.Name)
            .ToListAsync();

        return products.Select(MapToDto);
    }

    public async Task<IEnumerable<ProductDto>> SearchProductsAsync(string searchTerm)
    {
        var products = await _context.Products
            .Where(p => p.Name.Contains(searchTerm) ||
                       p.SKU.Contains(searchTerm) ||
                       (p.Description != null && p.Description.Contains(searchTerm)))
            .OrderBy(p => p.Name)
            .ToListAsync();

        return products.Select(MapToDto);
    }

    public async Task<IEnumerable<ProductDto>> GetLowStockProductsAsync(int threshold = 10)
    {
        var products = await _context.Products
            .Where(p => p.IsActive && p.Stock > 0 && p.Stock <= threshold)
            .OrderBy(p => p.Stock)
            .ToListAsync();

        return products.Select(MapToDto);
    }

    public async Task<IEnumerable<ProductDto>> GetOutOfStockProductsAsync()
    {
        var products = await _context.Products
            .Where(p => p.IsActive && p.Stock == 0)
            .OrderBy(p => p.Name)
            .ToListAsync();

        return products.Select(MapToDto);
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductDto dto)
    {
        var existingProduct = await _context.Products
            .FirstOrDefaultAsync(p => p.SKU == dto.SKU);

        if (existingProduct != null)
            throw new InvalidOperationException($"Bu SKU'ya sahip ürün zaten mevcut: {dto.SKU}");

        var product = new Product
        {
            Name = dto.Name,
            SKU = dto.SKU,
            Price = dto.Price,
            Stock = dto.Stock,
            CategoryId = dto.CategoryId,
            MainImageUrl = dto.MainImageUrl,
            Description = dto.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return MapToDto(product);
    }

    public async Task<ProductDto> UpdateProductAsync(int id, UpdateProductDto dto)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            throw new KeyNotFoundException($"Ürün bulunamadı: {id}");

        var existingProduct = await _context.Products
            .FirstOrDefaultAsync(p => p.SKU == dto.SKU && p.Id != id);

        if (existingProduct != null)
            throw new InvalidOperationException($"Bu SKU'ya sahip başka bir ürün mevcut: {dto.SKU}");

        product.Name = dto.Name;
        product.SKU = dto.SKU;
        product.Price = dto.Price;
        product.Stock = dto.Stock;
        product.CategoryId = dto.CategoryId;
        product.MainImageUrl = dto.MainImageUrl;
        product.Description = dto.Description;
        product.IsActive = dto.IsActive;
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return MapToDto(product);
    }

    public async Task<bool> UpdateStockAsync(int id, int quantity)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return false;

        product.Stock = quantity;
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteProductAsync(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return false;

        var hasStockMovements = await _context.Stocks
            .AnyAsync(s => s.ProductId == id);

        if (hasStockMovements)
            throw new InvalidOperationException("Stok hareketi olan ürün silinemez. Önce ürünü pasif yapın.");

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ActivateProductAsync(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return false;

        product.IsActive = true;
        product.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeactivateProductAsync(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return false;

        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<ProductStatisticsDto> GetProductStatisticsAsync()
    {
        var allProducts = await _context.Products.ToListAsync();

        return new ProductStatisticsDto
        {
            TotalProducts = allProducts.Count,
            ActiveProducts = allProducts.Count(p => p.IsActive),
            InactiveProducts = allProducts.Count(p => !p.IsActive),
            LowStockProducts = allProducts.Count(p => p.IsActive && p.Stock > 0 && p.Stock <= 10),
            OutOfStockProducts = allProducts.Count(p => p.IsActive && p.Stock == 0),
            TotalInventoryValue = allProducts.Where(p => p.IsActive).Sum(p => p.Price * p.Stock)
        };
    }

    private ProductDto MapToDto(Product product)
    {
        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            SKU = product.SKU,
            Price = product.Price,
            Stock = product.Stock,
            CategoryId = product.CategoryId,
            MainImageUrl = product.MainImageUrl,
            Description = product.Description,
            IsActive = product.IsActive,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };
    }

    private ProductSummaryDto MapToSummaryDto(Product product)
    {
        return new ProductSummaryDto
        {
            Id = product.Id,
            Name = product.Name,
            SKU = product.SKU,
            Price = product.Price,
            Stock = product.Stock,
            IsActive = product.IsActive
        };
    }
}
