namespace Katana.Core.DTOs;
public class SyncResultDto
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public int ProcessedRecords { get; set; }
    public int SuccessfulRecords { get; set; }
    public int FailedRecords { get; set; }
    public int DuplicateRecords { get; set; }
    public int SentRecords { get; set; }
    public int SkippedRecords { get; set; }
    public bool IsDryRun { get; set; }
    public List<string> Errors { get; set; } = new();
    public DateTime SyncTime { get; set; } = DateTime.UtcNow;
    public string SyncType { get; set; } = string.Empty; 
    public TimeSpan Duration { get; set; }

    
    public int TotalChecked { get; set; }
    public int AlreadyExists { get; set; }
    public int NewCreated { get; set; }
    public int Failed { get; set; }
    public List<string> Details { get; set; } = new();
}

public class BatchSyncResultDto
{
    public List<SyncResultDto> Results { get; set; } = new();
    public bool OverallSuccess => Results.All(r => r.IsSuccess);
    public int TotalProcessedRecords => Results.Sum(r => r.ProcessedRecords);
    public int TotalSuccessfulRecords => Results.Sum(r => r.SuccessfulRecords);
    public int TotalFailedRecords => Results.Sum(r => r.FailedRecords);
    public DateTime BatchTime { get; set; } = DateTime.UtcNow;
    public TimeSpan TotalDuration => TimeSpan.FromTicks(Results.Sum(r => r.Duration.Ticks));
}

public class SyncStatusDto
{
    public string SyncType { get; set; } = string.Empty;
    public DateTime? LastSyncTime { get; set; }
    public bool IsRunning { get; set; }
    public string? CurrentStatus { get; set; }
    public int PendingRecords { get; set; }
    public DateTime? NextScheduledSync { get; set; }
}
public class SyncOptionsDto
{
    public bool DryRun { get; set; }
    public bool PreferBarcodeMatch { get; set; } = true;
    /// <summary>
    /// true: Tüm ürünleri gönder (mevcut olanlar dahil)
    /// false: Sadece yeni veya değişen ürünleri gönder (varsayılan)
    /// </summary>
    public bool ForceSendDuplicates { get; set; } = false;
    public int? Limit { get; set; }
}

public class StockComparisonDto
{
    public string Sku { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool ExistsInLuca { get; set; }
    public string? LucaCode { get; set; }
    public string? LucaBarcode { get; set; }
    public string Status { get; set; } = "UNKNOWN";
}

