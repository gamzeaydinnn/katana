using Katana.API.Hubs;
using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Katana.Core.Entities;
using Katana.Core.Helpers;
using Katana.Data.Context;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Katana.API.Workers;

/// <summary>
/// Luca'ya toplu ürün gönderimi yapan background worker.
/// Batch job queue'dan işleri alır ve arka planda paralel olarak işler.
/// - MaxDegreeOfParallelism: 10 (Luca API'yi yormadan optimum hız)
/// - SemaphoreSlim ile kontrollü paralel işlem
/// - SignalR ile gerçek zamanlı ilerleme bildirimi
/// </summary>
public class LucaBatchPushWorker : BackgroundService
{
    private readonly ILogger<LucaBatchPushWorker> _logger;
    private readonly IServiceProvider _services;
    private readonly IBatchJobService _batchJobService;
    private readonly IHubContext<NotificationHub> _hubContext;
    
    /// <summary>
    /// Maksimum paralel istek sayısı - Luca API'yi yormadan optimum değer
    /// </summary>
    private const int MaxParallelism = 10;
    
    /// <summary>
    /// Paralel işlem için minimum ürün eşiği
    /// Bu sayının altındaki ürünler sıralı işlenir (daha verimli)
    /// </summary>
    private const int ParallelThreshold = 50;
    
    /// <summary>
    /// İlerleme bildirimi gönderme aralığı (her X üründe bir)
    /// </summary>
    private const int ProgressNotifyInterval = 10;

    public LucaBatchPushWorker(
        ILogger<LucaBatchPushWorker> logger,
        IServiceProvider services,
        IBatchJobService batchJobService,
        IHubContext<NotificationHub> hubContext)
    {
        _logger = logger;
        _services = services;
        _batchJobService = batchJobService;
        _hubContext = hubContext;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("LucaBatchPushWorker başlatıldı (MaxParallelism: {MaxParallelism}, ParallelThreshold: {Threshold})", 
            MaxParallelism, ParallelThreshold);

        // Başlangıçta biraz bekle
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Bekleyen job var mı kontrol et
                var job = _batchJobService.DequeuePendingJob();

                if (job != null)
                {
                    // Ürün sayısına göre işlem modunu belirle
                    var useParallel = job.TotalItems >= ParallelThreshold;
                    
                    _logger.LogInformation(
                        "Batch job işleniyor: {JobId}, Toplam: {TotalItems} ürün, Mod: {Mode}", 
                        job.JobId, job.TotalItems, useParallel ? $"Paralel ({MaxParallelism} thread)" : "Sıralı");

                    if (useParallel)
                    {
                        await ProcessBatchJobParallelAsync(job, stoppingToken);
                    }
                    else
                    {
                        await ProcessBatchJobSequentialAsync(job, stoppingToken);
                    }
                }
                else
                {
                    // Bekleyen job yoksa 5 saniye bekle
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }

                // Eski job'ları temizle (24 saatten eski)
                _batchJobService.CleanupOldJobs(TimeSpan.FromHours(24));
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LucaBatchPushWorker döngü hatası");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        _logger.LogInformation("LucaBatchPushWorker durduruluyor");
    }

    /// <summary>
    /// Küçük job'lar için sıralı işlem (50'den az ürün).
    /// Daha az overhead, daha basit.
    /// </summary>
    private async Task ProcessBatchJobSequentialAsync(BatchJobItem job, CancellationToken stoppingToken)
    {
        var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
            stoppingToken, 
            job.CancellationTokenSource?.Token ?? CancellationToken.None);

        var startTime = DateTime.UtcNow;

        try
        {
            // Job'u başlat
            _batchJobService.UpdateJobStatus(job.JobId, j =>
            {
                j.Status = BatchJobStatus.InProgress;
                j.StartedAt = DateTime.UtcNow;
            });

            await NotifyProgressAsync(job.JobId, "Batch işlemi başladı (sıralı mod)", 0);

            using var scope = _services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();

            // Ürünleri ID'lere göre al
            var products = await dbContext.Products
                .Where(p => job.ItemIds.Contains(p.Id))
                .ToListAsync(combinedCts.Token);

            if (products.Count == 0)
            {
                _batchJobService.UpdateJobStatus(job.JobId, j =>
                {
                    j.Status = BatchJobStatus.Completed;
                    j.CompletedAt = DateTime.UtcNow;
                    j.Errors.Add("İşlenecek ürün bulunamadı");
                });
                await NotifyProgressAsync(job.JobId, "İşlenecek ürün bulunamadı", 100);
                return;
            }

            // Batch'lere böl
            var batches = products
                .Select((product, index) => new { product, index })
                .GroupBy(x => x.index / job.BatchSize)
                .Select((g, batchIndex) => new { BatchIndex = batchIndex + 1, Products = g.Select(x => x.product).ToList() })
                .ToList();

            _batchJobService.UpdateJobStatus(job.JobId, j =>
            {
                j.TotalItems = products.Count;
                j.TotalBatches = batches.Count;
            });

            int totalProcessed = 0;
            int totalSuccess = 0;
            int totalFailed = 0;

            foreach (var batch in batches)
            {
                if (combinedCts.Token.IsCancellationRequested) break;

                try
                {
                    var lucaStockCards = batch.Products
                        .Select(p => MappingHelper.MapToLucaStockCard(p))
                        .ToList();

                    var result = await lucaService.SendStockCardsAsync(lucaStockCards);

                    if (result.SuccessfulRecords > 0)
                    {
                        totalSuccess += batch.Products.Count;
                    }
                    else
                    {
                        totalFailed += batch.Products.Count;
                        foreach (var product in batch.Products)
                        {
                            _batchJobService.UpdateJobStatus(job.JobId, j =>
                            {
                                j.FailedItemDetails.Add(new BatchItemResult
                                {
                                    ItemId = product.Id,
                                    ItemCode = product.SKU ?? "",
                                    ItemName = product.Name ?? "",
                                    Success = false,
                                    ErrorMessage = result.Message ?? "Bilinmeyen hata",
                                    ProcessedAt = DateTime.UtcNow
                                });
                            });
                        }
                    }

                    totalProcessed += batch.Products.Count;
                }
                catch (Exception ex)
                {
                    totalFailed += batch.Products.Count;
                    totalProcessed += batch.Products.Count;
                    _logger.LogError(ex, "Batch {BatchIndex} hatası", batch.BatchIndex);
                }

                // İlerleme güncelle
                _batchJobService.UpdateJobStatus(job.JobId, j =>
                {
                    j.ProcessedItems = totalProcessed;
                    j.SuccessfulItems = totalSuccess;
                    j.FailedItems = totalFailed;
                    j.CurrentBatch = batch.BatchIndex;
                });

                var progress = (int)((double)totalProcessed / products.Count * 100);
                await NotifyProgressAsync(job.JobId, 
                    $"Batch {batch.BatchIndex}/{batches.Count} tamamlandı ({totalProcessed}/{products.Count})", 
                    progress);

                // Batch arası bekleme
                if (batch.BatchIndex < batches.Count && job.DelayBetweenBatchesMs > 0)
                {
                    await Task.Delay(job.DelayBetweenBatchesMs, combinedCts.Token);
                }
            }

            // Tamamla
            var totalElapsed = DateTime.UtcNow - startTime;
            var finalStatus = combinedCts.Token.IsCancellationRequested 
                ? BatchJobStatus.Cancelled
                : (totalFailed > 0 && totalSuccess > 0) ? BatchJobStatus.PartiallyCompleted
                : (totalFailed == 0) ? BatchJobStatus.Completed
                : BatchJobStatus.Failed;

            _batchJobService.UpdateJobStatus(job.JobId, j =>
            {
                j.Status = finalStatus;
                j.CompletedAt = DateTime.UtcNow;
            });

            var speed = totalElapsed.TotalSeconds > 0 ? totalProcessed / totalElapsed.TotalSeconds : 0;
            await NotifyProgressAsync(job.JobId, 
                $"✅ Tamamlandı: {totalSuccess}/{totalProcessed} ({totalElapsed.TotalSeconds:F1} sn, {speed:F1}/sn)", 
                100, status: finalStatus);

            _logger.LogInformation("Job {JobId} tamamlandı: {Status}, {Success}/{Total}", 
                job.JobId, finalStatus, totalSuccess, totalProcessed);
        }
        catch (OperationCanceledException)
        {
            _batchJobService.UpdateJobStatus(job.JobId, j =>
            {
                j.Status = BatchJobStatus.Cancelled;
                j.CompletedAt = DateTime.UtcNow;
            });
            await NotifyProgressAsync(job.JobId, "🚫 İptal edildi", 0, status: BatchJobStatus.Cancelled);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Job {JobId} kritik hata", job.JobId);
            _batchJobService.UpdateJobStatus(job.JobId, j =>
            {
                j.Status = BatchJobStatus.Failed;
                j.CompletedAt = DateTime.UtcNow;
                j.Errors.Add($"Kritik hata: {ex.Message}");
            });
            await NotifyProgressAsync(job.JobId, $"❌ Hata: {ex.Message}", 0, status: BatchJobStatus.Failed);
        }
    }

    /// <summary>
    /// Büyük job'lar için paralel işlem (50+ ürün).
    /// SemaphoreSlim ile maksimum 10 eş zamanlı istek yapılır.
    /// </summary>
    private async Task ProcessBatchJobParallelAsync(BatchJobItem job, CancellationToken stoppingToken)
    {
        var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
            stoppingToken, 
            job.CancellationTokenSource?.Token ?? CancellationToken.None);

        var startTime = DateTime.UtcNow;

        try
        {
            // Job'u başlat
            _batchJobService.UpdateJobStatus(job.JobId, j =>
            {
                j.Status = BatchJobStatus.InProgress;
                j.StartedAt = DateTime.UtcNow;
            });

            await NotifyProgressAsync(job.JobId, "Batch işlemi başladı (paralel mod)", 0, new BatchProgressDetails
            {
                TotalItems = job.TotalItems,
                ProcessedItems = 0,
                SuccessfulItems = 0,
                FailedItems = 0,
                ItemsPerSecond = 0,
                EstimatedSecondsRemaining = 0
            });

            using var scope = _services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();

            // Ürünleri ID'lere göre al
            var products = await dbContext.Products
                .Where(p => job.ItemIds.Contains(p.Id))
                .ToListAsync(combinedCts.Token);

            if (products.Count == 0)
            {
                _batchJobService.UpdateJobStatus(job.JobId, j =>
                {
                    j.Status = BatchJobStatus.Completed;
                    j.CompletedAt = DateTime.UtcNow;
                    j.Errors.Add("İşlenecek ürün bulunamadı");
                });
                await NotifyProgressAsync(job.JobId, "İşlenecek ürün bulunamadı", 100);
                return;
            }

            // Ürün sayısını güncelle
            var totalBatches = (int)Math.Ceiling((double)products.Count / job.BatchSize);
            _batchJobService.UpdateJobStatus(job.JobId, j =>
            {
                j.TotalItems = products.Count;
                j.TotalBatches = totalBatches;
            });

            _logger.LogInformation(
                "Job {JobId}: {Count} ürün, {Batches} batch, {MaxParallelism} paralel thread",
                job.JobId, products.Count, totalBatches, MaxParallelism);

            // Thread-safe sayaçlar
            var processedCount = 0;
            var successCount = 0;
            var failedCount = 0;
            var lastNotifyTime = DateTime.UtcNow;

            // Paralel işlem için SemaphoreSlim
            using var semaphore = new SemaphoreSlim(MaxParallelism, MaxParallelism);
            var tasks = new List<Task>();

            // Batch'lere böl
            var batches = products
                .Select((product, index) => new { product, index })
                .GroupBy(x => x.index / job.BatchSize)
                .Select((g, batchIndex) => new { BatchIndex = batchIndex + 1, Products = g.Select(x => x.product).ToList() })
                .ToList();

            foreach (var batch in batches)
            {
                if (combinedCts.Token.IsCancellationRequested)
                {
                    _logger.LogWarning("Job {JobId} iptal edildi", job.JobId);
                    break;
                }

                // Semaphore ile paralel kontrol
                await semaphore.WaitAsync(combinedCts.Token);

                var batchTask = ProcessSingleBatchAsync(
                    job.JobId,
                    batch.BatchIndex,
                    totalBatches,
                    batch.Products,
                    combinedCts.Token,
                    semaphore,
                    // Progress callback
                    (processed, success, failed) =>
                    {
                        Interlocked.Add(ref processedCount, processed);
                        Interlocked.Add(ref successCount, success);
                        Interlocked.Add(ref failedCount, failed);

                        // Her 10 üründe veya 2 saniyede bir bildirim
                        if (processedCount % ProgressNotifyInterval == 0 || 
                            (DateTime.UtcNow - lastNotifyTime).TotalSeconds >= 2)
                        {
                            lastNotifyTime = DateTime.UtcNow;
                            var elapsed = (DateTime.UtcNow - startTime).TotalSeconds;
                            var itemsPerSecond = elapsed > 0 ? processedCount / elapsed : 0;
                            var remaining = products.Count - processedCount;
                            var estimatedSeconds = itemsPerSecond > 0 ? remaining / itemsPerSecond : 0;

                            _batchJobService.UpdateJobStatus(job.JobId, j =>
                            {
                                j.ProcessedItems = processedCount;
                                j.SuccessfulItems = successCount;
                                j.FailedItems = failedCount;
                            });

                            var progress = (int)((double)processedCount / products.Count * 100);
                            _ = NotifyProgressAsync(job.JobId,
                                $"İşleniyor: {processedCount}/{products.Count} ({progress}%)",
                                progress,
                                new BatchProgressDetails
                                {
                                    TotalItems = products.Count,
                                    ProcessedItems = processedCount,
                                    SuccessfulItems = successCount,
                                    FailedItems = failedCount,
                                    ItemsPerSecond = Math.Round(itemsPerSecond, 1),
                                    EstimatedSecondsRemaining = (int)estimatedSeconds
                                });
                        }
                    },
                    // Error callback
                    (productId, sku, name, error) =>
                    {
                        _batchJobService.UpdateJobStatus(job.JobId, j =>
                        {
                            j.FailedItemDetails.Add(new BatchItemResult
                            {
                                ItemId = productId,
                                ItemCode = sku,
                                ItemName = name,
                                Success = false,
                                ErrorMessage = error,
                                ProcessedAt = DateTime.UtcNow
                            });
                        });
                    });

                tasks.Add(batchTask);
            }

            // Tüm task'lerin tamamlanmasını bekle
            await Task.WhenAll(tasks);

            // Final istatistikleri güncelle
            var totalElapsed = DateTime.UtcNow - startTime;
            var finalItemsPerSecond = totalElapsed.TotalSeconds > 0 ? processedCount / totalElapsed.TotalSeconds : 0;

            // Job'u tamamla
            var finalStatus = combinedCts.Token.IsCancellationRequested 
                ? BatchJobStatus.Cancelled
                : (failedCount > 0 && successCount > 0)
                    ? BatchJobStatus.PartiallyCompleted
                    : (failedCount == 0)
                        ? BatchJobStatus.Completed
                        : BatchJobStatus.Failed;

            _batchJobService.UpdateJobStatus(job.JobId, j =>
            {
                j.Status = finalStatus;
                j.CompletedAt = DateTime.UtcNow;
                j.ProcessedItems = processedCount;
                j.SuccessfulItems = successCount;
                j.FailedItems = failedCount;
            });

            var statusMessage = finalStatus switch
            {
                BatchJobStatus.Completed => $"✅ Tamamlandı: {successCount} ürün ({totalElapsed.TotalMinutes:F1} dk, {finalItemsPerSecond:F1}/sn)",
                BatchJobStatus.PartiallyCompleted => $"⚠️ Kısmen tamamlandı: {successCount} başarılı, {failedCount} başarısız",
                BatchJobStatus.Failed => $"❌ Başarısız: {failedCount} ürün gönderilemedi",
                BatchJobStatus.Cancelled => "🚫 İptal edildi",
                _ => "Bilinmeyen durum"
            };

            await NotifyProgressAsync(job.JobId, statusMessage, 100, new BatchProgressDetails
            {
                TotalItems = products.Count,
                ProcessedItems = processedCount,
                SuccessfulItems = successCount,
                FailedItems = failedCount,
                ItemsPerSecond = Math.Round(finalItemsPerSecond, 1),
                EstimatedSecondsRemaining = 0,
                ElapsedSeconds = (int)totalElapsed.TotalSeconds
            }, finalStatus);

            _logger.LogInformation(
                "Batch job {JobId} tamamlandı: {Status}, {Success}/{Total} başarılı, Süre: {Elapsed:F1} dk, Hız: {Speed:F1}/sn",
                job.JobId, finalStatus, successCount, processedCount, totalElapsed.TotalMinutes, finalItemsPerSecond);
        }
        catch (OperationCanceledException)
        {
            _batchJobService.UpdateJobStatus(job.JobId, j =>
            {
                j.Status = BatchJobStatus.Cancelled;
                j.CompletedAt = DateTime.UtcNow;
            });
            await NotifyProgressAsync(job.JobId, "🚫 İptal edildi", 0, status: BatchJobStatus.Cancelled);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Batch job {JobId} işlenirken kritik hata", job.JobId);
            
            _batchJobService.UpdateJobStatus(job.JobId, j =>
            {
                j.Status = BatchJobStatus.Failed;
                j.CompletedAt = DateTime.UtcNow;
                j.Errors.Add($"Kritik hata: {ex.Message}");
            });

            await NotifyProgressAsync(job.JobId, $"❌ Hata: {ex.Message}", 0, status: BatchJobStatus.Failed);
        }
    }

    /// <summary>
    /// Tek bir batch'i işler
    /// </summary>
    private async Task ProcessSingleBatchAsync(
        string jobId,
        int batchIndex,
        int totalBatches,
        List<Product> products,
        CancellationToken ct,
        SemaphoreSlim semaphore,
        Action<int, int, int> progressCallback,
        Action<int, string, string, string> errorCallback)
    {
        try
        {
            using var scope = _services.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();

            _logger.LogDebug("Batch {BatchIndex}/{TotalBatches} başladı, {Count} ürün", 
                batchIndex, totalBatches, products.Count);

            // Ürünleri Luca formatına dönüştür
            var lucaStockCards = products
                .Select(p => MappingHelper.MapToLucaStockCard(p))
                .ToList();

            // Luca'ya gönder
            var result = await lucaService.SendStockCardsAsync(lucaStockCards);

            // Sonuçları işle
            int batchSuccess = 0;
            int batchFailed = 0;

            if (result.SuccessfulRecords > 0)
            {
                batchSuccess = products.Count;
            }
            else
            {
                batchFailed = products.Count;
                foreach (var product in products)
                {
                    errorCallback(product.Id, product.SKU ?? "", product.Name ?? "", result.Message ?? "Bilinmeyen hata");
                }
            }

            progressCallback(products.Count, batchSuccess, batchFailed);

            _logger.LogDebug("Batch {BatchIndex}/{TotalBatches} tamamlandı: {Success} başarılı, {Failed} başarısız", 
                batchIndex, totalBatches, batchSuccess, batchFailed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Batch {BatchIndex} işlenirken hata", batchIndex);
            
            foreach (var product in products)
            {
                errorCallback(product.Id, product.SKU ?? "", product.Name ?? "", ex.Message);
            }
            
            progressCallback(products.Count, 0, products.Count);
            
            _batchJobService.UpdateJobStatus(jobId, j =>
            {
                j.Errors.Add($"Batch {batchIndex} hatası: {ex.Message}");
            });
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task NotifyProgressAsync(
        string jobId, 
        string message, 
        int progress, 
        BatchProgressDetails? details = null,
        BatchJobStatus? status = null)
    {
        try
        {
            await _hubContext.Clients.All.SendAsync("BatchJobProgress", new
            {
                JobId = jobId,
                Message = message,
                Progress = progress,
                Status = status?.ToString() ?? "InProgress",
                Timestamp = DateTime.UtcNow,
                Details = details
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Batch job ilerleme bildirimi gönderilemedi: {JobId}", jobId);
        }
    }
}

/// <summary>
/// SignalR ile gönderilen detaylı ilerleme bilgisi
/// </summary>
public class BatchProgressDetails
{
    public int TotalItems { get; set; }
    public int ProcessedItems { get; set; }
    public int SuccessfulItems { get; set; }
    public int FailedItems { get; set; }
    public double ItemsPerSecond { get; set; }
    public int EstimatedSecondsRemaining { get; set; }
    public int ElapsedSeconds { get; set; }
}
