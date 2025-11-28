using System.Text.Json.Serialization;

namespace Katana.Core.DTOs;

#region Inventory
public class InventoryDto
{
    [JsonPropertyName("variant_id")]
    public long VariantId { get; set; }

    [JsonPropertyName("location_id")]
    public long LocationId { get; set; }

    [JsonPropertyName("safety_stock_level")]
    public decimal? SafetyStockLevel { get; set; }

    [JsonPropertyName("reorder_point")]
    public decimal? ReorderPoint { get; set; }

    [JsonPropertyName("average_cost")]
    public decimal? AverageCost { get; set; }

    [JsonPropertyName("value_in_stock")]
    public decimal? ValueInStock { get; set; }

    [JsonPropertyName("quantity_in_stock")]
    public decimal? QuantityInStock { get; set; }

    [JsonPropertyName("quantity_committed")]
    public decimal? QuantityCommitted { get; set; }

    [JsonPropertyName("quantity_expected")]
    public decimal? QuantityExpected { get; set; }

    [JsonPropertyName("quantity_missing_or_excess")]
    public decimal? QuantityMissingOrExcess { get; set; }

    [JsonPropertyName("quantity_potential")]
    public decimal? QuantityPotential { get; set; }
}




public class InventoryListQuery
{
    [JsonPropertyName("location_id")]
    public long? LocationId { get; set; }

    [JsonPropertyName("variant_id")]
    public List<long>? VariantIds { get; set; }

    [JsonPropertyName("include_archived")]
    public bool? IncludeArchived { get; set; }

    [JsonPropertyName("extend")]
    public List<string>? Extend { get; set; }

    [JsonPropertyName("limit")]
    public int? Limit { get; set; }

    [JsonPropertyName("page")]
    public int? Page { get; set; }
}

public class InventorySafetyStockUpdateRequest
{
    [JsonPropertyName("location_id")]
    public long LocationId { get; set; }

    [JsonPropertyName("variant_id")]
    public long VariantId { get; set; }

    [JsonPropertyName("value")]
    public decimal Value { get; set; }
}

public class InventoryReorderPointUpdateRequest
{
    [JsonPropertyName("location_id")]
    public long LocationId { get; set; }

    [JsonPropertyName("variant_id")]
    public long VariantId { get; set; }

    [JsonPropertyName("value")]
    public decimal Value { get; set; }
}

public class NegativeStockDto
{
    [JsonPropertyName("location_id")]
    public long LocationId { get; set; }

    [JsonPropertyName("variant_id")]
    public long VariantId { get; set; }

    [JsonPropertyName("latest_negative_stock_date")]
    public DateTime? LatestNegativeStockDate { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("sku")]
    public string? Sku { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }
}

public class NegativeStockListQuery
{
    [JsonPropertyName("location_id")]
    public long? LocationId { get; set; }

    [JsonPropertyName("variant_id")]
    public long? VariantId { get; set; }

    [JsonPropertyName("latest_negative_stock_date_max")]
    public DateTime? LatestNegativeStockDateMax { get; set; }

    [JsonPropertyName("latest_negative_stock_date_min")]
    public DateTime? LatestNegativeStockDateMin { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("sku")]
    public string? Sku { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("limit")]
    public int? Limit { get; set; }

    [JsonPropertyName("page")]
    public int? Page { get; set; }
}

#endregion

#region Stock Adjustment




public class StockAdjustmentDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("stock_adjustment_date")]
    public DateTime StockAdjustmentDate { get; set; }

    [JsonPropertyName("location_id")]
    public long LocationId { get; set; }

    [JsonPropertyName("stock_adjustment_number")]
    public string StockAdjustmentNumber { get; set; } = string.Empty;

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("additional_info")]
    public string? AdditionalInfo { get; set; }

    [JsonPropertyName("stock_adjustment_rows")]
    public List<StockAdjustmentRowDto> StockAdjustmentRows { get; set; } = new();

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

public class StockAdjustmentRowDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("variant_id")]
    public long VariantId { get; set; }

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }

    [JsonPropertyName("cost_per_unit")]
    public decimal? CostPerUnit { get; set; }

    [JsonPropertyName("batch_transactions")]
    public List<ManufacturingOrderBatchTransactionDto>? BatchTransactions { get; set; }
}

public class StockAdjustmentCreateRequest
{
    [JsonPropertyName("stock_adjustment_number")]
    public string StockAdjustmentNumber { get; set; } = string.Empty;

    [JsonPropertyName("stock_adjustment_date")]
    public DateTime StockAdjustmentDate { get; set; }

    [JsonPropertyName("location_id")]
    public long LocationId { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("additional_info")]
    public string? AdditionalInfo { get; set; }

    [JsonPropertyName("stock_adjustment_rows")]
    public List<StockAdjustmentRowDto> StockAdjustmentRows { get; set; } = new();
}

public class StockAdjustmentUpdateRequest
{
    [JsonPropertyName("stock_adjustment_number")]
    public string? StockAdjustmentNumber { get; set; }

    [JsonPropertyName("stock_adjustment_date")]
    public DateTime? StockAdjustmentDate { get; set; }

    [JsonPropertyName("location_id")]
    public long? LocationId { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("additional_info")]
    public string? AdditionalInfo { get; set; }
}

public class StockAdjustmentListQuery
{
    [JsonPropertyName("ids")]
    public List<long>? Ids { get; set; }

    [JsonPropertyName("stock_adjustment_number")]
    public string? StockAdjustmentNumber { get; set; }

    [JsonPropertyName("location_id")]
    public long? LocationId { get; set; }

    [JsonPropertyName("include_deleted")]
    public bool? IncludeDeleted { get; set; }

    [JsonPropertyName("limit")]
    public int? Limit { get; set; }

    [JsonPropertyName("page")]
    public int? Page { get; set; }

    [JsonPropertyName("created_at_min")]
    public DateTime? CreatedAtMin { get; set; }

    [JsonPropertyName("created_at_max")]
    public DateTime? CreatedAtMax { get; set; }

    [JsonPropertyName("updated_at_min")]
    public DateTime? UpdatedAtMin { get; set; }

    [JsonPropertyName("updated_at_max")]
    public DateTime? UpdatedAtMax { get; set; }
}

#endregion

#region Stocktake




public class StocktakeDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("stocktake_number")]
    public string StocktakeNumber { get; set; } = string.Empty;

    [JsonPropertyName("location_id")]
    public long LocationId { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("additional_info")]
    public string? AdditionalInfo { get; set; }

    [JsonPropertyName("stocktake_created_date")]
    public DateTime? StocktakeCreatedDate { get; set; }

    [JsonPropertyName("started_date")]
    public DateTime? StartedDate { get; set; }

    [JsonPropertyName("completed_date")]
    public DateTime? CompletedDate { get; set; }

    [JsonPropertyName("status_update_in_progress")]
    public bool? StatusUpdateInProgress { get; set; }

    [JsonPropertyName("set_remaining_items_as_counted")]
    public bool? SetRemainingItemsAsCounted { get; set; }

    [JsonPropertyName("stock_adjustment_id")]
    public long? StockAdjustmentId { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("deleted_at")]
    public DateTime? DeletedAt { get; set; }
}

public class StocktakeCreateRequest
{
    [JsonPropertyName("stocktake_number")]
    public string StocktakeNumber { get; set; } = string.Empty;

    [JsonPropertyName("location_id")]
    public long LocationId { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("additional_info")]
    public string? AdditionalInfo { get; set; }

    [JsonPropertyName("created_date")]
    public DateTime CreatedDate { get; set; }

    [JsonPropertyName("set_remaining_items_as_counted")]
    public bool? SetRemainingItemsAsCounted { get; set; }

    [JsonPropertyName("stocktake_rows")]
    public List<StocktakeRowDto>? StocktakeRows { get; set; }
}

public class StocktakeRowDto
{
    [JsonPropertyName("variant_id")]
    public long VariantId { get; set; }

    [JsonPropertyName("batch_id")]
    public long? BatchId { get; set; }

    [JsonPropertyName("quantity")]
    public decimal? Quantity { get; set; }

    [JsonPropertyName("stocktake_id")]
    public long? StocktakeId { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("in_stock_quantity")]
    public decimal? InStockQuantity { get; set; }

    [JsonPropertyName("counted_quantity")]
    public decimal? CountedQuantity { get; set; }

    [JsonPropertyName("discrepancy_quantity")]
    public decimal? DiscrepancyQuantity { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [JsonPropertyName("deleted_at")]
    public DateTime? DeletedAt { get; set; }
}

public class StocktakeUpdateRequest
{
    [JsonPropertyName("stocktake_number")]
    public string? StocktakeNumber { get; set; }

    [JsonPropertyName("location_id")]
    public long? LocationId { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("additional_info")]
    public string? AdditionalInfo { get; set; }

    [JsonPropertyName("created_date")]
    public DateTime? CreatedDate { get; set; }

    [JsonPropertyName("completed_date")]
    public DateTime? CompletedDate { get; set; }

    [JsonPropertyName("set_remaining_items_as_counted")]
    public bool? SetRemainingItemsAsCounted { get; set; }
}

public class StocktakeRowCreateRequest
{
    [JsonPropertyName("stocktake_id")]
    public long StocktakeId { get; set; }

    [JsonPropertyName("stocktake_rows")]
    public List<StocktakeRowDto> StocktakeRows { get; set; } = new();
}

public class StocktakeRowUpdateRequest
{
    [JsonPropertyName("variant_id")]
    public long? VariantId { get; set; }

    [JsonPropertyName("batch_id")]
    public long? BatchId { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("counted_quantity")]
    public decimal? CountedQuantity { get; set; }
}

public class StocktakeRowListQuery
{
    [JsonPropertyName("ids")]
    public List<long>? Ids { get; set; }

    [JsonPropertyName("stocktake_ids")]
    public List<long>? StocktakeIds { get; set; }

    [JsonPropertyName("variant_id")]
    public long? VariantId { get; set; }

    [JsonPropertyName("batch_id")]
    public long? BatchId { get; set; }

    [JsonPropertyName("stock_adjustment_id")]
    public long? StockAdjustmentId { get; set; }

    [JsonPropertyName("include_deleted")]
    public bool? IncludeDeleted { get; set; }

    [JsonPropertyName("limit")]
    public int? Limit { get; set; }

    [JsonPropertyName("page")]
    public int? Page { get; set; }

    [JsonPropertyName("created_at_min")]
    public DateTime? CreatedAtMin { get; set; }

    [JsonPropertyName("created_at_max")]
    public DateTime? CreatedAtMax { get; set; }

    [JsonPropertyName("updated_at_min")]
    public DateTime? UpdatedAtMin { get; set; }

    [JsonPropertyName("updated_at_max")]
    public DateTime? UpdatedAtMax { get; set; }
}

public class StocktakeListQuery
{
    [JsonPropertyName("ids")]
    public List<long>? Ids { get; set; }

    [JsonPropertyName("stocktake_number")]
    public string? StocktakeNumber { get; set; }

    [JsonPropertyName("location_id")]
    public long? LocationId { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("stock_adjustment_id")]
    public long? StockAdjustmentId { get; set; }

    [JsonPropertyName("include_deleted")]
    public bool? IncludeDeleted { get; set; }

    [JsonPropertyName("limit")]
    public int? Limit { get; set; }

    [JsonPropertyName("page")]
    public int? Page { get; set; }

    [JsonPropertyName("created_at_min")]
    public DateTime? CreatedAtMin { get; set; }

    [JsonPropertyName("created_at_max")]
    public DateTime? CreatedAtMax { get; set; }

    [JsonPropertyName("updated_at_min")]
    public DateTime? UpdatedAtMin { get; set; }

    [JsonPropertyName("updated_at_max")]
    public DateTime? UpdatedAtMax { get; set; }
}

#endregion

#region Stock Transfer




public class StockTransferDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("stock_transfer_number")]
    public string StockTransferNumber { get; set; } = string.Empty;

    [JsonPropertyName("source_location_id")]
    public long SourceLocationId { get; set; }

    [JsonPropertyName("target_location_id")]
    public long TargetLocationId { get; set; }

    [JsonPropertyName("transfer_date")]
    public DateTime TransferDate { get; set; }

    [JsonPropertyName("order_created_date")]
    public DateTime? OrderCreatedDate { get; set; }

    [JsonPropertyName("expected_arrival_date")]
    public DateTime? ExpectedArrivalDate { get; set; }

    [JsonPropertyName("additional_info")]
    public string? AdditionalInfo { get; set; }

    [JsonPropertyName("stock_transfer_rows")]
    public List<StockTransferRowDto> StockTransferRows { get; set; } = new();

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

public class StockTransferRowDto
{
    [JsonPropertyName("variant_id")]
    public long VariantId { get; set; }

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }

    [JsonPropertyName("batch_transactions")]
    public List<ManufacturingOrderBatchTransactionDto>? BatchTransactions { get; set; }
}

public class StockTransferCreateRequest
{
    [JsonPropertyName("stock_transfer_number")]
    public string StockTransferNumber { get; set; } = string.Empty;

    [JsonPropertyName("source_location_id")]
    public long SourceLocationId { get; set; }

    [JsonPropertyName("target_location_id")]
    public long TargetLocationId { get; set; }

    [JsonPropertyName("transfer_date")]
    public DateTime TransferDate { get; set; }

    [JsonPropertyName("order_created_date")]
    public DateTime? OrderCreatedDate { get; set; }

    [JsonPropertyName("expected_arrival_date")]
    public DateTime? ExpectedArrivalDate { get; set; }

    [JsonPropertyName("additional_info")]
    public string? AdditionalInfo { get; set; }

    [JsonPropertyName("stock_transfer_rows")]
    public List<StockTransferRowDto> StockTransferRows { get; set; } = new();
}

public class StockTransferListQuery
{
    [JsonPropertyName("ids")]
    public List<long>? Ids { get; set; }

    [JsonPropertyName("stock_transfer_number")]
    public string? StockTransferNumber { get; set; }

    [JsonPropertyName("source_location_id")]
    public long? SourceLocationId { get; set; }

    [JsonPropertyName("target_location_id")]
    public long? TargetLocationId { get; set; }

    [JsonPropertyName("include_deleted")]
    public bool? IncludeDeleted { get; set; }

    [JsonPropertyName("limit")]
    public int? Limit { get; set; }

    [JsonPropertyName("page")]
    public int? Page { get; set; }

    [JsonPropertyName("created_at_min")]
    public DateTime? CreatedAtMin { get; set; }

    [JsonPropertyName("created_at_max")]
    public DateTime? CreatedAtMax { get; set; }

    [JsonPropertyName("updated_at_min")]
    public DateTime? UpdatedAtMin { get; set; }

    [JsonPropertyName("updated_at_max")]
    public DateTime? UpdatedAtMax { get; set; }
}

public class StockTransferUpdateRequest
{
    [JsonPropertyName("stock_transfer_number")]
    public string? StockTransferNumber { get; set; }

    [JsonPropertyName("transfer_date")]
    public DateTime? TransferDate { get; set; }

    [JsonPropertyName("order_created_date")]
    public DateTime? OrderCreatedDate { get; set; }

    [JsonPropertyName("expected_arrival_date")]
    public DateTime? ExpectedArrivalDate { get; set; }

    [JsonPropertyName("additional_info")]
    public string? AdditionalInfo { get; set; }
}

public class StockTransferStatusUpdateRequest
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}

#endregion

#region Inventory Movement




public class InventoryMovementDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("variant_id")]
    public long VariantId { get; set; }

    [JsonPropertyName("location_id")]
    public long LocationId { get; set; }

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }

    [JsonPropertyName("resource_type")]
    public string? ResourceType { get; set; }

    [JsonPropertyName("resource_id")]
    public long? ResourceId { get; set; }

    [JsonPropertyName("caused_by_order_no")]
    public string? CausedByOrderNo { get; set; }

    [JsonPropertyName("caused_by_resource_id")]
    public long? CausedByResourceId { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

public class InventoryMovementListQuery
{
    [JsonPropertyName("ids")]
    public List<long>? Ids { get; set; }

    [JsonPropertyName("variant_ids")]
    public List<long>? VariantIds { get; set; }

    [JsonPropertyName("location_id")]
    public long? LocationId { get; set; }

    [JsonPropertyName("resource_type")]
    public string? ResourceType { get; set; }

    [JsonPropertyName("resource_id")]
    public long? ResourceId { get; set; }

    [JsonPropertyName("caused_by_order_no")]
    public string? CausedByOrderNo { get; set; }

    [JsonPropertyName("caused_by_resource_id")]
    public long? CausedByResourceId { get; set; }

    [JsonPropertyName("limit")]
    public int? Limit { get; set; }

    [JsonPropertyName("page")]
    public int? Page { get; set; }

    [JsonPropertyName("created_at_min")]
    public DateTime? CreatedAtMin { get; set; }

    [JsonPropertyName("created_at_max")]
    public DateTime? CreatedAtMax { get; set; }

    [JsonPropertyName("updated_at_min")]
    public DateTime? UpdatedAtMin { get; set; }

    [JsonPropertyName("updated_at_max")]
    public DateTime? UpdatedAtMax { get; set; }
}

#endregion

