using System.Text.Json.Serialization;

namespace Katana.Core.DTOs;

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
    public string Status { get; set; } = string.Empty; // created | received
}
