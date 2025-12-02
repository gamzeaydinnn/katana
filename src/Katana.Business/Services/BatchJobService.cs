using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Katana.Business.Services;

/// <summary>
/// Batch job yönetim servisi - in-memory kuyruk ve durum takibi
/// </summary>
public class BatchJobService : IBatchJobService
{
    private readonly ILogger<BatchJobService> _logger;
    private readonly ConcurrentDictionary<string, BatchJobItem> _jobs = new();
    private readonly ConcurrentQueue<string> _pendingJobIds = new();
    private readonly object _lock = new();

    public BatchJobService(ILogger<BatchJobService> logger)
    {
        _logger = logger;
    }

    public Task<BatchJobCreatedResponse> CreateBatchJobAsync(BatchPushRequest request, string createdBy)
    {
        var jobId = $"batch_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N")[..8]}";
        
        var itemIds = request.ProductIds ?? new List<int>();
        var totalItems = itemIds.Count;
        var batchSize = request.BatchSize > 0 ? request.BatchSize : 100;
        var totalBatches = (int)Math.Ceiling((double)totalItems / batchSize);

        var job = new BatchJobItem
        {
            JobId = jobId,
            JobType = BatchJobType.ProductPush,
            Status = BatchJobStatus.Pending,
            ItemIds = itemIds,
            BatchSize = batchSize,
            DelayBetweenBatchesMs = request.DelayBetweenBatchesMs,
            TotalItems = totalItems,
            ProcessedItems = 0,
            SuccessfulItems = 0,
            FailedItems = 0,
            CurrentBatch = 0,
            TotalBatches = totalBatches,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy,
            CancellationTokenSource = new CancellationTokenSource()
        };

        _jobs[jobId] = job;
        _pendingJobIds.Enqueue(jobId);

        _logger.LogInformation(
            "Batch job oluşturuldu: {JobId}, Toplam: {TotalItems} ürün, {TotalBatches} batch, BatchSize: {BatchSize}",
            jobId, totalItems, totalBatches, batchSize);

        var response = new BatchJobCreatedResponse
        {
            JobId = jobId,
            Message = $"Batch job başarıyla oluşturuldu. {totalItems} ürün {totalBatches} batch halinde işlenecek.",
            TotalProducts = totalItems,
            TotalBatches = totalBatches,
            BatchSize = batchSize,
            CreatedAt = job.CreatedAt,
            StatusUrl = $"/api/luca/batch-status/{jobId}"
        };

        return Task.FromResult(response);
    }

    public BatchJobStatusDto? GetJobStatus(string jobId)
    {
        if (!_jobs.TryGetValue(jobId, out var job))
            return null;

        return MapToStatusDto(job);
    }

    public ActiveBatchJobsResponse GetActiveJobs()
    {
        var allJobs = _jobs.Values.ToList();
        var summaries = allJobs
            .OrderByDescending(j => j.CreatedAt)
            .Select(j => new BatchJobSummaryDto
            {
                JobId = j.JobId,
                JobType = j.JobType,
                Status = j.Status,
                TotalItems = j.TotalItems,
                ProcessedItems = j.ProcessedItems,
                ProgressPercentage = j.TotalItems > 0 ? Math.Round((double)j.ProcessedItems / j.TotalItems * 100, 2) : 0,
                CreatedAt = j.CreatedAt,
                CompletedAt = j.CompletedAt,
                CreatedBy = j.CreatedBy
            })
            .ToList();

        return new ActiveBatchJobsResponse
        {
            Jobs = summaries,
            TotalJobs = summaries.Count,
            RunningJobs = summaries.Count(j => j.Status == BatchJobStatus.InProgress),
            PendingJobs = summaries.Count(j => j.Status == BatchJobStatus.Pending)
        };
    }

    public bool CancelJob(string jobId, string cancelledBy, string reason)
    {
        if (!_jobs.TryGetValue(jobId, out var job))
            return false;

        if (job.Status == BatchJobStatus.Completed || job.Status == BatchJobStatus.Failed)
        {
            _logger.LogWarning("Job {JobId} zaten tamamlanmış/başarısız, iptal edilemez", jobId);
            return false;
        }

        lock (_lock)
        {
            job.CancellationTokenSource?.Cancel();
            job.Status = BatchJobStatus.Cancelled;
            job.CompletedAt = DateTime.UtcNow;
            job.Errors.Add($"İptal eden: {cancelledBy}, Sebep: {reason}");
        }

        _logger.LogInformation("Job {JobId} iptal edildi: {Reason}", jobId, reason);
        return true;
    }

    public BatchJobItem? DequeuePendingJob()
    {
        while (_pendingJobIds.TryDequeue(out var jobId))
        {
            if (_jobs.TryGetValue(jobId, out var job) && job.Status == BatchJobStatus.Pending)
            {
                return job;
            }
        }
        return null;
    }

    public void UpdateJobStatus(string jobId, Action<BatchJobItem> updateAction)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            lock (_lock)
            {
                updateAction(job);
            }
        }
    }

    public int CleanupOldJobs(TimeSpan olderThan)
    {
        var cutoff = DateTime.UtcNow - olderThan;
        var toRemove = _jobs
            .Where(kvp => 
                (kvp.Value.Status == BatchJobStatus.Completed || 
                 kvp.Value.Status == BatchJobStatus.Failed ||
                 kvp.Value.Status == BatchJobStatus.Cancelled) &&
                kvp.Value.CompletedAt.HasValue &&
                kvp.Value.CompletedAt.Value < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var jobId in toRemove)
        {
            _jobs.TryRemove(jobId, out _);
        }

        if (toRemove.Count > 0)
        {
            _logger.LogInformation("{Count} eski job temizlendi", toRemove.Count);
        }

        return toRemove.Count;
    }

    public bool HasPendingJobs()
    {
        return _jobs.Values.Any(j => j.Status == BatchJobStatus.Pending || j.Status == BatchJobStatus.InProgress);
    }

    private static BatchJobStatusDto MapToStatusDto(BatchJobItem job)
    {
        TimeSpan? estimatedRemaining = null;
        if (job.Status == BatchJobStatus.InProgress && job.StartedAt.HasValue && job.ProcessedItems > 0)
        {
            var elapsed = DateTime.UtcNow - job.StartedAt.Value;
            var itemsPerSecond = job.ProcessedItems / elapsed.TotalSeconds;
            if (itemsPerSecond > 0)
            {
                var remainingItems = job.TotalItems - job.ProcessedItems;
                estimatedRemaining = TimeSpan.FromSeconds(remainingItems / itemsPerSecond);
            }
        }

        return new BatchJobStatusDto
        {
            JobId = job.JobId,
            Status = job.Status,
            JobType = job.JobType,
            TotalItems = job.TotalItems,
            ProcessedItems = job.ProcessedItems,
            SuccessfulItems = job.SuccessfulItems,
            FailedItems = job.FailedItems,
            CurrentBatch = job.CurrentBatch,
            TotalBatches = job.TotalBatches,
            CreatedAt = job.CreatedAt,
            StartedAt = job.StartedAt,
            CompletedAt = job.CompletedAt,
            EstimatedTimeRemaining = estimatedRemaining,
            Errors = job.Errors.TakeLast(50).ToList(),
            FailedItemDetails = job.FailedItemDetails.TakeLast(100).ToList()
        };
    }
}
