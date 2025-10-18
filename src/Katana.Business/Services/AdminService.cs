using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Katana.Business.DTOs;
using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Katana.Core.Entities;

namespace Katana.Business.Services;
public class AdminService : IAdminService
{
    private readonly IntegrationDbContext _context;
    private readonly IKatanaService _katanaService;
    private readonly ILucaService _lucaService;
    private readonly ILogger<AdminService> _logger;

    public AdminService(
        IntegrationDbContext context,
        IKatanaService katanaService,
        ILucaService lucaService,
        ILogger<AdminService> logger)
    {
        _context = context;
        _katanaService = katanaService;
        _lucaService = lucaService;
        _logger = logger;
    }
    public async Task<List<AdminSyncStatusDto>> GetSyncStatusesAsync()
    {
        var statuses = new List<AdminSyncStatusDto>();
        try
        {
            // STOCK / INVOICE / CUSTOMER için son durumları getir
            foreach (var type in new[] { "STOCK", "INVOICE", "CUSTOMER" })
            {
                var lastTime = await _context.SyncOperationLogs
                    .Where(l => l.SyncType == type)
                    .OrderByDescending(l => l.EndTime ?? l.StartTime)
                    .Select(l => (l.EndTime ?? l.StartTime))
                    .FirstOrDefaultAsync();

                var lastStatus = await _context.SyncOperationLogs
                    .Where(l => l.SyncType == type)
                    .OrderByDescending(l => l.EndTime ?? l.StartTime)
                    .Select(l => l.Status)
                    .FirstOrDefaultAsync();

                statuses.Add(new AdminSyncStatusDto
                {
                    IntegrationName = type,               // burada integration yerine syncType gösteriyoruz
                    LastSyncDate = lastTime,
                    Status = lastStatus ?? "Unknown"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching sync statuses");
        }

        return statuses;
    }
    public async Task<List<ErrorLogDto>> GetErrorLogsAsync(int page = 1, int pageSize = 50)
    {
        return await _context.ErrorLogs
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new ErrorLogDto
            {
                Id = l.Id,
                IntegrationName = l.IntegrationName,
                Message = l.Message,
                CreatedAt = l.CreatedAt
            })
            .ToListAsync();
    }

    // integrationName parametresi yerine syncType beklediğimizi dökümante edebilirsin: "STOCK" | "INVOICE" | "CUSTOMER"
    public async Task<SyncReportDto> GetSyncReportAsync(string integrationName)
    {
        var syncType = integrationName?.ToUpperInvariant();

        var total = await _context.SyncOperationLogs.CountAsync(l => l.SyncType == syncType);
        var success = await _context.SyncOperationLogs.CountAsync(l => l.SyncType == syncType && l.Status == "SUCCESS");
         var failed = total - success;

        return new SyncReportDto
        {
            IntegrationName = syncType ?? "UNKNOWN",
            TotalRecords = total,
            SuccessCount = success,
            FailedCount = failed,
            ReportDate = DateTime.UtcNow
        };
    }
    public async Task<bool> RunManualSyncAsync(ManualSyncRequest request)
    {
        try
        {
            // Bu metodu gerçek senaryoya göre zenginleştirebilirsin.
            if (request.IntegrationName.Equals("Katana", StringComparison.OrdinalIgnoreCase))
            {
                var products = await _katanaService.GetProductsAsync();
                return products.Any();
            }
            else if (request.IntegrationName.Equals("Luca", StringComparison.OrdinalIgnoreCase))
            {
                // Luca manuel sync örnek stub
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running manual sync for {IntegrationName}", request.IntegrationName);
            return false;
        }
    }
}
