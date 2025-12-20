using System.Text.Json.Serialization;

namespace Katana.Core.DTOs;

/// <summary>
/// Batch job durumu
/// </summary>
public enum BatchJobStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Failed = 3,
    PartiallyCompleted = 4,
    Cancelled = 5
}

/// <summary>
/// Batch işlem türü
/// </summary>
public enum BatchJobType
{
    ProductPush = 1,
    StockSync = 2,
    InvoiceSync = 3,
    CustomerSync = 4
}

/// <summary>
/// Batch push isteği
/// </summary>
public class BatchPushRequest
{
    /// <summary>
    /// Belirli ürün ID'leri (boş ise tüm ürünler)
    /// </summary>
    public List<int>? ProductIds { get; set; }
    
    /// <summary>
    /// Her batch'teki maksimum ürün sayısı
    /// </summary>
    public int BatchSize { get; set; } = 100;
    
    /// <summary>
    /// Batch'ler arası bekleme süresi (ms)
    /// </summary>
    public int DelayBetweenBatchesMs { get; set; } = 1000;
    
    /// <summary>
    /// Sadece güncellenmiş ürünleri gönder
    /// </summary>
    public bool OnlyUpdated { get; set; } = false;
    
    /// <summary>
    /// Son X saat içinde güncellenenler (OnlyUpdated=true ise)
    /// </summary>
    public int UpdatedWithinHours { get; set; } = 24;
}

/// <summary>
/// Batch job oluşturma yanıtı
/// </summary>
public class BatchJobCreatedResponse
{
    public string JobId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int TotalProducts { get; set; }
    public int TotalBatches { get; set; }
    public int BatchSize { get; set; }
    public DateTime CreatedAt { get; set; }
    public string StatusUrl { get; set; } = string.Empty;
}

/// <summary>
/// Batch job durumu
/// </summary>
public class BatchJobStatusDto
{
    public string JobId { get; set; } = string.Empty;
    public BatchJobStatus Status { get; set; }
    public BatchJobType JobType { get; set; }
    public int TotalItems { get; set; }
    public int ProcessedItems { get; set; }
    public int SuccessfulItems { get; set; }
    public int FailedItems { get; set; }
    public int CurrentBatch { get; set; }
    public int TotalBatches { get; set; }
    public double ProgressPercentage => TotalItems > 0 ? Math.Round((double)ProcessedItems / TotalItems * 100, 2) : 0;
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan? ElapsedTime => StartedAt.HasValue 
        ? (CompletedAt ?? DateTime.UtcNow) - StartedAt.Value 
        : null;
    public TimeSpan? EstimatedTimeRemaining { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<BatchItemResult> FailedItemDetails { get; set; } = new();
    public string? CancelledBy { get; set; }
    public DateTime? CancelledAt { get; set; }
}

/// <summary>
/// Tek bir batch öğesi sonucu
/// </summary>
public class BatchItemResult
{
    public int ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime ProcessedAt { get; set; }
}

/// <summary>
/// Batch job listesi için özet
/// </summary>
public class BatchJobSummaryDto
{
    public string JobId { get; set; } = string.Empty;
    public BatchJobType JobType { get; set; }
    public BatchJobStatus Status { get; set; }
    public int TotalItems { get; set; }
    public int ProcessedItems { get; set; }
    public double ProgressPercentage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}

/// <summary>
/// Batch job kuyruğu öğesi (in-memory queue için)
/// </summary>
public class BatchJobItem
{
    public string JobId { get; set; } = string.Empty;
    public BatchJobType JobType { get; set; }
    public BatchJobStatus Status { get; set; }
    public List<int> ItemIds { get; set; } = new();
    public int BatchSize { get; set; }
    public int DelayBetweenBatchesMs { get; set; }
    public int TotalItems { get; set; }
    public int ProcessedItems { get; set; }
    public int SuccessfulItems { get; set; }
    public int FailedItems { get; set; }
    public int CurrentBatch { get; set; }
    public int TotalBatches { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
    public List<BatchItemResult> FailedItemDetails { get; set; } = new();
    public CancellationTokenSource? CancellationTokenSource { get; set; }
}

/// <summary>
/// Aktif batch job'ları listele yanıtı
/// </summary>
public class ActiveBatchJobsResponse
{
    public List<BatchJobSummaryDto> Jobs { get; set; } = new();
    public int TotalJobs { get; set; }
    public int RunningJobs { get; set; }
    public int PendingJobs { get; set; }
}

/// <summary>
/// Batch job iptal isteği
/// </summary>
public class CancelBatchJobRequest
{
    public string Reason { get; set; } = string.Empty;
}
