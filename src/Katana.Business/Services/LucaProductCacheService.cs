using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Katana.Data.Context;
using Katana.Data.Models;
using Katana.Business.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Katana.Business.Services;

public class LucaProductCacheService
{
    private readonly IntegrationDbContext _db;
    private readonly ILucaService _luca;

    public LucaProductCacheService(IntegrationDbContext db, ILucaService luca)
    {
        _db = db;
        _luca = luca;
    }

    // 1) Koza → DB Sync
    public async Task<int> RefreshProductsFromKozaAsync()
    {
        var kozaProducts = await _luca.FetchProductsAsync(System.Threading.CancellationToken.None);

        if (kozaProducts == null || !kozaProducts.Any())
            return 0;

        foreach (var item in kozaProducts)
        {
            var t = item.GetType();
            // Try multiple property names for compatibility
            string code = GetStringProp(t, item, "ProductCode") ?? GetStringProp(t, item, "Code") ?? GetStringProp(t, item, "CodeNumber") ?? GetStringProp(t, item, "StockCode") ?? string.Empty;
            string name = GetStringProp(t, item, "ProductName") ?? GetStringProp(t, item, "Name") ?? GetStringProp(t, item, "StockName") ?? string.Empty;
            string? category = GetStringProp(t, item, "Category") ?? GetStringProp(t, item, "CategoryName");

            if (string.IsNullOrWhiteSpace(code))
                continue;

            var entity = await _db.LucaProducts
                .FirstOrDefaultAsync(x => x.LucaCode == code);

            if (entity == null)
            {
                entity = new LucaProduct
                {
                    LucaCode = code,
                    LucaName = name,
                    LucaCategory = category,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _db.LucaProducts.Add(entity);
            }
            else
            {
                entity.LucaName = name;
                entity.LucaCategory = category;
                entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        return await _db.SaveChangesAsync();
    }

    // 2) Admin panel → DB'den oku
    public async Task<List<LucaProduct>> GetAllCachedAsync()
    {
        return await _db.LucaProducts
            .OrderBy(x => x.LucaCode)
            .ToListAsync();
    }

    private static string? GetStringProp(Type t, object obj, string propName)
    {
        var p = t.GetProperty(propName);
        if (p == null) return null;
        var val = p.GetValue(obj);
        return val?.ToString();
    }
}
