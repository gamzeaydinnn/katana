namespace Katana.Core.DTOs;

#region Sales Order DTOs (Local Entity)

/// <summary>
/// Sales Order DTO for list and detail views (local entity, not Katana API DTO)
/// </summary>
public class LocalSalesOrderDto
{
    public int Id { get; set; }
    public long KatanaOrderId { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public DateTime? OrderCreatedDate { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public string? Currency { get; set; }
    public string Status { get; set; } = "NOT_SHIPPED";
    public decimal? Total { get; set; }
    public decimal? TotalInBaseCurrency { get; set; }
    public string? AdditionalInfo { get; set; }
    public string? CustomerRef { get; set; }
    public string? Source { get; set; }
    public long? LocationId { get; set; }
    
    // Luca integration fields
    public int? LucaOrderId { get; set; }
    public string? BelgeSeri { get; set; }
    public string? BelgeNo { get; set; }
    public string? DuzenlemeSaati { get; set; }
    public int? BelgeTurDetayId { get; set; }
    public int? NakliyeBedeliTuru { get; set; }
    public int? TeklifSiparisTur { get; set; }
    public bool OnayFlag { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public string? LastSyncError { get; set; }
    public bool IsSyncedToLuca { get; set; }
    
    // Lines
    public List<LocalSalesOrderLineDto> Lines { get; set; } = new();
    
    // Computed properties for UI
    public string LucaSyncStatus => IsSyncedToLuca && string.IsNullOrEmpty(LastSyncError)
        ? "synced"
        : (!string.IsNullOrEmpty(LastSyncError) ? "error" : "not_synced");
}

/// <summary>
/// Sales Order Line DTO (local entity)
/// </summary>
public class LocalSalesOrderLineDto
{
    public int Id { get; set; }
    public int SalesOrderId { get; set; }
    public long KatanaRowId { get; set; }
    public long VariantId { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string? ProductName { get; set; }
    public decimal Quantity { get; set; }
    public decimal? PricePerUnit { get; set; }
    public decimal? PricePerUnitInBaseCurrency { get; set; }
    public decimal? Total { get; set; }
    public decimal? TotalInBaseCurrency { get; set; }
    public decimal? TaxRate { get; set; }
    public long? TaxRateId { get; set; }
    public long? LocationId { get; set; }
    public string? ProductAvailability { get; set; }
    public DateTime? ProductExpectedDate { get; set; }
    
    // Luca fields
    public int? LucaDetayId { get; set; }
    public int? LucaStokId { get; set; }
    public int? LucaDepoId { get; set; }
}

/// <summary>
/// Sales Order summary for list view
/// </summary>
public class SalesOrderSummaryDto
{
    public int Id { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public DateTime? OrderCreatedDate { get; set; }
    public string Status { get; set; } = "NOT_SHIPPED";
    public string? Currency { get; set; }
    public decimal? Total { get; set; }
    
    // Luca sync status
    public int? LucaOrderId { get; set; }
    public bool IsSyncedToLuca { get; set; }
    public string? LastSyncError { get; set; }
    public DateTime? LastSyncAt { get; set; }
    
    public string LucaSyncStatus => IsSyncedToLuca && string.IsNullOrEmpty(LastSyncError)
        ? "synced"
        : (!string.IsNullOrEmpty(LastSyncError) ? "error" : "not_synced");
}

/// <summary>
/// Grouped sales order DTO (by KatanaOrderId)
/// </summary>
public class GroupedSalesOrderDto
{
    public long GroupKatanaOrderId { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public List<string> OrderNos { get; set; } = new();
    public int CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public DateTime? OrderCreatedDate { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public string? Currency { get; set; }
    public string Status { get; set; } = "NOT_SHIPPED";
    public decimal? Total { get; set; }
    public decimal? TotalInBaseCurrency { get; set; }
    public bool IsSyncedToLuca { get; set; }
    public string? LastSyncError { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public List<LocalSalesOrderLineDto> Lines { get; set; } = new();
}

/// <summary>
/// DTO for updating Luca-specific fields
/// </summary>
public class UpdateSalesOrderLucaFieldsDto
{
    public string? BelgeSeri { get; set; }
    public string? BelgeNo { get; set; }
    public string? DuzenlemeSaati { get; set; }
    public int? BelgeTurDetayId { get; set; }
    public int? NakliyeBedeliTuru { get; set; }
    public int? TeklifSiparisTur { get; set; }
    public bool? OnayFlag { get; set; }
    public string? BelgeAciklama { get; set; }
}

/// <summary>
/// Sync result DTO
/// </summary>
public class SalesOrderSyncResultDto
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? LucaOrderId { get; set; }
    public DateTime? SyncedAt { get; set; }
    public string? ErrorDetails { get; set; }
}

/// <summary>
/// Sync status DTO
/// </summary>
public class SalesOrderSyncStatusDto
{
    public int SalesOrderId { get; set; }
    public int? LucaOrderId { get; set; }
    public bool IsSyncedToLuca { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public string? LastSyncError { get; set; }
    public string Status { get; set; } = "not_synced"; // synced, error, not_synced
}

#endregion

#region Purchase Order DTOs (Local Entity)

/// <summary>
/// Purchase Order DTO for list and detail views (local entity)
/// </summary>
public class LocalPurchaseOrderDto
{
    public int Id { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public string? SupplierCode { get; set; }
    public string Status { get; set; } = "Pending";
    public decimal TotalAmount { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime? ExpectedDate { get; set; }
    public bool IsSynced { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Luca integration
    public int? LucaOrderId { get; set; }
    public string? BelgeSeri { get; set; }
    public string? BelgeNo { get; set; }
    public string? Currency { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public string? LastSyncError { get; set; }
    
    public List<LocalPurchaseOrderItemDto> Items { get; set; } = new();
    
    public string LucaSyncStatus => LucaOrderId.HasValue && string.IsNullOrEmpty(LastSyncError)
        ? "synced"
        : (!string.IsNullOrEmpty(LastSyncError) ? "error" : "not_synced");
}

/// <summary>
/// Purchase Order Item DTO (local entity)
/// </summary>
public class LocalPurchaseOrderItemDto
{
    public int Id { get; set; }
    public int PurchaseOrderId { get; set; }
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public string? SKU { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal? TaxRate { get; set; }
    public decimal TotalPrice { get; set; }
    public int? LucaStokId { get; set; }
    public int? LucaDepoId { get; set; }
}

/// <summary>
/// Create Purchase Order DTO
/// </summary>
public class CreatePurchaseOrderDto
{
    public int SupplierId { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public DateTime? ExpectedDate { get; set; }
    public string? Currency { get; set; } = "TRY";
    
    // Luca-specific fields
    public string? BelgeSeri { get; set; }
    public int? BelgeTurDetayId { get; set; }
    public bool KdvFlag { get; set; } = true;
    
    public List<CreatePurchaseOrderItemDto> Items { get; set; } = new();
}

/// <summary>
/// Create Purchase Order Item DTO
/// </summary>
public class CreatePurchaseOrderItemDto
{
    public int ProductId { get; set; }
    public string? KartKodu { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal? TaxRate { get; set; }
    public string? DepoKodu { get; set; }
}

/// <summary>
/// Purchase Order Sync Result
/// </summary>
public class PurchaseOrderSyncResultDto
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? LucaOrderId { get; set; }
    public DateTime? SyncedAt { get; set; }
    public string? ErrorDetails { get; set; }
}

#endregion


#region Order Grouped Summary DTOs

/// <summary>
/// Sipariş özeti - varyant gruplarıyla birlikte
/// </summary>
public class OrderGroupedSummaryDto
{
    public int OrderId { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public DateTime? OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Currency { get; set; }
    public int TotalLineCount { get; set; }
    public int UniqueProductCount { get; set; }
    public List<OrderProductGroupDto> ProductGroups { get; set; } = new();
    public decimal GrandTotal { get; set; }
    public decimal GrandTotalInBaseCurrency { get; set; }
}

/// <summary>
/// Sipariş içindeki ürün grubu (aynı ana ürünün varyantları)
/// </summary>
public class OrderProductGroupDto
{
    public string ProductBaseCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int VariantCount { get; set; }
    public List<LocalSalesOrderLineDto> Lines { get; set; } = new();
    public decimal SubtotalQuantity { get; set; }
    public decimal SubtotalAmount { get; set; }
    public decimal SubtotalAmountInBaseCurrency { get; set; }
}

#endregion
