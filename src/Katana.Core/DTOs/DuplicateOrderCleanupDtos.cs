namespace Katana.Core.DTOs;

/// <summary>
/// Duplike sipariş analiz sonucu
/// </summary>
public class DuplicateOrderAnalysisResult
{
    public int TotalOrders { get; set; }
    public int DuplicateGroups { get; set; }
    public int OrdersToDelete { get; set; }
    public List<DuplicateOrderGroup> Groups { get; set; } = new();
}

/// <summary>
/// Aynı OrderNo'ya sahip sipariş grubu
/// </summary>
public class DuplicateOrderGroup
{
    public string OrderNo { get; set; } = string.Empty;
    public int Count { get; set; }
    public DuplicateOrderInfo OrderToKeep { get; set; } = new();
    public List<DuplicateOrderInfo> OrdersToDelete { get; set; } = new();
}

/// <summary>
/// Duplike sipariş bilgisi
/// </summary>
public class DuplicateOrderInfo
{
    public int Id { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public decimal? Total { get; set; }
    public string? Currency { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
    public long? KatanaOrderId { get; set; }
    public long? LucaOrderId { get; set; }
    public bool IsSyncedToLuca { get; set; }
    public string? KeepReason { get; set; }
}

/// <summary>
/// Bozuk OrderNo analiz sonucu
/// </summary>
public class MalformedOrderAnalysisResult
{
    public int TotalMalformed { get; set; }
    public int CanMerge { get; set; }
    public int CanRename { get; set; }
    public List<MalformedOrderInfo> Orders { get; set; } = new();
}

/// <summary>
/// Bozuk OrderNo bilgisi
/// </summary>
public class MalformedOrderInfo
{
    public int Id { get; set; }
    public string CurrentOrderNo { get; set; } = string.Empty;
    public string CorrectOrderNo { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty; // "Merge" or "Rename"
    public int? MergeTargetId { get; set; }
    public string? CustomerName { get; set; }
    public decimal? Total { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Temizlik isteği
/// </summary>
public class OrderCleanupRequest
{
    public bool DryRun { get; set; } = true;
}

/// <summary>
/// Temizlik sonucu
/// </summary>
public class OrderCleanupResult
{
    public bool Success { get; set; }
    public bool WasDryRun { get; set; }
    public int OrdersDeleted { get; set; }
    public int OrdersMerged { get; set; }
    public int OrdersRenamed { get; set; }
    public int LinesDeleted { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<OrderCleanupLogEntry> Log { get; set; } = new();
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// Temizlik log kaydı
/// </summary>
public class OrderCleanupLogEntry
{
    public int OrderId { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
