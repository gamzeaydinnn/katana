using System;

namespace Katana.Core.DTOs;




public class StockMovementDto
{
    public int Id { get; set; }

    
    
    
    public string SKU { get; set; } = string.Empty;

    
    
    
    public string ProductName { get; set; } = string.Empty;

    
    
    
    public string MovementType { get; set; } = "IN";

    
    
    
    public int Quantity { get; set; }

    
    
    
    public decimal UnitPrice { get; set; }

    
    
    
    public decimal TotalAmount { get; set; }

    
    
    
    public DateTime MovementDate { get; set; } = DateTime.UtcNow;

    
    
    
    public string SourceType { get; set; } = string.Empty;

    
    
    
    public string SourceReference { get; set; } = string.Empty;

    
    
    
    public string WarehouseCode { get; set; } = "MAIN";

    
    
    
    public string? Notes { get; set; }
}




public class LucaStockMovementDto
{
    public string SKU { get; set; } = string.Empty;
    public string AccountCode { get; set; } = string.Empty;
    public string MovementType { get; set; } = string.Empty;
        public int Quantity { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime MovementDate { get; set; } = DateTime.UtcNow;
    public string Warehouse { get; set; } = string.Empty;
}




public class StockMovementSyncResultDto
{
    public int ProcessedRecords { get; set; }
    public int SuccessfulRecords { get; set; }
    public int FailedRecords { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsSuccess => FailedRecords == 0;
}
