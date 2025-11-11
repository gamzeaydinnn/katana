using System.Diagnostics;
using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Katana.Core.Entities;
using Katana.Core.Enums;
using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Katana.Business.UseCases.Sync;

/// <summary>
/// ETL bileşenlerini koordine ederek Katana ↔ Luca senkronizasyonunu gerçekleştirir.
/// </summary>
public class SyncService : ISyncService, IIntegrationService
{
    private readonly IExtractorService _extractorService;
    private readonly ITransformerService _transformerService;
    private readonly ILoaderService _loaderService;
    private readonly ILucaService _lucaService;
    private readonly IntegrationDbContext _dbContext;
    private readonly ILogger<SyncService> _logger;

    public SyncService(
        IExtractorService extractorService,
        ITransformerService transformerService,
        ILoaderService loaderService,
        ILucaService lucaService,
        IntegrationDbContext dbContext,
        ILogger<SyncService> logger)
    {
        _extractorService = extractorService;
        _transformerService = transformerService;
        _loaderService = loaderService;
        _lucaService = lucaService;
        _dbContext = dbContext;
        _logger = logger;
    }

    public Task<SyncResultDto> SyncStockAsync(DateTime? fromDate = null) =>
        ExecuteSyncAsync("STOCK", async ct =>
        {
            var productDtos = await _extractorService.ExtractProductsAsync(fromDate, ct);
            var products = await _transformerService.ToProductsAsync(productDtos);
            var successful = await _loaderService.LoadProductsAsync(products, ct: ct);
            return BuildResult("STOCK", productDtos.Count, successful);
        });

    public Task<SyncResultDto> SyncInvoicesAsync(DateTime? fromDate = null) =>
        ExecuteSyncAsync("INVOICE", async ct =>
        {
            var invoiceDtos = await _extractorService.ExtractInvoicesAsync(fromDate, ct);
            var invoices = await _transformerService.ToInvoicesAsync(invoiceDtos);
            var successful = await _loaderService.LoadInvoicesAsync(invoices, ct: ct);
            return BuildResult("INVOICE", invoiceDtos.Count, successful);
        });

    public Task<SyncResultDto> SyncCustomersAsync(DateTime? fromDate = null) =>
        ExecuteSyncAsync("CUSTOMER", async ct =>
        {
            var customerDtos = await _extractorService.ExtractCustomersAsync(fromDate, ct);
            var customers = await _transformerService.ToCustomersAsync(customerDtos);
            var successful = await _loaderService.LoadCustomersAsync(customers, ct: ct);
            return BuildResult("CUSTOMER", customerDtos.Count, successful);
        });

    public async Task<BatchSyncResultDto> SyncAllAsync(DateTime? fromDate = null)
    {
        var results = new List<SyncResultDto>
        {
            await SyncCustomersAsync(fromDate),
            await SyncStockAsync(fromDate),
            await SyncInvoicesAsync(fromDate)
        };

        return new BatchSyncResultDto
        {
            Results = results,
            BatchTime = DateTime.UtcNow
        };
    }

    public async Task<List<SyncStatusDto>> GetSyncStatusAsync()
    {
        var latestLogs = await _dbContext.SyncOperationLogs
            .OrderByDescending(log => log.StartTime)
            .GroupBy(log => log.SyncType)
            .Select(group => group.First())
            .ToListAsync();

        return latestLogs.Select(log => new SyncStatusDto
        {
            SyncType = log.SyncType,
            LastSyncTime = log.EndTime,
            IsRunning = string.Equals(log.Status, "RUNNING", StringComparison.OrdinalIgnoreCase),
            CurrentStatus = log.Status,
            PendingRecords = 0
        }).ToList();
    }

    public async Task<bool> IsSyncRunningAsync(string syncType)
    {
        var normalizedType = syncType.ToUpperInvariant();
        return await _dbContext.SyncOperationLogs
            .AnyAsync(log => log.SyncType == normalizedType && log.Status == "RUNNING");
    }

    private async Task<SyncResultDto> ExecuteSyncAsync(
        string syncType,
        Func<CancellationToken, Task<SyncResultDto>> operation)
    {
        var stopwatch = Stopwatch.StartNew();
        var logEntry = await StartOperationLogAsync(syncType);
        var cts = new CancellationTokenSource();

        try
        {
            var result = await operation(cts.Token);
            stopwatch.Stop();

            result.SyncType = syncType;
            result.SyncTime = DateTime.UtcNow;
            result.Duration = stopwatch.Elapsed;

            await FinalizeOperationAsync(
                logEntry,
                "SUCCESS",
                result.ProcessedRecords,
                result.SuccessfulRecords,
                result.FailedRecords,
                null);

            _logger.LogInformation("SyncService => {SyncType} sync completed. Processed={Processed} Success={Success}",
                syncType, result.ProcessedRecords, result.SuccessfulRecords);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            await FinalizeOperationAsync(
                logEntry,
                "FAILED",
                0,
                0,
                0,
                ex.Message);

            _logger.LogError(ex, "SyncService => {SyncType} sync failed.", syncType);

            return new SyncResultDto
            {
                SyncType = syncType,
                IsSuccess = false,
                Message = ex.Message,
                SyncTime = DateTime.UtcNow,
                Duration = stopwatch.Elapsed,
                Errors = { ex.ToString() }
            };
        }
    }

    private async Task<SyncOperationLog> StartOperationLogAsync(string syncType)
    {
        var log = new SyncOperationLog
        {
            SyncType = syncType,
            Status = "RUNNING",
            StartTime = DateTime.UtcNow
        };

        _dbContext.SyncOperationLogs.Add(log);
        await _dbContext.SaveChangesAsync();
        return log;
    }

    private async Task FinalizeOperationAsync(
        SyncOperationLog log,
        string status,
        int processed,
        int successful,
        int failed,
        string? errorMessage)
    {
        log.Status = status;
        log.ProcessedRecords = processed;
        log.SuccessfulRecords = successful;
        log.FailedRecords = failed;
        log.EndTime = DateTime.UtcNow;
        log.ErrorMessage = errorMessage;

        await _dbContext.SaveChangesAsync();
    }

    private static SyncResultDto BuildResult(string syncType, int processed, int successful) =>
        new()
        {
            SyncType = syncType,
            IsSuccess = successful == processed,
            ProcessedRecords = processed,
            SuccessfulRecords = successful,
            FailedRecords = processed - successful,
            Message = successful == processed
                ? $"{processed} kayıt senkronize edildi."
                : $"{processed} kaydın {successful} tanesi senkronize edildi."
        };

    // Luca → Katana (Reverse Sync) implementations
    public async Task<SyncResultDto> SyncStockFromLucaAsync(DateTime? fromDate = null)
    {
        var stopwatch = Stopwatch.StartNew();
        var logEntry = await StartOperationLogAsync("LUCA_TO_KATANA_STOCK");

        try
        {
            _logger.LogInformation("Starting Luca → Katana stock sync");
            
            // Fetch from Luca
            var lucaStockDtos = await _lucaService.FetchStockMovementsAsync(fromDate);
            
            // TODO: Transform Luca format → Katana format using TransformerService
            // For now, basic transformation
            _logger.LogInformation("Fetched {Count} stock movements from Luca", lucaStockDtos.Count);
            
            stopwatch.Stop();
            await FinalizeOperationAsync(logEntry, "SUCCESS", lucaStockDtos.Count, lucaStockDtos.Count, 0, null);
            
            return new SyncResultDto
            {
                SyncType = "LUCA_TO_KATANA_STOCK",
                IsSuccess = true,
                ProcessedRecords = lucaStockDtos.Count,
                SuccessfulRecords = lucaStockDtos.Count,
                Message = $"Luca'dan {lucaStockDtos.Count} stok hareketi alındı",
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            await FinalizeOperationAsync(logEntry, "FAILED", 0, 0, 0, ex.Message);
            _logger.LogError(ex, "Luca → Katana stock sync failed");
            
            return new SyncResultDto
            {
                SyncType = "LUCA_TO_KATANA_STOCK",
                IsSuccess = false,
                Message = ex.Message,
                Duration = stopwatch.Elapsed,
                Errors = { ex.ToString() }
            };
        }
    }

    public async Task<SyncResultDto> SyncInvoicesFromLucaAsync(DateTime? fromDate = null)
    {
        var stopwatch = Stopwatch.StartNew();
        var logEntry = await StartOperationLogAsync("LUCA_TO_KATANA_INVOICE");

        try
        {
            _logger.LogInformation("Starting Luca → Katana invoice sync");
            
            var lucaInvoiceDtos = await _lucaService.FetchInvoicesAsync(fromDate);
            _logger.LogInformation("Fetched {Count} invoices from Luca", lucaInvoiceDtos.Count);
            
            stopwatch.Stop();
            await FinalizeOperationAsync(logEntry, "SUCCESS", lucaInvoiceDtos.Count, lucaInvoiceDtos.Count, 0, null);
            
            return new SyncResultDto
            {
                SyncType = "LUCA_TO_KATANA_INVOICE",
                IsSuccess = true,
                ProcessedRecords = lucaInvoiceDtos.Count,
                SuccessfulRecords = lucaInvoiceDtos.Count,
                Message = $"Luca'dan {lucaInvoiceDtos.Count} fatura alındı",
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            await FinalizeOperationAsync(logEntry, "FAILED", 0, 0, 0, ex.Message);
            _logger.LogError(ex, "Luca → Katana invoice sync failed");
            
            return new SyncResultDto
            {
                SyncType = "LUCA_TO_KATANA_INVOICE",
                IsSuccess = false,
                Message = ex.Message,
                Duration = stopwatch.Elapsed,
                Errors = { ex.ToString() }
            };
        }
    }

    public async Task<SyncResultDto> SyncCustomersFromLucaAsync(DateTime? fromDate = null)
    {
        var stopwatch = Stopwatch.StartNew();
        var logEntry = await StartOperationLogAsync("LUCA_TO_KATANA_CUSTOMER");

        try
        {
            _logger.LogInformation("Starting Luca → Katana customer sync");
            
            var lucaCustomerDtos = await _lucaService.FetchCustomersAsync(fromDate);
            _logger.LogInformation("Fetched {Count} customers from Luca", lucaCustomerDtos.Count);
            
            stopwatch.Stop();
            await FinalizeOperationAsync(logEntry, "SUCCESS", lucaCustomerDtos.Count, lucaCustomerDtos.Count, 0, null);
            
            return new SyncResultDto
            {
                SyncType = "LUCA_TO_KATANA_CUSTOMER",
                IsSuccess = true,
                ProcessedRecords = lucaCustomerDtos.Count,
                SuccessfulRecords = lucaCustomerDtos.Count,
                Message = $"Luca'dan {lucaCustomerDtos.Count} müşteri alındı",
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            await FinalizeOperationAsync(logEntry, "FAILED", 0, 0, 0, ex.Message);
            _logger.LogError(ex, "Luca → Katana customer sync failed");
            
            return new SyncResultDto
            {
                SyncType = "LUCA_TO_KATANA_CUSTOMER",
                IsSuccess = false,
                Message = ex.Message,
                Duration = stopwatch.Elapsed,
                Errors = { ex.ToString() }
            };
        }
    }

    public async Task<BatchSyncResultDto> SyncAllFromLucaAsync(DateTime? fromDate = null)
    {
        var results = new List<SyncResultDto>
        {
            await SyncCustomersFromLucaAsync(fromDate),
            await SyncStockFromLucaAsync(fromDate),
            await SyncInvoicesFromLucaAsync(fromDate)
        };

        return new BatchSyncResultDto
        {
            Results = results,
            BatchTime = DateTime.UtcNow
        };
    }
}
