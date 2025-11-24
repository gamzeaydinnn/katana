using System.Text.Json.Serialization;

namespace Katana.Core.DTOs;

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
