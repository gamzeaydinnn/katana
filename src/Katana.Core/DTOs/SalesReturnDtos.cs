using System.Text.Json.Serialization;

namespace Katana.Core.DTOs;

public class SalesReturnDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("customer_id")]
    public long? CustomerId { get; set; }

    [JsonPropertyName("sales_order_id")]
    public long SalesOrderId { get; set; }

    [JsonPropertyName("order_no")]
    public string? OrderNo { get; set; }

    [JsonPropertyName("return_location_id")]
    public long ReturnLocationId { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("return_date")]
    public DateTime? ReturnDate { get; set; }

    [JsonPropertyName("order_created_date")]
    public DateTime? OrderCreatedDate { get; set; }

    [JsonPropertyName("additional_info")]
    public string? AdditionalInfo { get; set; }

    [JsonPropertyName("refund_status")]
    public string? RefundStatus { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

public class SalesReturnCreateRequest
{
    [JsonPropertyName("sales_order_id")]
    public long SalesOrderId { get; set; }

    [JsonPropertyName("order_created_date")]
    public DateTime? OrderCreatedDate { get; set; }

    [JsonPropertyName("return_location_id")]
    public long ReturnLocationId { get; set; }

    [JsonPropertyName("order_no")]
    public string? OrderNo { get; set; }

    [JsonPropertyName("additional_info")]
    public string? AdditionalInfo { get; set; }
}

public class SalesReturnUpdateRequest
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("additional_info")]
    public string? AdditionalInfo { get; set; }
}

public class SalesReturnListQuery
{
    [JsonPropertyName("ids")]
    public List<long>? Ids { get; set; }

    [JsonPropertyName("return_order_no")]
    public string? ReturnOrderNo { get; set; }

    [JsonPropertyName("sales_order_id")]
    public long? SalesOrderId { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("refund_status")]
    public string? RefundStatus { get; set; }

    [JsonPropertyName("return_date_min")]
    public DateTime? ReturnDateMin { get; set; }

    [JsonPropertyName("return_date_max")]
    public DateTime? ReturnDateMax { get; set; }

    [JsonPropertyName("order_created_date_min")]
    public DateTime? OrderCreatedDateMin { get; set; }

    [JsonPropertyName("order_created_date_max")]
    public DateTime? OrderCreatedDateMax { get; set; }

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

public class SalesReturnReasonDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class SalesReturnRowDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("sales_return_id")]
    public long SalesReturnId { get; set; }

    [JsonPropertyName("variant_id")]
    public long VariantId { get; set; }

    [JsonPropertyName("fulfillment_row_id")]
    public long FulfillmentRowId { get; set; }

    [JsonPropertyName("sales_order_row_id")]
    public long? SalesOrderRowId { get; set; }

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }

    [JsonPropertyName("net_price_per_unit")]
    public decimal? NetPricePerUnit { get; set; }

    [JsonPropertyName("reason_id")]
    public long? ReasonId { get; set; }

    [JsonPropertyName("restock_location_id")]
    public long? RestockLocationId { get; set; }

    [JsonPropertyName("batch_transactions")]
    public List<SalesReturnBatchTransactionDto>? BatchTransactions { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

public class SalesReturnBatchTransactionDto
{
    [JsonPropertyName("batch_id")]
    public long BatchId { get; set; }

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }
}

public class SalesReturnUnassignedBatchTransactionDto
{
    [JsonPropertyName("batch_id")]
    public long BatchId { get; set; }

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }

    [JsonPropertyName("batch_number")]
    public string? BatchNumber { get; set; }

    [JsonPropertyName("batch_created_date")]
    public DateTime? BatchCreatedDate { get; set; }

    [JsonPropertyName("batch_expiration_date")]
    public DateTime? BatchExpirationDate { get; set; }

    [JsonPropertyName("barcode")]
    public string? Barcode { get; set; }
}

public class SalesReturnRowCreateRequest
{
    [JsonPropertyName("sales_return_id")]
    public long SalesReturnId { get; set; }

    [JsonPropertyName("variant_id")]
    public long VariantId { get; set; }

    [JsonPropertyName("fulfillment_row_id")]
    public long FulfillmentRowId { get; set; }

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }

    [JsonPropertyName("restock_location_id")]
    public long? RestockLocationId { get; set; }

    [JsonPropertyName("reason_id")]
    public long? ReasonId { get; set; }
}

public class SalesReturnRowUpdateRequest
{
    [JsonPropertyName("quantity")]
    public decimal? Quantity { get; set; }

    [JsonPropertyName("restock_location_id")]
    public long? RestockLocationId { get; set; }

    [JsonPropertyName("reason_id")]
    public long? ReasonId { get; set; }

    [JsonPropertyName("batch_transactions")]
    public List<SalesReturnBatchTransactionDto>? BatchTransactions { get; set; }
}

public class SalesReturnRowListQuery
{
    [JsonPropertyName("ids")]
    public List<long>? Ids { get; set; }

    [JsonPropertyName("sales_return_id")]
    public long? SalesReturnId { get; set; }

    [JsonPropertyName("variant_id")]
    public long? VariantId { get; set; }

    [JsonPropertyName("sales_order_row_id")]
    public long? SalesOrderRowId { get; set; }

    [JsonPropertyName("reason_id")]
    public long? ReasonId { get; set; }

    [JsonPropertyName("restock_location_id")]
    public long? RestockLocationId { get; set; }

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
