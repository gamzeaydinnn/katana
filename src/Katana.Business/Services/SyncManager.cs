//SyncManager (use-case orchestration: orchestration -> transaction -> audit -> error handling)
using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Microsoft.Extensions.Logging;

namespace Katana.Business.Services;

/// <summary>
/// Sync operasyonlarını yöneten üst seviye servis.
/// </summary>
public class SyncManager
{
    private readonly ISyncService _syncService;
    private readonly ILogger<SyncManager> _logger;

    public SyncManager(ISyncService syncService, ILogger<SyncManager> logger)
    {
        _syncService = syncService;
        _logger = logger;
    }

    /// <summary>
    /// Manuel olarak tüm verilerin senkronizasyonunu başlatır.
    /// </summary>
    public async Task<BatchSyncResultDto> RunManualSyncAsync()
    {
        _logger.LogInformation("🚀 Manual full sync started by admin.");
        return await _syncService.SyncAllAsync();
    }

    /// <summary>
    /// Belirli bir senkronizasyon türünü (örnek: STOCK) başlatır.
    /// </summary>
    public async Task<SyncResultDto> RunSyncByTypeAsync(string syncType)
    {
        _logger.LogInformation("Running manual sync for {SyncType}", syncType);

        return syncType.ToUpper() switch
        {
            "STOCK" => await _syncService.SyncStockAsync(),
            "INVOICE" => await _syncService.SyncInvoicesAsync(),
            "CUSTOMER" => await _syncService.SyncCustomersAsync(),
            _ => new SyncResultDto
            {
                IsSuccess = false,
                SyncType = syncType,
                Message = $"Invalid sync type: {syncType}"
            }
        };
    }
}
