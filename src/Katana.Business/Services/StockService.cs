using Katana.Core.DTOs;
using Katana.Core.Entities;
using Katana.Core.Interfaces;
using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Katana.Business.Services;




public class StockService : IStockService
{
    private readonly IntegrationDbContext _context;
    private readonly ILogger<StockService> _logger;

    public StockService(IntegrationDbContext context, ILogger<StockService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<StockDto>> GetAllStockMovementsAsync()
    {
        try
        {
            var stockMovements = await _context.Stocks
                .Include(s => s.Product)
                .OrderByDescending(s => s.Timestamp)
                .ToListAsync();

            return stockMovements.Select(MapToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all stock movements");
            throw;
        }
    }

    public async Task<List<StockDto>> GetStockMovementsByProductIdAsync(int productId)
    {
        try
        {
            var stockMovements = await _context.Stocks
                .Include(s => s.Product)
                .Where(s => s.ProductId == productId)
                .OrderByDescending(s => s.Timestamp)
                .ToListAsync();

            return stockMovements.Select(MapToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock movements for product {ProductId}", productId);
            throw;
        }
    }

    public async Task<List<StockDto>> GetStockMovementsByLocationAsync(string location)
    {
        try
        {
            var stockMovements = await _context.Stocks
                .Include(s => s.Product)
                .Where(s => s.Location == location)
                .OrderByDescending(s => s.Timestamp)
                .ToListAsync();

            return stockMovements.Select(MapToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock movements for location {Location}", location);
            throw;
        }
    }

    public async Task<List<StockDto>> GetStockMovementsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            var stockMovements = await _context.Stocks
                .Include(s => s.Product)
                .Where(s => s.Timestamp >= startDate && s.Timestamp <= endDate)
                .OrderByDescending(s => s.Timestamp)
                .ToListAsync();

            return stockMovements.Select(MapToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock movements for date range");
            throw;
        }
    }

    public async Task<StockDto> CreateStockMovementAsync(CreateStockMovementDto dto)
    {
        try
        {
            var product = await _context.Products.FindAsync(dto.ProductId);
            if (product == null)
            {
                throw new ArgumentException($"Product with ID {dto.ProductId} not found");
            }

            var stock = new Stock
            {
                ProductId = dto.ProductId,
                Location = dto.Location,
                Quantity = dto.Quantity,
                Type = dto.Type,
                Reason = dto.Reason,
                Reference = dto.Reference,
                Timestamp = DateTime.UtcNow,
                IsSynced = false
            };

            _context.Stocks.Add(stock);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created stock movement {Id} for product {ProductId}", stock.Id, dto.ProductId);

            return MapToDto(stock);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating stock movement");
            throw;
        }
    }

    public async Task<bool> DeleteStockMovementAsync(int id)
    {
        try
        {
            var stock = await _context.Stocks.FindAsync(id);
            if (stock == null)
            {
                return false;
            }

            _context.Stocks.Remove(stock);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted stock movement {Id}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting stock movement {Id}", id);
            throw;
        }
    }

    public async Task<List<StockSummaryDto>> GetStockSummaryAsync()
    {
        try
        {
            var products = await _context.Products
                .Include(p => p.Stocks)
                .Include(p => p.StockMovements)
                .ToListAsync();

            var summaries = products.Select(p => new StockSummaryDto
            {
                ProductId = p.Id,
                ProductName = p.Name,
                ProductSKU = p.SKU,
                TotalStock = p.Stock,
                StockByLocation = p.Stocks
                    .GroupBy(s => s.Location)
                    .ToDictionary(g => g.Key, g => g.Sum(s => s.Type == "IN" ? s.Quantity : -s.Quantity)),
                LastMovement = p.Stocks.Any()
                    ? p.Stocks.Max(s => s.Timestamp)
                    : DateTime.MinValue
            }).ToList();

            return summaries;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock summary");
            throw;
        }
    }

    public async Task<StockSummaryDto?> GetStockSummaryByProductIdAsync(int productId)
    {
        try
        {
            var product = await _context.Products
                .Include(p => p.Stocks)
                .Include(p => p.StockMovements)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
            {
                return null;
            }

            return new StockSummaryDto
            {
                ProductId = product.Id,
                ProductName = product.Name,
                ProductSKU = product.SKU,
                TotalStock = product.Stock,
                StockByLocation = product.Stocks
                    .GroupBy(s => s.Location)
                    .ToDictionary(g => g.Key, g => g.Sum(s => s.Type == "IN" ? s.Quantity : -s.Quantity)),
                LastMovement = product.Stocks.Any()
                    ? product.Stocks.Max(s => s.Timestamp)
                    : DateTime.MinValue
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock summary for product {ProductId}", productId);
            throw;
        }
    }

    public async Task<List<StockDto>> GetUnsyncedStockMovementsAsync()
    {
        try
        {
            var stockMovements = await _context.Stocks
                .Include(s => s.Product)
                .Where(s => !s.IsSynced)
                .OrderByDescending(s => s.Timestamp)
                .ToListAsync();

            return stockMovements.Select(MapToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unsynced stock movements");
            throw;
        }
    }

    public async Task<bool> MarkAsSyncedAsync(int id)
    {
        try
        {
            var stock = await _context.Stocks.FindAsync(id);
            if (stock == null)
            {
                return false;
            }

            stock.IsSynced = true;
            stock.SyncedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Marked stock movement {Id} as synced", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking stock movement {Id} as synced", id);
            throw;
        }
    }

    private StockDto MapToDto(Stock stock)
    {
        return new StockDto
        {
            Id = stock.Id,
            ProductId = stock.ProductId,
            ProductName = stock.Product?.Name ?? "",
            ProductSKU = stock.Product?.SKU ?? "",
            Location = stock.Location,
            Quantity = stock.Quantity,
            Type = stock.Type,
            Reason = stock.Reason,
            Timestamp = stock.Timestamp,
            Reference = stock.Reference,
            IsSynced = stock.IsSynced,
            SyncedAt = stock.SyncedAt
        };
    }
}
