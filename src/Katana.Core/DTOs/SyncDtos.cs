namespace Katana.Core.DTOs;

public class SyncResultDto
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public int ProcessedRecords { get; set; }
    public int SuccessfulRecords { get; set; }
    public int FailedRecords { get; set; }
    public List<string> Errors { get; set; } = new();
    public DateTime SyncTime { get; set; } = DateTime.UtcNow;
    public string SyncType { get; set; } = string.Empty; 
    public TimeSpan Duration { get; set; }
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

