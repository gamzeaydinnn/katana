using System.Text.Json.Serialization;

namespace Katana.Core.DTOs;

public class SalesOrderDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("customer_id")]
    public long CustomerId { get; set; }

    [JsonPropertyName("order_no")]
    public string OrderNo { get; set; } = string.Empty;

    [JsonPropertyName("source")]
    public string? Source { get; set; }

    [JsonPropertyName("order_created_date")]
    public DateTime? OrderCreatedDate { get; set; }

    [JsonPropertyName("delivery_date")]
    public DateTime? DeliveryDate { get; set; }

    [JsonPropertyName("picked_date")]
    public DateTime? PickedDate { get; set; }

    [JsonPropertyName("location_id")]
    public long? LocationId { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "NOT_SHIPPED";

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("conversion_rate")]
    public decimal? ConversionRate { get; set; }

    [JsonPropertyName("conversion_date")]
    public DateTime? ConversionDate { get; set; }

    [JsonPropertyName("invoicing_status")]
    public string? InvoicingStatus { get; set; }

    [JsonPropertyName("total")]
    public decimal? Total { get; set; }

    [JsonPropertyName("total_in_base_currency")]
    public decimal? TotalInBaseCurrency { get; set; }

    [JsonPropertyName("additional_info")]
    public string? AdditionalInfo { get; set; }

    [JsonPropertyName("customer_ref")]
    public string? CustomerRef { get; set; }

    [JsonPropertyName("sales_order_rows")]
    public List<SalesOrderRowDto> SalesOrderRows { get; set; } = new();

    [JsonPropertyName("ecommerce_order_type")]
    public string? EcommerceOrderType { get; set; }

    [JsonPropertyName("ecommerce_store_name")]
    public string? EcommerceStoreName { get; set; }

    [JsonPropertyName("ecommerce_order_id")]
    public string? EcommerceOrderId { get; set; }

    [JsonPropertyName("product_availability")]
    public string? ProductAvailability { get; set; }

    [JsonPropertyName("product_expected_date")]
    public DateTime? ProductExpectedDate { get; set; }

    [JsonPropertyName("ingredient_availability")]
    public string? IngredientAvailability { get; set; }

    [JsonPropertyName("ingredient_expected_date")]
    public DateTime? IngredientExpectedDate { get; set; }

    [JsonPropertyName("production_status")]
    public string? ProductionStatus { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("billing_address_id")]
    public long? BillingAddressId { get; set; }

    [JsonPropertyName("shipping_address_id")]
    public long? ShippingAddressId { get; set; }

    [JsonPropertyName("addresses")]
    public List<SalesOrderAddressDto>? Addresses { get; set; }
}

public class SalesOrderRowAttributeDto
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}

public class SalesOrderRowDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }

    [JsonPropertyName("variant_id")]
    public long VariantId { get; set; }

    [JsonPropertyName("tax_rate_id")]
    public long? TaxRateId { get; set; }

    [JsonPropertyName("location_id")]
    public long? LocationId { get; set; }

    [JsonPropertyName("product_availability")]
    public string? ProductAvailability { get; set; }

    [JsonPropertyName("product_expected_date")]
    public DateTime? ProductExpectedDate { get; set; }

    [JsonPropertyName("price_per_unit")]
    public decimal? PricePerUnit { get; set; }

    [JsonPropertyName("price_per_unit_in_base_currency")]
    public decimal? PricePerUnitInBaseCurrency { get; set; }

    [JsonPropertyName("total")]
    public decimal? Total { get; set; }

    [JsonPropertyName("total_in_base_currency")]
    public decimal? TotalInBaseCurrency { get; set; }

    [JsonPropertyName("cogs_value")]
    public decimal? CogsValue { get; set; }

    [JsonPropertyName("attributes")]
    public List<SalesOrderRowAttributeDto>? Attributes { get; set; }

    [JsonPropertyName("batch_transactions")]
    public List<ManufacturingOrderBatchTransactionDto>? BatchTransactions { get; set; }

    [JsonPropertyName("serial_numbers")]
    public List<long>? SerialNumbers { get; set; }

    [JsonPropertyName("serial_number_transactions")]
    public List<SalesOrderSerialNumberTransactionDto>? SerialNumberTransactions { get; set; }

    [JsonPropertyName("linked_manufacturing_order_id")]
    public long? LinkedManufacturingOrderId { get; set; }

    [JsonPropertyName("conversion_rate")]
    public decimal? ConversionRate { get; set; }

    [JsonPropertyName("conversion_date")]
    public DateTime? ConversionDate { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

public class SalesOrderAddressDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("sales_order_id")]
    public long SalesOrderId { get; set; }

    [JsonPropertyName("entity_type")]
    public string EntityType { get; set; } = string.Empty; // billing | shipping

    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }

    [JsonPropertyName("company")]
    public string? Company { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("line_1")]
    public string? Line1 { get; set; }

    [JsonPropertyName("line_2")]
    public string? Line2 { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("zip")]
    public string? Zip { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Satış siparişi adresi oluşturma isteği.
/// </summary>
public class SalesOrderAddressCreateRequest
{
    [JsonPropertyName("sales_order_id")]
    public long SalesOrderId { get; set; }

    [JsonPropertyName("entity_type")]
    public string EntityType { get; set; } = "billing"; // billing | shipping

    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }

    [JsonPropertyName("company")]
    public string? Company { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("line_1")]
    public string? Line1 { get; set; }

    [JsonPropertyName("line_2")]
    public string? Line2 { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("zip")]
    public string? Zip { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }
}

/// <summary>
/// Satış siparişi adresi güncelleme isteği.
/// </summary>
public class SalesOrderAddressUpdateRequest
{
    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }

    [JsonPropertyName("company")]
    public string? Company { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("line_1")]
    public string? Line1 { get; set; }

    [JsonPropertyName("line_2")]
    public string? Line2 { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("zip")]
    public string? Zip { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }
}

/// <summary>
/// Satış siparişi adresi listeleme filtreleri.
/// </summary>
public class SalesOrderAddressListQuery
{
    [JsonPropertyName("ids")]
    public List<long>? Ids { get; set; }

    [JsonPropertyName("sales_order_ids")]
    public List<long>? SalesOrderIds { get; set; }

    [JsonPropertyName("entity_type")]
    public string? EntityType { get; set; }

    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }

    [JsonPropertyName("company")]
    public string? Company { get; set; }

    [JsonPropertyName("line_1")]
    public string? Line1 { get; set; }

    [JsonPropertyName("line_2")]
    public string? Line2 { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("zip")]
    public string? Zip { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

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

public class SalesOrderReturnableItemDto
{
    [JsonPropertyName("variant_id")]
    public long VariantId { get; set; }

    [JsonPropertyName("fulfillment_row_id")]
    public long FulfillmentRowId { get; set; }

    [JsonPropertyName("available_for_return_quantity")]
    public decimal AvailableForReturnQuantity { get; set; }

    [JsonPropertyName("net_price_per_unit")]
    public decimal NetPricePerUnit { get; set; }

    [JsonPropertyName("location_id")]
    public long LocationId { get; set; }

    [JsonPropertyName("quantity_sold")]
    public decimal QuantitySold { get; set; }
}

public class SalesOrderCreateRequest
{
    [JsonPropertyName("order_no")]
    public string OrderNo { get; set; } = string.Empty;

    [JsonPropertyName("customer_id")]
    public long CustomerId { get; set; }

    [JsonPropertyName("sales_order_rows")]
    public List<SalesOrderRowCreateDto> SalesOrderRows { get; set; } = new();

    [JsonPropertyName("addresses")]
    public List<SalesOrderAddressDto>? Addresses { get; set; }

    [JsonPropertyName("order_created_date")]
    public DateTime? OrderCreatedDate { get; set; }

    [JsonPropertyName("delivery_date")]
    public DateTime? DeliveryDate { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("location_id")]
    public long? LocationId { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("additional_info")]
    public string? AdditionalInfo { get; set; }

    [JsonPropertyName("customer_ref")]
    public string? CustomerRef { get; set; }

    [JsonPropertyName("ecommerce_order_type")]
    public string? EcommerceOrderType { get; set; }

    [JsonPropertyName("ecommerce_store_name")]
    public string? EcommerceStoreName { get; set; }

    [JsonPropertyName("ecommerce_order_id")]
    public string? EcommerceOrderId { get; set; }
}

public class SalesOrderRowCreateDto
{
    [JsonPropertyName("sales_order_id")]
    public long? SalesOrderId { get; set; }

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }

    [JsonPropertyName("variant_id")]
    public long VariantId { get; set; }

    [JsonPropertyName("tax_rate_id")]
    public long? TaxRateId { get; set; }

    [JsonPropertyName("location_id")]
    public long? LocationId { get; set; }

    [JsonPropertyName("attributes")]
    public List<SalesOrderRowAttributeDto>? Attributes { get; set; }

    [JsonPropertyName("price_per_unit")]
    public decimal? PricePerUnit { get; set; }

    [JsonPropertyName("total_discount")]
    public decimal? TotalDiscount { get; set; }

    [JsonPropertyName("tracking_number")]
    public string? TrackingNumber { get; set; }

    [JsonPropertyName("tracking_number_url")]
    public string? TrackingNumberUrl { get; set; }
}

public class SalesOrderRowUpdateRequest
{
    [JsonPropertyName("quantity")]
    public decimal? Quantity { get; set; }

    [JsonPropertyName("variant_id")]
    public long? VariantId { get; set; }

    [JsonPropertyName("tax_rate_id")]
    public long? TaxRateId { get; set; }

    [JsonPropertyName("location_id")]
    public long? LocationId { get; set; }

    [JsonPropertyName("price_per_unit")]
    public decimal? PricePerUnit { get; set; }

    [JsonPropertyName("total_discount")]
    public decimal? TotalDiscount { get; set; }

    [JsonPropertyName("batch_transactions")]
    public List<ManufacturingOrderBatchTransactionDto>? BatchTransactions { get; set; }

    [JsonPropertyName("serial_number_transactions")]
    public List<SalesOrderSerialNumberTransactionDto>? SerialNumberTransactions { get; set; }

    [JsonPropertyName("attributes")]
    public List<SalesOrderRowAttributeDto>? Attributes { get; set; }
}

public class SalesOrderRowListQuery
{
    [JsonPropertyName("ids")]
    public List<long>? Ids { get; set; }

    [JsonPropertyName("sales_order_ids")]
    public List<long>? SalesOrderIds { get; set; }

    [JsonPropertyName("variant_id")]
    public long? VariantId { get; set; }

    [JsonPropertyName("location_id")]
    public long? LocationId { get; set; }

    [JsonPropertyName("tax_rate_id")]
    public long? TaxRateId { get; set; }

    [JsonPropertyName("linked_manufacturing_order_id")]
    public long? LinkedManufacturingOrderId { get; set; }

    [JsonPropertyName("product_availability")]
    public string? ProductAvailability { get; set; }

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

public class SalesOrderSerialNumberTransactionDto
{
    [JsonPropertyName("serial_number_id")]
    public long SerialNumberId { get; set; }

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }
}

public class SalesOrderUpdateRequest
{
    [JsonPropertyName("order_no")]
    public string? OrderNo { get; set; }

    [JsonPropertyName("customer_id")]
    public long? CustomerId { get; set; }

    [JsonPropertyName("order_created_date")]
    public DateTime? OrderCreatedDate { get; set; }

    [JsonPropertyName("delivery_date")]
    public DateTime? DeliveryDate { get; set; }

    [JsonPropertyName("picked_date")]
    public DateTime? PickedDate { get; set; }

    [JsonPropertyName("location_id")]
    public long? LocationId { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("conversion_rate")]
    public decimal? ConversionRate { get; set; }

    [JsonPropertyName("conversion_date")]
    public DateTime? ConversionDate { get; set; }

    [JsonPropertyName("additional_info")]
    public string? AdditionalInfo { get; set; }

    [JsonPropertyName("customer_ref")]
    public string? CustomerRef { get; set; }

    [JsonPropertyName("tracking_number")]
    public string? TrackingNumber { get; set; }

    [JsonPropertyName("tracking_number_url")]
    public string? TrackingNumberUrl { get; set; }

    [JsonPropertyName("attributes")]
    public List<SalesOrderRowAttributeDto>? Attributes { get; set; }

    [JsonPropertyName("batch_transactions")]
    public List<ManufacturingOrderBatchTransactionDto>? BatchTransactions { get; set; }

    [JsonPropertyName("serial_number_transactions")]
    public List<SalesOrderSerialNumberTransactionDto>? SerialNumberTransactions { get; set; }
}

public class SalesOrderListQuery
{
    [JsonPropertyName("ids")]
    public List<long>? Ids { get; set; }

    [JsonPropertyName("order_no")]
    public string? OrderNo { get; set; }

    [JsonPropertyName("source")]
    public string? Source { get; set; }

    [JsonPropertyName("location_id")]
    public long? LocationId { get; set; }

    [JsonPropertyName("customer_id")]
    public long? CustomerId { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("invoicing_status")]
    public string? InvoicingStatus { get; set; }

    [JsonPropertyName("product_availability")]
    public string? ProductAvailability { get; set; }

    [JsonPropertyName("ingredient_availability")]
    public string? IngredientAvailability { get; set; }

    [JsonPropertyName("production_status")]
    public string? ProductionStatus { get; set; }

    [JsonPropertyName("ecommerce_order_type")]
    public string? EcommerceOrderType { get; set; }

    [JsonPropertyName("ecommerce_store_name")]
    public string? EcommerceStoreName { get; set; }

    [JsonPropertyName("ecommerce_order_id")]
    public string? EcommerceOrderId { get; set; }

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

public class SalesOrderRetrieveQuery
{
    [JsonPropertyName("extend")]
    public List<string>? Extend { get; set; }
}

public class SalesOrderFulfillmentDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("sales_order_id")]
    public long SalesOrderId { get; set; }

    [JsonPropertyName("picked_date")]
    public DateTime? PickedDate { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty; // PACKED, DELIVERED

    [JsonPropertyName("conversion_rate")]
    public decimal? ConversionRate { get; set; }

    [JsonPropertyName("conversion_date")]
    public DateTime? ConversionDate { get; set; }

    [JsonPropertyName("tracking_number")]
    public string? TrackingNumber { get; set; }

    [JsonPropertyName("tracking_url")]
    public string? TrackingUrl { get; set; }

    [JsonPropertyName("tracking_carrier")]
    public string? TrackingCarrier { get; set; }

    [JsonPropertyName("tracking_method")]
    public string? TrackingMethod { get; set; }

    [JsonPropertyName("sales_order_fulfillment_rows")]
    public List<SalesOrderFulfillmentRowDto> SalesOrderFulfillmentRows { get; set; } = new();

    [JsonPropertyName("packer_id")]
    public long? PackerId { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

public class SalesOrderFulfillmentRowDto
{
    [JsonPropertyName("sales_order_row_id")]
    public long SalesOrderRowId { get; set; }

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }

    [JsonPropertyName("batch_transactions")]
    public List<ManufacturingOrderBatchTransactionDto>? BatchTransactions { get; set; }

    [JsonPropertyName("serial_numbers")]
    public List<long>? SerialNumbers { get; set; }
}

public class SalesOrderFulfillmentCreateRequest
{
    [JsonPropertyName("sales_order_id")]
    public long SalesOrderId { get; set; }

    [JsonPropertyName("picked_date")]
    public DateTime? PickedDate { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "DELIVERED";

    [JsonPropertyName("conversion_rate")]
    public decimal? ConversionRate { get; set; }

    [JsonPropertyName("conversion_date")]
    public DateTime? ConversionDate { get; set; }

    [JsonPropertyName("tracking_number")]
    public string? TrackingNumber { get; set; }

    [JsonPropertyName("tracking_url")]
    public string? TrackingUrl { get; set; }

    [JsonPropertyName("tracking_carrier")]
    public string? TrackingCarrier { get; set; }

    [JsonPropertyName("tracking_method")]
    public string? TrackingMethod { get; set; }

    [JsonPropertyName("sales_order_fulfillment_rows")]
    public List<SalesOrderFulfillmentRowDto> SalesOrderFulfillmentRows { get; set; } = new();
}

public class SalesOrderFulfillmentUpdateRequest
{
    [JsonPropertyName("picked_date")]
    public DateTime? PickedDate { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("conversion_rate")]
    public decimal? ConversionRate { get; set; }

    [JsonPropertyName("conversion_date")]
    public DateTime? ConversionDate { get; set; }

    [JsonPropertyName("packer_id")]
    public long? PackerId { get; set; }

    [JsonPropertyName("tracking_number")]
    public string? TrackingNumber { get; set; }

    [JsonPropertyName("tracking_url")]
    public string? TrackingUrl { get; set; }

    [JsonPropertyName("tracking_carrier")]
    public string? TrackingCarrier { get; set; }

    [JsonPropertyName("tracking_method")]
    public string? TrackingMethod { get; set; }
}

public class SalesOrderFulfillmentListQuery
{
    [JsonPropertyName("sales_order_id")]
    public long? SalesOrderId { get; set; }

    [JsonPropertyName("picked_date_min")]
    public DateTime? PickedDateMin { get; set; }

    [JsonPropertyName("tracking_number")]
    public string? TrackingNumber { get; set; }

    [JsonPropertyName("tracking_url")]
    public string? TrackingUrl { get; set; }

    [JsonPropertyName("tracking_carrier")]
    public string? TrackingCarrier { get; set; }

    [JsonPropertyName("tracking_method")]
    public string? TrackingMethod { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

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
