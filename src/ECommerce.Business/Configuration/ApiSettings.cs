namespace ECommerce.Business.Configuration;

public class KatanaApiSettings
{
    public const string SectionName = "KatanaApi";
    
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetryAttempts { get; set; } = 3;
    public bool UseBasicAuth { get; set; } = false;
    
    // API Endpoints
    public KatanaEndpoints Endpoints { get; set; } = new();
}

public class KatanaEndpoints
{
    public string Products { get; set; } = "/api/products";
    public string Stock { get; set; } = "/api/stock/movements";
    public string Invoices { get; set; } = "/api/invoices";
    public string Customers { get; set; } = "/api/customers";
    public string Health { get; set; } = "/api/health";
}

public class LucaApiSettings
{
    public const string SectionName = "LucaApi";
    
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetryAttempts { get; set; } = 3;
    public bool UseTokenAuth { get; set; } = true;
    
    // API Endpoints
    public LucaEndpoints Endpoints { get; set; } = new();
}

public class LucaEndpoints
{
    public string Invoices { get; set; } = "/api/documents/invoices";
    public string Stock { get; set; } = "/api/inventory/movements";
    public string Customers { get; set; } = "/api/customers";
    public string Auth { get; set; } = "/api/auth/token";
    public string Health { get; set; } = "/api/health";
}

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