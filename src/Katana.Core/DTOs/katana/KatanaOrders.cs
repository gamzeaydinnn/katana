using System.Text.Json.Serialization;

namespace Katana.Core.DTOs;

#region Sales Order




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
    public string EntityType { get; set; } = string.Empty; 

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




public class SalesOrderAddressCreateRequest
{
    [JsonPropertyName("sales_order_id")]
    public long SalesOrderId { get; set; }

    [JsonPropertyName("entity_type")]
    public string EntityType { get; set; } = "billing"; 

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
    public string Status { get; set; } = string.Empty; 

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

public class SalesOrderShippingFeeDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("sales_order_id")]
    public long SalesOrderId { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("tax_rate_id")]
    public long? TaxRateId { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}

public class SalesOrderShippingFeeCreateRequest
{
    [JsonPropertyName("sales_order_id")]
    public long SalesOrderId { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("tax_rate_id")]
    public long? TaxRateId { get; set; }
}

public class SalesOrderShippingFeeUpdateRequest
{
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }

    [JsonPropertyName("tax_rate_id")]
    public long? TaxRateId { get; set; }
}

public class SalesOrderShippingFeeListQuery
{
    [JsonPropertyName("ids")]
    public List<long>? Ids { get; set; }

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

#endregion

#region Purchase Order




public class PurchaseOrderDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty; 

    [JsonPropertyName("billing_status")]
    public string? BillingStatus { get; set; }

    [JsonPropertyName("last_document_status")]
    public string? LastDocumentStatus { get; set; }

    [JsonPropertyName("order_no")]
    public string OrderNo { get; set; } = string.Empty;

    [JsonPropertyName("entity_type")]
    public string EntityType { get; set; } = "regular"; 

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




public class PurchaseOrderReceiveRequest
{
    [JsonPropertyName("purchase_order_id")]
    public long PurchaseOrderId { get; set; }
}

public class PurchaseOrderAccountingMetadataDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("integration_type")]
    public string IntegrationType { get; set; } = string.Empty; 

    [JsonPropertyName("bill_id")]
    public string? BillId { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("purchase_order_id")]
    public long PurchaseOrderId { get; set; }

    [JsonPropertyName("received_items_group_id")]
    public long? ReceivedItemsGroupId { get; set; }
}

public class PurchaseOrderAccountingMetadataListQuery
{
    [JsonPropertyName("purchase_order_id")]
    public long? PurchaseOrderId { get; set; }

    [JsonPropertyName("received_items_group_id")]
    public long? ReceivedItemsGroupId { get; set; }

    [JsonPropertyName("limit")]
    public int? Limit { get; set; }

    [JsonPropertyName("page")]
    public int? Page { get; set; }
}

public class PurchaseOrderAdditionalCostRowDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("additional_cost_id")]
    public long AdditionalCostId { get; set; }

    [JsonPropertyName("group_id")]
    public long GroupId { get; set; }

    [JsonPropertyName("tax_rate_id")]
    public long TaxRateId { get; set; }

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("distribution_method")]
    public string DistributionMethod { get; set; } = string.Empty; 

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}

public class PurchaseOrderAdditionalCostRowCreateRequest
{
    [JsonPropertyName("additional_cost_id")]
    public long AdditionalCostId { get; set; }

    [JsonPropertyName("group_id")]
    public long GroupId { get; set; }

    [JsonPropertyName("tax_rate_id")]
    public long TaxRateId { get; set; }

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("distribution_method")]
    public string DistributionMethod { get; set; } = "BY_VALUE";
}

public class PurchaseOrderAdditionalCostRowUpdateRequest
{
    [JsonPropertyName("additional_cost_id")]
    public long? AdditionalCostId { get; set; }

    [JsonPropertyName("tax_rate_id")]
    public long? TaxRateId { get; set; }

    [JsonPropertyName("price")]
    public decimal? Price { get; set; }

    [JsonPropertyName("distribution_method")]
    public string? DistributionMethod { get; set; }
}

public class PurchaseOrderAdditionalCostRowListQuery
{
    [JsonPropertyName("ids")]
    public List<long>? Ids { get; set; }

    [JsonPropertyName("group_id")]
    public long? GroupId { get; set; }

    [JsonPropertyName("additional_cost_id")]
    public long? AdditionalCostId { get; set; }

    [JsonPropertyName("tax_rate_id")]
    public long? TaxRateId { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("distribution_method")]
    public string? DistributionMethod { get; set; }

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




public class OutsourcedPurchaseOrderRecipeRowDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("purchase_order_id")]
    public long PurchaseOrderId { get; set; }

    [JsonPropertyName("purchase_order_row_id")]
    public long PurchaseOrderRowId { get; set; }

    [JsonPropertyName("ingredient_variant_id")]
    public long IngredientVariantId { get; set; }

    [JsonPropertyName("planned_quantity_per_unit")]
    public decimal PlannedQuantityPerUnit { get; set; }

    [JsonPropertyName("ingredient_availability")]
    public string? IngredientAvailability { get; set; }

    [JsonPropertyName("ingredient_expected_date")]
    public DateTime? IngredientExpectedDate { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("batch_transactions")]
    public List<ManufacturingOrderBatchTransactionDto>? BatchTransactions { get; set; }

    [JsonPropertyName("cost")]
    public decimal? Cost { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("deleted_at")]
    public DateTime? DeletedAt { get; set; }
}

public class OutsourcedPurchaseOrderRecipeRowCreateRequest
{
    [JsonPropertyName("purchase_order_row_id")]
    public long PurchaseOrderRowId { get; set; }

    [JsonPropertyName("ingredient_variant_id")]
    public long IngredientVariantId { get; set; }

    [JsonPropertyName("planned_quantity_per_unit")]
    public decimal PlannedQuantityPerUnit { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("batch_transactions")]
    public List<ManufacturingOrderBatchTransactionDto>? BatchTransactions { get; set; }
}

public class OutsourcedPurchaseOrderRecipeRowUpdateRequest
{
    [JsonPropertyName("ingredient_variant_id")]
    public long? IngredientVariantId { get; set; }

    [JsonPropertyName("planned_quantity_per_unit")]
    public decimal? PlannedQuantityPerUnit { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("batch_transactions")]
    public List<ManufacturingOrderBatchTransactionDto>? BatchTransactions { get; set; }
}

public class OutsourcedPurchaseOrderRecipeRowListQuery
{
    [JsonPropertyName("ids")]
    public List<long>? Ids { get; set; }

    [JsonPropertyName("purchase_order_id")]
    public long? PurchaseOrderId { get; set; }

    [JsonPropertyName("purchase_order_row_id")]
    public long? PurchaseOrderRowId { get; set; }

    [JsonPropertyName("ingredient_variant_id")]
    public long? IngredientVariantId { get; set; }

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

#region Manufacturing Order




public class ManufacturingOrderDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty; 

    [JsonPropertyName("order_no")]
    public string OrderNo { get; set; } = string.Empty;

    [JsonPropertyName("variant_id")]
    public long VariantId { get; set; }

    [JsonPropertyName("planned_quantity")]
    public decimal PlannedQuantity { get; set; }

    [JsonPropertyName("actual_quantity")]
    public decimal ActualQuantity { get; set; }

    [JsonPropertyName("batch_transactions")]
    public List<ManufacturingOrderBatchTransactionDto> BatchTransactions { get; set; } = new();

    [JsonPropertyName("location_id")]
    public long LocationId { get; set; }

    [JsonPropertyName("order_created_date")]
    public DateTime? OrderCreatedDate { get; set; }

    [JsonPropertyName("done_date")]
    public DateTime? DoneDate { get; set; }

    [JsonPropertyName("production_deadline_date")]
    public DateTime? ProductionDeadlineDate { get; set; }

    [JsonPropertyName("additional_info")]
    public string? AdditionalInfo { get; set; }

    [JsonPropertyName("is_linked_to_sales_order")]
    public bool? IsLinkedToSalesOrder { get; set; }

    [JsonPropertyName("sales_order_id")]
    public long? SalesOrderId { get; set; }

    [JsonPropertyName("sales_order_row_id")]
    public long? SalesOrderRowId { get; set; }

    [JsonPropertyName("sales_order_delivery_deadline")]
    public DateTime? SalesOrderDeliveryDeadline { get; set; }

    [JsonPropertyName("ingredient_availability")]
    public string? IngredientAvailability { get; set; }

    [JsonPropertyName("total_cost")]
    public decimal? TotalCost { get; set; }

    [JsonPropertyName("total_planned_time")]
    public long? TotalPlannedTime { get; set; }

    [JsonPropertyName("total_actual_time")]
    public long? TotalActualTime { get; set; }

    [JsonPropertyName("material_cost")]
    public decimal? MaterialCost { get; set; }

    [JsonPropertyName("subassemblies_cost")]
    public decimal? SubassembliesCost { get; set; }

    [JsonPropertyName("operations_cost")]
    public decimal? OperationsCost { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("deleted_at")]
    public DateTime? DeletedAt { get; set; }
}

public class ManufacturingOrderBatchTransactionDto
{
    [JsonPropertyName("batch_id")]
    public long BatchId { get; set; }

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }
}

public class ManufacturingOrderCreateRequest
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "NOT_STARTED";

    [JsonPropertyName("order_no")]
    public string OrderNo { get; set; } = string.Empty;

    [JsonPropertyName("variant_id")]
    public long VariantId { get; set; }

    [JsonPropertyName("location_id")]
    public long LocationId { get; set; }

    [JsonPropertyName("planned_quantity")]
    public decimal PlannedQuantity { get; set; }

    [JsonPropertyName("actual_quantity")]
    public decimal? ActualQuantity { get; set; }

    [JsonPropertyName("order_created_date")]
    public DateTime? OrderCreatedDate { get; set; }

    [JsonPropertyName("production_deadline_date")]
    public DateTime? ProductionDeadlineDate { get; set; }

    [JsonPropertyName("additional_info")]
    public string? AdditionalInfo { get; set; }

    [JsonPropertyName("batch_transactions")]
    public List<ManufacturingOrderBatchTransactionDto>? BatchTransactions { get; set; }
}

public class ManufacturingOrderUpdateRequest
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("order_no")]
    public string? OrderNo { get; set; }

    [JsonPropertyName("variant_id")]
    public long? VariantId { get; set; }

    [JsonPropertyName("location_id")]
    public long? LocationId { get; set; }

    [JsonPropertyName("planned_quantity")]
    public decimal? PlannedQuantity { get; set; }

    [JsonPropertyName("actual_quantity")]
    public decimal? ActualQuantity { get; set; }

    [JsonPropertyName("order_created_date")]
    public DateTime? OrderCreatedDate { get; set; }

    [JsonPropertyName("production_deadline_date")]
    public DateTime? ProductionDeadlineDate { get; set; }

    [JsonPropertyName("additional_info")]
    public string? AdditionalInfo { get; set; }

    [JsonPropertyName("done_date")]
    public DateTime? DoneDate { get; set; }

    [JsonPropertyName("batch_transactions")]
    public List<ManufacturingOrderBatchTransactionDto>? BatchTransactions { get; set; }
}

public class ManufacturingOrderListQuery
{
    [JsonPropertyName("ids")]
    public List<long>? Ids { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("order_no")]
    public string? OrderNo { get; set; }

    [JsonPropertyName("location_id")]
    public long? LocationId { get; set; }

    [JsonPropertyName("is_linked_to_sales_order")]
    public bool? IsLinkedToSalesOrder { get; set; }

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




public class ManufacturingOrderMakeToOrderRequest
{
    [JsonPropertyName("sales_order_row_id")]
    public long SalesOrderRowId { get; set; }

    [JsonPropertyName("create_subassemblies")]
    public bool CreateSubassemblies { get; set; }
}




public class ManufacturingOrderUnlinkRequest
{
    [JsonPropertyName("sales_order_row_id")]
    public long SalesOrderRowId { get; set; }
}




public class ManufacturingOrderProductionDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("manufacturing_order_id")]
    public long ManufacturingOrderId { get; set; }

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }

    [JsonPropertyName("production_date")]
    public DateTime? ProductionDate { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    [JsonPropertyName("ingredients")]
    public List<ManufacturingOrderProductionIngredientDto>? Ingredients { get; set; }

    [JsonPropertyName("operations")]
    public List<ManufacturingOrderProductionOperationDto>? Operations { get; set; }
}

public class ManufacturingOrderProductionIngredientDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("location_id")]
    public long LocationId { get; set; }

    [JsonPropertyName("variant_id")]
    public long VariantId { get; set; }

    [JsonPropertyName("manufacturing_order_id")]
    public long ManufacturingOrderId { get; set; }

    [JsonPropertyName("manufacturing_order_recipe_row_id")]
    public long ManufacturingOrderRecipeRowId { get; set; }

    [JsonPropertyName("production_id")]
    public long ProductionId { get; set; }

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }

    [JsonPropertyName("production_date")]
    public DateTime? ProductionDate { get; set; }

    [JsonPropertyName("cost")]
    public decimal? Cost { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("deleted_at")]
    public DateTime? DeletedAt { get; set; }
}

public class ManufacturingOrderProductionOperationDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("location_id")]
    public long LocationId { get; set; }

    [JsonPropertyName("manufacturing_order_id")]
    public long ManufacturingOrderId { get; set; }

    [JsonPropertyName("manufacturing_order_operation_id")]
    public long ManufacturingOrderOperationId { get; set; }

    [JsonPropertyName("production_id")]
    public long ProductionId { get; set; }

    [JsonPropertyName("time")]
    public long Time { get; set; }

    [JsonPropertyName("production_date")]
    public DateTime? ProductionDate { get; set; }

    [JsonPropertyName("cost")]
    public decimal? Cost { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("deleted_at")]
    public DateTime? DeletedAt { get; set; }
}

public class ManufacturingOrderProductionListQuery
{
    [JsonPropertyName("ids")]
    public List<long>? Ids { get; set; }

    [JsonPropertyName("manufacturing_order_ids")]
    public List<long>? ManufacturingOrderIds { get; set; }

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

public class ManufacturingOrderProductionCreateRequest
{
    [JsonPropertyName("manufacturing_order_id")]
    public long ManufacturingOrderId { get; set; }

    [JsonPropertyName("completed_quantity")]
    public decimal CompletedQuantity { get; set; }

    [JsonPropertyName("completed_date")]
    public DateTime? CompletedDate { get; set; }

    [JsonPropertyName("is_final")]
    public bool? IsFinal { get; set; }

    [JsonPropertyName("ingredients")]
    public List<ManufacturingOrderProductionIngredientDto>? Ingredients { get; set; }

    [JsonPropertyName("operations")]
    public List<ManufacturingOrderProductionOperationDto>? Operations { get; set; }

    [JsonPropertyName("serial_numbers")]
    public List<long>? SerialNumbers { get; set; }
}

public class ManufacturingOrderProductionUpdateRequest
{
    [JsonPropertyName("production_date")]
    public DateTime? ProductionDate { get; set; }
}




public class ManufacturingOrderProductionIngredientUpdateRequest
{
    [JsonPropertyName("batch_transactions")]
    public List<ManufacturingOrderBatchTransactionDto>? BatchTransactions { get; set; }
}

public class ManufacturingOrderOperationRowDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty; 

    [JsonPropertyName("rank")]
    public int? Rank { get; set; }

    [JsonPropertyName("manufacturing_order_id")]
    public long ManufacturingOrderId { get; set; }

    [JsonPropertyName("operation_id")]
    public long? OperationId { get; set; }

    [JsonPropertyName("operation_name")]
    public string? OperationName { get; set; }

    [JsonPropertyName("resource_id")]
    public long? ResourceId { get; set; }

    [JsonPropertyName("resource_name")]
    public string? ResourceName { get; set; }

    [JsonPropertyName("assigned_operators")]
    public List<ManufacturingOrderOperatorDto>? AssignedOperators { get; set; }

    [JsonPropertyName("completed_by_operators")]
    public List<ManufacturingOrderOperatorDto>? CompletedByOperators { get; set; }

    [JsonPropertyName("active_operator_id")]
    public long? ActiveOperatorId { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; } 

    [JsonPropertyName("planned_time_parameter")]
    public long? PlannedTimeParameter { get; set; }

    [JsonPropertyName("total_actual_time")]
    public long? TotalActualTime { get; set; }

    [JsonPropertyName("planned_cost_per_unit")]
    public decimal? PlannedCostPerUnit { get; set; }

    [JsonPropertyName("total_actual_cost")]
    public decimal? TotalActualCost { get; set; }

    [JsonPropertyName("cost_parameter")]
    public decimal? CostParameter { get; set; }

    [JsonPropertyName("group_boundary")]
    public int? GroupBoundary { get; set; }

    [JsonPropertyName("is_status_actionable")]
    public bool? IsStatusActionable { get; set; }

    [JsonPropertyName("completed_at")]
    public DateTime? CompletedAt { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("deleted_at")]
    public DateTime? DeletedAt { get; set; }
}

public class ManufacturingOrderOperatorDto
{
    [JsonPropertyName("operator_id")]
    public long OperatorId { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("deleted_at")]
    public DateTime? DeletedAt { get; set; }
}

public class ManufacturingOrderOperationRowCreateRequest
{
    [JsonPropertyName("manufacturing_order_id")]
    public long ManufacturingOrderId { get; set; }

    [JsonPropertyName("operation_id")]
    public long? OperationId { get; set; }

    [JsonPropertyName("operation_name")]
    public string? OperationName { get; set; }

    [JsonPropertyName("resource_id")]
    public long? ResourceId { get; set; }

    [JsonPropertyName("resource_name")]
    public string? ResourceName { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; } 

    [JsonPropertyName("planned_time_parameter")]
    public long? PlannedTimeParameter { get; set; }

    [JsonPropertyName("cost_parameter")]
    public decimal? CostParameter { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "NOT_STARTED";

    [JsonPropertyName("assigned_operators")]
    public List<ManufacturingOrderOperatorDto>? AssignedOperators { get; set; }
}

public class ManufacturingOrderOperationRowListQuery
{
    [JsonPropertyName("ids")]
    public List<long>? Ids { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("manufacturing_order_id")]
    public long? ManufacturingOrderId { get; set; }

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




public class ManufacturingOrderOperationRowUpdateRequest
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("rank")]
    public int? Rank { get; set; }

    [JsonPropertyName("operation_id")]
    public long? OperationId { get; set; }

    [JsonPropertyName("operation_name")]
    public string? OperationName { get; set; }

    [JsonPropertyName("resource_id")]
    public long? ResourceId { get; set; }

    [JsonPropertyName("resource_name")]
    public string? ResourceName { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("planned_time_parameter")]
    public long? PlannedTimeParameter { get; set; }

    [JsonPropertyName("total_actual_time")]
    public long? TotalActualTime { get; set; }

    [JsonPropertyName("planned_cost_per_unit")]
    public decimal? PlannedCostPerUnit { get; set; }

    [JsonPropertyName("total_actual_cost")]
    public decimal? TotalActualCost { get; set; }

    [JsonPropertyName("cost_parameter")]
    public decimal? CostParameter { get; set; }

    [JsonPropertyName("group_boundary")]
    public int? GroupBoundary { get; set; }

    [JsonPropertyName("assigned_operators")]
    public List<ManufacturingOrderOperatorDto>? AssignedOperators { get; set; }

    [JsonPropertyName("completed_by_operators")]
    public List<ManufacturingOrderOperatorDto>? CompletedByOperators { get; set; }

    [JsonPropertyName("active_operator_id")]
    public long? ActiveOperatorId { get; set; }

    [JsonPropertyName("completed_at")]
    public DateTime? CompletedAt { get; set; }
}




public class ManufacturingOrderRecipeRowDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("manufacturing_order_id")]
    public long ManufacturingOrderId { get; set; }

    [JsonPropertyName("variant_id")]
    public long VariantId { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("planned_quantity_per_unit")]
    public decimal PlannedQuantityPerUnit { get; set; }

    [JsonPropertyName("total_actual_quantity")]
    public decimal? TotalActualQuantity { get; set; }

    [JsonPropertyName("ingredient_availability")]
    public string? IngredientAvailability { get; set; }

    [JsonPropertyName("ingredient_expected_date")]
    public DateTime? IngredientExpectedDate { get; set; }

    [JsonPropertyName("batch_transactions")]
    public List<ManufacturingOrderBatchTransactionDto>? BatchTransactions { get; set; }

    [JsonPropertyName("cost")]
    public decimal? Cost { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("deleted_at")]
    public DateTime? DeletedAt { get; set; }
}

public class ManufacturingOrderRecipeRowCreateRequest
{
    [JsonPropertyName("manufacturing_order_id")]
    public long ManufacturingOrderId { get; set; }

    [JsonPropertyName("variant_id")]
    public long VariantId { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("planned_quantity_per_unit")]
    public decimal PlannedQuantityPerUnit { get; set; }

    [JsonPropertyName("total_actual_quantity")]
    public decimal? TotalActualQuantity { get; set; }

    [JsonPropertyName("batch_transactions")]
    public List<ManufacturingOrderBatchTransactionDto>? BatchTransactions { get; set; }
}

public class ManufacturingOrderRecipeRowUpdateRequest
{
    [JsonPropertyName("variant_id")]
    public long? VariantId { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("planned_quantity_per_unit")]
    public decimal? PlannedQuantityPerUnit { get; set; }

    [JsonPropertyName("total_actual_quantity")]
    public decimal? TotalActualQuantity { get; set; }

    [JsonPropertyName("batch_transactions")]
    public List<ManufacturingOrderBatchTransactionDto>? BatchTransactions { get; set; }
}

public class ManufacturingOrderRecipeRowListQuery
{
    [JsonPropertyName("ids")]
    public List<long>? Ids { get; set; }

    [JsonPropertyName("manufacturing_order_id")]
    public long? ManufacturingOrderId { get; set; }

    [JsonPropertyName("variant_id")]
    public long? VariantId { get; set; }

    [JsonPropertyName("ingredient_availability")]
    public string? IngredientAvailability { get; set; }

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
