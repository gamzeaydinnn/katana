namespace Katana.Business.DTOs.Sync
{
    public class SyncResultDto
    {
        public bool Success { get; set; }
        public int TotalProcessed { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public int SkippedCount { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new();
        public TimeSpan Duration { get; set; }
        public bool IsDryRun { get; set; }
    }

    public class SyncProgressDto
    {
        public string JobId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int ProcessedCount { get; set; }
        public int TotalCount { get; set; }
        public double ProgressPercentage => TotalCount > 0 ? (ProcessedCount * 100.0 / TotalCount) : 0;
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
