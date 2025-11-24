using System.Text.Json.Serialization;

namespace Katana.Core.DTOs;

public class StocktakeDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("stocktake_number")]
    public string StocktakeNumber { get; set; } = string.Empty;

    [JsonPropertyName("location_id")]
    public long LocationId { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty; // NOT_STARTED, IN_PROGRESS, COUNTED, COMPLETED

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
