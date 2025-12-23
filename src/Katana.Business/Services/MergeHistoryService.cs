using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Katana.Data.Context;
using Katana.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Katana.Business.Services;

/// <summary>
/// Service for managing merge history
/// </summary>
public class MergeHistoryService : IMergeHistoryService
{
    private readonly IntegrationDbContext _context;
    private readonly ILogger<MergeHistoryService> _logger;

    public MergeHistoryService(
        IntegrationDbContext context,
        ILogger<MergeHistoryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new merge history entry
    /// Requirements: 7.1
    /// </summary>
    public async Task<int> CreateMergeHistoryAsync(MergeHistoryEntry entry)
    {
        _logger.LogInformation("Creating merge history entry");

        var history = new MergeHistory
        {
            CanonicalProductId = entry.CanonicalProductId,
            CanonicalProductName = entry.CanonicalProductName,
            CanonicalProductSKU = entry.CanonicalProductSKU,
            MergedProductIds = entry.MergedProductIds,
            SalesOrdersUpdated = entry.SalesOrdersUpdated,
            BOMsUpdated = entry.BOMsUpdated,
            StockMovementsUpdated = entry.StockMovementsUpdated,
            AdminUserId = entry.AdminUserId,
            AdminUserName = entry.AdminUserName,
            Status = entry.Status,
            Reason = entry.Reason
        };

        _context.MergeHistories.Add(history);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Merge history entry created with ID {HistoryId}", history.Id);

        return history.Id;
    }

    /// <summary>
    /// Gets merge history with filters
    /// Requirements: 7.2, 7.3
    /// </summary>
    public async Task<List<MergeHistoryEntry>> GetMergeHistoryAsync(MergeHistoryFilter? filter = null)
    {
        _logger.LogInformation("Getting merge history");

        var query = _context.MergeHistories
            .Include(m => m.CanonicalProduct)
            .AsQueryable();

        if (filter != null)
        {
            if (filter.StartDate.HasValue)
            {
                query = query.Where(m => m.CreatedAt >= filter.StartDate.Value);
            }

            if (filter.EndDate.HasValue)
            {
                query = query.Where(m => m.CreatedAt <= filter.EndDate.Value);
            }

            if (!string.IsNullOrEmpty(filter.AdminUserId))
            {
                query = query.Where(m => m.AdminUserId == filter.AdminUserId);
            }

            if (filter.Status.HasValue)
            {
                query = query.Where(m => m.Status == filter.Status.Value);
            }
        }

        var histories = await query
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();

        return histories.Select(h => new MergeHistoryEntry
        {
            Id = h.Id,
            CanonicalProductId = h.CanonicalProductId,
            CanonicalProductName = h.CanonicalProductName,
            CanonicalProductSKU = h.CanonicalProductSKU,
            MergedProductIds = h.MergedProductIds,
            SalesOrdersUpdated = h.SalesOrdersUpdated,
            BOMsUpdated = h.BOMsUpdated,
            StockMovementsUpdated = h.StockMovementsUpdated,
            AdminUserId = h.AdminUserId,
            AdminUserName = h.AdminUserName,
            CreatedAt = h.CreatedAt,
            Status = h.Status,
            Reason = h.Reason
        }).ToList();
    }

    /// <summary>
    /// Gets detailed information about a specific merge history entry
    /// Requirements: 7.2
    /// </summary>
    public async Task<MergeHistoryEntry?> GetMergeHistoryDetailAsync(int mergeHistoryId)
    {
        _logger.LogInformation("Getting merge history detail for {HistoryId}", mergeHistoryId);

        var history = await _context.MergeHistories
            .Include(m => m.CanonicalProduct)
            .FirstOrDefaultAsync(m => m.Id == mergeHistoryId);

        if (history == null)
        {
            return null;
        }

        return new MergeHistoryEntry
        {
            Id = history.Id,
            CanonicalProductId = history.CanonicalProductId,
            CanonicalProductName = history.CanonicalProductName,
            CanonicalProductSKU = history.CanonicalProductSKU,
            MergedProductIds = history.MergedProductIds,
            SalesOrdersUpdated = history.SalesOrdersUpdated,
            BOMsUpdated = history.BOMsUpdated,
            StockMovementsUpdated = history.StockMovementsUpdated,
            AdminUserId = history.AdminUserId,
            AdminUserName = history.AdminUserName,
            CreatedAt = history.CreatedAt,
            Status = history.Status,
            Reason = history.Reason
        };
    }

    /// <summary>
    /// Updates merge history status
    /// Requirements: 7.5
    /// </summary>
    public async Task UpdateMergeHistoryStatusAsync(int mergeHistoryId, MergeStatus status)
    {
        _logger.LogInformation("Updating merge history {HistoryId} status to {Status}", mergeHistoryId, status);

        var history = await _context.MergeHistories.FindAsync(mergeHistoryId);
        if (history == null)
        {
            _logger.LogWarning("Merge history {HistoryId} not found", mergeHistoryId);
            return;
        }

        history.Status = status;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Merge history {HistoryId} status updated", mergeHistoryId);
    }
}
