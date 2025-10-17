using Katana.Data.Context;
using Katana.Data.Models;
using Microsoft.EntityFrameworkCore;
//mapping tablolarını DB’de yönetir.
namespace Katana.Data.Repositories
{
    public class MappingRepository
    {
        private readonly IntegrationDbContext _context;

        public MappingRepository(IntegrationDbContext context)
        {
            _context = context;
        }

        // SKU -> Account mapping
        public async Task<Dictionary<string, string>> GetSkuToAccountMappingsAsync()
        {
            return await _context.MappingTables
                .Where(m => m.MappingType == "SKU_ACCOUNT" && m.IsActive)
                .ToDictionaryAsync(m => m.SourceValue, m => m.TargetValue);
        }

        // Location -> Warehouse mapping
        public async Task<Dictionary<string, string>> GetLocationMappingsAsync()
        {
            return await _context.MappingTables
                .Where(m => m.MappingType == "LOCATION_WAREHOUSE" && m.IsActive)
                .ToDictionaryAsync(m => m.SourceValue, m => m.TargetValue);
        }

        // Add or update SKU mapping
        public async Task UpsertSkuMappingAsync(string sku, string accountCode)
        {
            var mapping = await _context.MappingTables
                .FirstOrDefaultAsync(m => m.MappingType == "SKU_ACCOUNT" && m.SourceValue == sku);

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
                    SourceValue = sku,
                    TargetValue = accountCode,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
        }

        // Add or update Location mapping
        public async Task UpsertLocationMappingAsync(string location, string warehouseCode)
        {
            var mapping = await _context.MappingTables
                .FirstOrDefaultAsync(m => m.MappingType == "LOCATION_WAREHOUSE" && m.SourceValue == location);

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
                    SourceValue = location,
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
