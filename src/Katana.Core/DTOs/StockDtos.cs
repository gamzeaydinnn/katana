namespace Katana.Core.DTOs;
public class StockDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSKU { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Reference { get; set; }
    public bool IsSynced { get; set; }
    public DateTime? SyncedAt { get; set; }
}

public class CreateStockMovementDto
{
    public int ProductId { get; set; }
    public string Location { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string? Reference { get; set; }
}

public class StockSummaryDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSKU { get; set; } = string.Empty;
    public int TotalStock { get; set; }
    public Dictionary<string, int> StockByLocation { get; set; } = new();
    public DateTime LastMovement { get; set; }
}
