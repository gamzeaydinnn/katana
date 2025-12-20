using Katana.Core.Entities;
using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Katana.Data.Repositories;

public class CategoryRepository : Repository<Category>
{
    public CategoryRepository(IntegrationDbContext context) : base(context)
    {
    }

    public override async Task<List<Category>> GetAllAsync()
    {
        return await _dbSet
            .Include(c => c.Children)
            .ToListAsync();
    }

    public override async Task<Category?> GetByIdAsync(int id)
    {
        return await _dbSet
            .Include(c => c.Children)
            .FirstOrDefaultAsync(c => c.Id == id);
    }
}

