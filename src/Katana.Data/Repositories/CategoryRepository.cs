using Katana.Core.Entities;
using Katana.Core.Interfaces;
using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Katana.Data.Repositories;

public class CategoryRepository : IRepository<Category>
{
    private readonly IntegrationDbContext _context;
    private readonly DbSet<Category> _dbSet;

    public CategoryRepository(IntegrationDbContext context)
    {
        _context = context;
        _dbSet = context.Set<Category>();
    }

    public async Task<IEnumerable<Category>> GetAllAsync()
        => await _dbSet.Include(c => c.Children).Include(c => c.Products).ToListAsync();

    public async Task<Category?> GetByIdAsync(int id)
        => await _dbSet.Include(c => c.Children).FirstOrDefaultAsync(c => c.Id == id);

    public async Task AddAsync(Category entity)
    {
        await _dbSet.AddAsync(entity);
    }

    public void Update(Category entity)
    {
        _dbSet.Update(entity);
    }

    public void Delete(Category entity)
    {
        _dbSet.Remove(entity);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    Task<List<Category>> IRepository<Category>.GetAllAsync()
    {
        throw new NotImplementedException();
    }

    Task<Category> IRepository<Category>.AddAsync(Category entity)
    {
        throw new NotImplementedException();
    }

    public Task<Category> UpdateAsync(Category entity)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(int id)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ExistsAsync(int id)
    {
        throw new NotImplementedException();
    }
}
