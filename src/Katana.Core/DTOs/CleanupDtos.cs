namespace Katana.Core.DTOs;

public class OrderProductAnalysisResult
{
    public int TotalApprovedOrders { get; set; }
    public int TotalProductsSentToKatana { get; set; }
    public int UniqueSkuCount { get; set; }
    public List<OrderProductInfo> OrderProducts { get; set; } = new();
    public Dictionary<string, int> SkuDuplicates { get; set; } = new();
}

public class OrderProductInfo
{
    public int OrderId { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int? KatanaOrderId { get; set; }
    public DateTime ApprovedDate { get; set; }
}

public class KatanaCleanupResult
{
    public bool Success { get; set; }
    public int TotalAttempted { get; set; }
    public int SuccessCount { get; set; }
    public int FailCount { get; set; }
    public List<CleanupError> Errors { get; set; } = new();
    public TimeSpan Duration { get; set; }
}

public class CleanupError
{
    public string Message { get; set; } = string.Empty;
    public string ErrorType { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string? StackTrace { get; set; }
}

public class ResetResult
{
    public bool Success { get; set; }
    public int OrdersReset { get; set; }
    public int LinesAffected { get; set; }
    public int MappingsCleared { get; set; }
    public List<ResetError> Errors { get; set; } = new();
    public TimeSpan Duration { get; set; }
}

public class ResetError
{
    public int OrderId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
}

public class CleanupReport
{
    public OrderProductAnalysisResult? Analysis { get; set; }
    public KatanaCleanupResult? KatanaCleanup { get; set; }
    public ResetResult? Reset { get; set; }
    public DateTime GeneratedAt { get; set; }
}

public class BackupResult
{
    public bool Success { get; set; }
    public string? BackupId { get; set; }
    public string? BackupPath { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ErrorMessage { get; set; }
}

public class RollbackResult
{
    public bool Success { get; set; }
    public string? BackupId { get; set; }
    public DateTime RestoredAt { get; set; }
    public string? ErrorMessage { get; set; }
}

public class CleanupOperation
{
    public int Id { get; set; }
    public string OperationType { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? Parameters { get; set; }
    public string? Result { get; set; }
    public string? ErrorMessage { get; set; }
}

public class CleanupLog
{
    public int Id { get; set; }
    public int CleanupOperationId { get; set; }
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public int? EntityId { get; set; }
    public string? Details { get; set; }
}
