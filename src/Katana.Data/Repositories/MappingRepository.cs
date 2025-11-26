using Katana.Data.Context;
using Katana.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Katana.Data.Repositories
{
    public class MappingRepository
    {
        private readonly IntegrationDbContext _context;

        public MappingRepository(IntegrationDbContext context)
        {
            _context = context;
        }

        
        public async Task<Dictionary<string, string>> GetSkuToAccountMappingsAsync()
        {
            var list = await _context.MappingTables
                .Where(m => m.MappingType == "SKU_ACCOUNT" && m.IsActive)
                .Select(m => new { m.SourceValue, m.TargetValue })
                .ToListAsync();
            return list.ToDictionary(m => m.SourceValue, m => m.TargetValue, StringComparer.OrdinalIgnoreCase);
        }

        
        public async Task<Dictionary<string, string>> GetLocationMappingsAsync()
        {
            var list = await _context.MappingTables
                .Where(m => m.MappingType == "LOCATION_WAREHOUSE" && m.IsActive)
                .Select(m => new { m.SourceValue, m.TargetValue })
                .ToListAsync();
            return list.ToDictionary(m => m.SourceValue, m => m.TargetValue, StringComparer.OrdinalIgnoreCase);
        }

        
        public async Task UpsertSkuMappingAsync(string sku, string accountCode)
        {
            var normalizedSku = (sku ?? string.Empty).Trim().ToUpperInvariant();
            var mapping = await _context.MappingTables
                .FirstOrDefaultAsync(m => m.MappingType == "SKU_ACCOUNT" && m.SourceValue == normalizedSku);

            if (mapping != null)
            {
                mapping.TargetValue = accountCode;
                mapping.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.MappingTables.Add(new MappingTable
                {
                    MappingType = "SKU_ACCOUNT",
                    SourceValue = normalizedSku,
                    TargetValue = accountCode,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
        }

        
        public async Task UpsertLocationMappingAsync(string location, string warehouseCode)
        {
            var normalizedLocation = (location ?? string.Empty).Trim().ToUpperInvariant();
            var mapping = await _context.MappingTables
                .FirstOrDefaultAsync(m => m.MappingType == "LOCATION_WAREHOUSE" && m.SourceValue == normalizedLocation);

            if (mapping != null)
            {
                mapping.TargetValue = warehouseCode;
                mapping.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.MappingTables.Add(new MappingTable
                {
                    MappingType = "LOCATION_WAREHOUSE",
                    SourceValue = normalizedLocation,
                    TargetValue = warehouseCode,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
        }
    }
}
