namespace Katana.Data.Configuration;

public class SyncSettings
{
    public const string SectionName = "Sync";
    
    public int BatchSize { get; set; } = 100;
    public int RetryCount { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 5;
    public string ScheduleInterval { get; set; } = "0 */6 * * *"; // Every 6 hours
    public bool EnableAutoSync { get; set; } = true;
    public bool EnableParallelProcessing { get; set; } = true;
    public int MaxParallelTasks { get; set; } = 5;
    public int SyncHistoryRetentionDays { get; set; } = 30;
    
    // Sync type specific settings
    public SyncTypeSettings Stock { get; set; } = new();
    public SyncTypeSettings Invoice { get; set; } = new();
    public SyncTypeSettings Customer { get; set; } = new();
}

public class SyncTypeSettings
{
    public bool Enabled { get; set; } = true;
    public int BatchSize { get; set; } = 100;
    public int SyncIntervalMinutes { get; set; } = 360; // 6 hours
    public DateTime? LastSyncTime { get; set; }
}

