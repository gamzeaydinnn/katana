//LoaderService (LucaClient çağrıları, file exports, retry logic)
//ILucaClient ile yükleme, retry, batch ack.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Katana.Core.Entities;
using Katana.Business.Interfaces; // ILucaService
using Katana.Data.Context;
using Katana.Data.Models; // FailedSyncRecord / IntegrationLog vb
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Polly;

namespace Katana.Business.Services;

/// <summary>
/// Hedef sisteme (Luca) yazma/yükleme, batch işlemleri, retry/backoff ve loglama.
/// </summary>
public class LoaderService
{
    private readonly ILucaService _luca;
    private readonly IntegrationDbContext _db;
    private readonly ILogger<LoaderService> _logger;

    public LoaderService(ILucaService luca, IntegrationDbContext db, ILogger<LoaderService> logger)
    {
        _luca = luca;
        _db = db;
        _logger = logger;
    }

    public async Task<int> LoadProductsAsync(IEnumerable<Product> products, int batchSize = 100, CancellationToken ct = default)
    {
        var list = products.ToList();
        if (!list.Any()) return 0;

        var policy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(3, retry => TimeSpan.FromSeconds(Math.Pow(2, retry)),
                (ex, ts, attempt, ctx) => _logger.LogWarning(ex, "Loader retry #{Attempt}", attempt));

        var total = 0;
        foreach (var chunk in list.Chunk(batchSize))
        {
            await policy.ExecuteAsync(async () =>
            {
                await _luca.UpsertProductsAsync(chunk, ct);
                total += chunk.Length;
            });
            if (ct.IsCancellationRequested) break;
        }

        await AddIntegrationLogAsync("PRODUCT", "SUCCESS", total, 0, null, ct);
        return total;
    }

    public async Task<int> LoadInvoicesAsync(IEnumerable<Invoice> invoices, int batchSize = 50, CancellationToken ct = default)
    {
        var list = invoices.ToList();
        if (!list.Any()) return 0;

        var total = 0;
        var failed = 0;

        var policy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(3, retry => TimeSpan.FromSeconds(Math.Pow(2, retry)),
                (ex, ts, attempt, ctx) => _logger.LogWarning(ex, "Loader retry #{Attempt}", attempt));

        foreach (var chunk in list.Chunk(batchSize))
        {
            try
            {
                await policy.ExecuteAsync(async () =>
                {
                    await _luca.UpsertInvoicesAsync(chunk, ct);
                    total += chunk.Length;
                });
            }
            catch (Exception ex)
            {
                failed += chunk.Length;
                _logger.LogError(ex, "Loader: invoice batch hata aldı ({Count})", chunk.Length);
                await AddFailedRecordsAsync("INVOICE", chunk.Select(c => c.InvoiceNo), ex, ct);
            }

            if (ct.IsCancellationRequested) break;
        }

        var status = failed == 0 ? "SUCCESS" : (total == 0 ? "FAILED" : "PARTIAL");
        await AddIntegrationLogAsync("INVOICE", status, total, failed, null, ct);

        return total;
    }

    private async Task AddFailedRecordsAsync(string recordType, IEnumerable<string> ids, Exception ex, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        foreach (var id in ids)
        {
            _db.FailedSyncRecords.Add(new FailedSyncRecord
            {
                IntegrationLogId = 0, // istersen son log id ile ilişkilendir
                RecordType = recordType,
                RecordId = id,
                OriginalData = string.Empty,
                ErrorMessage = ex.Message,
                ErrorCode = ex.GetType().Name,
                FailedAt = now,
                RetryCount = 0,
                LastRetryAt = null,
                NextRetryAt = now.AddMinutes(15),
                Status = "FAILED"
            });
        }
        await _db.SaveChangesAsync(ct);
    }

    private async Task AddIntegrationLogAsync(string syncType, string status, int success, int failed, string? details, CancellationToken ct)
    {
        _db.IntegrationLogs.Add(new IntegrationLog
        {
            SyncType = syncType,
            Status = status,
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow,
            FailedRecords = failed,
            SuccessfulRecords = success,
            ProcessedRecords = success + failed,
            Details = details
        });
        await _db.SaveChangesAsync(ct);
    }
}
