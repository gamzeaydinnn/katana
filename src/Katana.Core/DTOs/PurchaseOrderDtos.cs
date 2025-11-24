using System.Text.Json.Serialization;

namespace Katana.Core.DTOs;

public class PurchaseOrderDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty; // NOT_RECEIVED, PARTIALLY_RECEIVED, RECEIVED

    [JsonPropertyName("billing_status")]
    public string? BillingStatus { get; set; }

    [JsonPropertyName("last_document_status")]
    public string? LastDocumentStatus { get; set; }

    [JsonPropertyName("order_no")]
    public string OrderNo { get; set; } = string.Empty;

    [JsonPropertyName("entity_type")]
    public string EntityType { get; set; } = "regular"; // regular | outsourced

    [JsonPropertyName("supplier_id")]
    public long SupplierId { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;

    [JsonPropertyName("expected_arrival_date")]
    public DateTime? ExpectedArrivalDate { get; set; }

    [JsonPropertyName("order_created_date")]
    public DateTime? OrderCreatedDate { get; set; }

    [JsonPropertyName("additional_info")]
    public string? AdditionalInfo { get; set; }

    [JsonPropertyName("location_id")]
    public long LocationId { get; set; }

    [JsonPropertyName("ingredient_availability")]
    public string? IngredientAvailability { get; set; }

    [JsonPropertyName("ingredient_expected_date")]
    public DateTime? IngredientExpectedDate { get; set; }

    [JsonPropertyName("tracking_location_id")]
    public long? TrackingLocationId { get; set; }

    [JsonPropertyName("total")]
    public decimal? Total { get; set; }

    [JsonPropertyName("total_in_base_currency")]
    public decimal? TotalInBaseCurrency { get; set; }

    [JsonPropertyName("purchase_order_rows")]
    public List<PurchaseOrderRowDto> PurchaseOrderRows { get; set; } = new();

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

public class PurchaseOrderRowDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }

    [JsonPropertyName("variant_id")]
    public long VariantId { get; set; }

    [JsonPropertyName("tax_rate_id")]
    public long? TaxRateId { get; set; }

    [JsonPropertyName("group_id")]
    public long? GroupId { get; set; }

    [JsonPropertyName("price_per_unit")]
    public decimal PricePerUnit { get; set; }

    [JsonPropertyName("purchase_uom")]
    public string? PurchaseUom { get; set; }

    [JsonPropertyName("purchase_uom_conversion_rate")]
    public decimal? PurchaseUomConversionRate { get; set; }

    [JsonPropertyName("batch_transactions")]
    public List<ManufacturingOrderBatchTransactionDto>? BatchTransactions { get; set; }

    [JsonPropertyName("total")]
    public decimal? Total { get; set; }

    [JsonPropertyName("total_in_base_currency")]
    public decimal? TotalInBaseCurrency { get; set; }

    [JsonPropertyName("conversion_rate")]
    public decimal? ConversionRate { get; set; }

    [JsonPropertyName("conversion_date")]
    public DateTime? ConversionDate { get; set; }

    [JsonPropertyName("received_date")]
    public DateTime? ReceivedDate { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("arrival_date")]
    public DateTime? ArrivalDate { get; set; }
}

public class PurchaseOrderCreateRequest
{
    [JsonPropertyName("order_no")]
    public string OrderNo { get; set; } = string.Empty;

    [JsonPropertyName("entity_type")]
    public string EntityType { get; set; } = "regular";

    [JsonPropertyName("supplier_id")]
    public long SupplierId { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = "NOT_RECEIVED";

    [JsonPropertyName("expected_arrival_date")]
    public DateTime? ExpectedArrivalDate { get; set; }

    [JsonPropertyName("order_created_date")]
    public DateTime? OrderCreatedDate { get; set; }

    [JsonPropertyName("location_id")]
    public long LocationId { get; set; }

    [JsonPropertyName("tracking_location_id")]
    public long? TrackingLocationId { get; set; }

    [JsonPropertyName("additional_info")]
    public string? AdditionalInfo { get; set; }

    [JsonPropertyName("purchase_order_rows")]
    public List<PurchaseOrderRowCreateDto> PurchaseOrderRows { get; set; } = new();
}

public class PurchaseOrderRowCreateDto
{
    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }

    [JsonPropertyName("variant_id")]
    public long VariantId { get; set; }

    [JsonPropertyName("tax_rate_id")]
    public long? TaxRateId { get; set; }

    [JsonPropertyName("group_id")]
    public long? GroupId { get; set; }

    [JsonPropertyName("price_per_unit")]
    public decimal PricePerUnit { get; set; }

    [JsonPropertyName("purchase_uom_conversion_rate")]
    public decimal? PurchaseUomConversionRate { get; set; }

    [JsonPropertyName("purchase_uom")]
    public string? PurchaseUom { get; set; }

    [JsonPropertyName("arrival_date")]
    public DateTime? ArrivalDate { get; set; }
}

public class PurchaseOrderUpdateRequest
{
    [JsonPropertyName("order_no")]
    public string? OrderNo { get; set; }

    [JsonPropertyName("supplier_id")]
    public long? SupplierId { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("tracking_location_id")]
    public long? TrackingLocationId { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("expected_arrival_date")]
    public DateTime? ExpectedArrivalDate { get; set; }

    [JsonPropertyName("order_created_date")]
    public DateTime? OrderCreatedDate { get; set; }

    [JsonPropertyName("location_id")]
    public long? LocationId { get; set; }

    [JsonPropertyName("additional_info")]
    public string? AdditionalInfo { get; set; }
}

public class PurchaseOrderListQuery
{
    [JsonPropertyName("ids")]
    public List<long>? Ids { get; set; }

    [JsonPropertyName("order_no")]
    public string? OrderNo { get; set; }

    [JsonPropertyName("entity_type")]
    public string? EntityType { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("billing_status")]
    public string? BillingStatus { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("location_id")]
    public long? LocationId { get; set; }

    [JsonPropertyName("tracking_location_id")]
    public long? TrackingLocationId { get; set; }

    [JsonPropertyName("supplier_id")]
    public long? SupplierId { get; set; }

    [JsonPropertyName("extend")]
    public List<string>? Extend { get; set; }

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

public class PurchaseOrderRetrieveQuery
{
    [JsonPropertyName("extend")]
    public List<string>? Extend { get; set; }
}

public class PurchaseOrderRowUpdateRequest
{
    [JsonPropertyName("quantity")]
    public decimal? Quantity { get; set; }

    [JsonPropertyName("variant_id")]
    public long? VariantId { get; set; }

    [JsonPropertyName("tax_rate_id")]
    public long? TaxRateId { get; set; }

    [JsonPropertyName("group_id")]
    public long? GroupId { get; set; }

    [JsonPropertyName("price_per_unit")]
    public decimal? PricePerUnit { get; set; }

    [JsonPropertyName("purchase_uom_conversion_rate")]
    public decimal? PurchaseUomConversionRate { get; set; }

    [JsonPropertyName("purchase_uom")]
    public string? PurchaseUom { get; set; }

    [JsonPropertyName("arrival_date")]
    public DateTime? ArrivalDate { get; set; }
}

public class PurchaseOrderRowListQuery
{
    [JsonPropertyName("ids")]
    public List<long>? Ids { get; set; }

    [JsonPropertyName("purchase_order_id")]
    public long? PurchaseOrderId { get; set; }

    [JsonPropertyName("variant_id")]
    public long? VariantId { get; set; }

    [JsonPropertyName("tax_rate_id")]
    public long? TaxRateId { get; set; }

    [JsonPropertyName("group_id")]
    public long? GroupId { get; set; }

    [JsonPropertyName("purchase_uom")]
    public string? PurchaseUom { get; set; }

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

/// <summary>
/// Purchase order receive isteği (basit tanım).
/// </summary>
public class PurchaseOrderReceiveRequest
{
    [JsonPropertyName("purchase_order_id")]
    public long PurchaseOrderId { get; set; }
}
