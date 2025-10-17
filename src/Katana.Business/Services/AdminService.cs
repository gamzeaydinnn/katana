using Katana.Business.DTOs;
using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Katana.Data.Context;
using Katana.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;


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
            var katanaStatus = new AdminSyncStatusDto
            {
                IntegrationName = "Katana",
                LastSyncDate = await _context.SyncLogs
                    .Where(l => l.IntegrationName == "Katana")
                    .OrderByDescending(l => l.CreatedAt)
                    .Select(l => l.CreatedAt)
                    .FirstOrDefaultAsync(),
                Status = "Unknown"
            };
            statuses.Add(katanaStatus);

            var lucaStatus = new AdminSyncStatusDto
            {
                IntegrationName = "Luca",
                LastSyncDate = await _context.SyncLogs
                    .Where(l => l.IntegrationName == "Luca")
                    .OrderByDescending(l => l.CreatedAt)
                    .Select(l => l.CreatedAt)
                    .FirstOrDefaultAsync(),
                Status = "Unknown"
            };
            statuses.Add(lucaStatus);
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

    public async Task<SyncReportDto> GetSyncReportAsync(string integrationName)
    {
        var total = await _context.SyncLogs.CountAsync(l => l.IntegrationName == integrationName);
        var success = await _context.SyncLogs.CountAsync(l => l.IntegrationName == integrationName && l.IsSuccess);
        var failed = total - success;

        return new SyncReportDto
        {
            IntegrationName = integrationName,
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
            if (request.IntegrationName.Equals("Katana", StringComparison.OrdinalIgnoreCase))
            {
                var products = await _katanaService.GetProductsAsync();
                // Burada gÃ¶nderme iÅŸlemi yapÄ±labilir
                return true;
            }
            else if (request.IntegrationName.Equals("Luca", StringComparison.OrdinalIgnoreCase))
            {
                // Luca manuel sync
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

    Task<List<AdminSyncStatusDto>> IAdminService.GetSyncStatusesAsync()
    {
        throw new NotImplementedException();
    }

    Task<List<ErrorLogDto>> IAdminService.GetErrorLogsAsync(int page, int pageSize)
    {
        throw new NotImplementedException();
    }

    Task<SyncReportDto> IAdminService.GetSyncReportAsync(string integrationName)
    {
        throw new NotImplementedException();
    }

   
}


