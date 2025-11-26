using System.Text.Json.Serialization;

namespace Katana.Core.DTOs;

#region Service




public class ServiceDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("uom")]
    public string Uom { get; set; } = string.Empty;

    [JsonPropertyName("category_name")]
    public string? CategoryName { get; set; }

    [JsonPropertyName("is_sellable")]
    public bool? IsSellable { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; } 

    [JsonPropertyName("additional_info")]
    public string? AdditionalInfo { get; set; }

    [JsonPropertyName("variants")]
    public List<ServiceVariantDto> Variants { get; set; } = new();

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    [JsonPropertyName("archived_at")]
    public DateTime? ArchivedAt { get; set; }
}

public class ServiceVariantDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("sku")]
    public string Sku { get; set; } = string.Empty;

    [JsonPropertyName("sales_price")]
    public decimal? SalesPrice { get; set; }

    [JsonPropertyName("default_cost")]
    public decimal? DefaultCost { get; set; }
}

public class ServiceListQuery
{
    [JsonPropertyName("ids")]
    public List<long>? Ids { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("uom")]
    public string? Uom { get; set; }

    [JsonPropertyName("is_sellable")]
    public bool? IsSellable { get; set; }

    [JsonPropertyName("category_name")]
    public string? CategoryName { get; set; }

    [JsonPropertyName("include_deleted")]
    public bool? IncludeDeleted { get; set; }

    [JsonPropertyName("include_archived")]
    public bool? IncludeArchived { get; set; }

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

public class ServiceCreateRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("uom")]
    public string Uom { get; set; } = string.Empty;

    [JsonPropertyName("category_name")]
    public string? CategoryName { get; set; }

    [JsonPropertyName("additional_info")]
    public string? AdditionalInfo { get; set; }

    [JsonPropertyName("is_sellable")]
    public bool? IsSellable { get; set; }

    [JsonPropertyName("custom_field_collection_id")]
    public long? CustomFieldCollectionId { get; set; }

    [JsonPropertyName("variants")]
    public List<ServiceVariantDto> Variants { get; set; } = new();
}

public class ServiceUpdateRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("uom")]
    public string? Uom { get; set; }

    [JsonPropertyName("category_name")]
    public string? CategoryName { get; set; }

    [JsonPropertyName("additional_info")]
    public string? AdditionalInfo { get; set; }

    [JsonPropertyName("is_sellable")]
    public bool? IsSellable { get; set; }

    [JsonPropertyName("is_archived")]
    public bool? IsArchived { get; set; }

    [JsonPropertyName("sales_price")]
    public decimal? SalesPrice { get; set; }

    [JsonPropertyName("default_cost")]
    public decimal? DefaultCost { get; set; }

    [JsonPropertyName("sku")]
    public string? Sku { get; set; }

    [JsonPropertyName("custom_field_collection_id")]
    public long? CustomFieldCollectionId { get; set; }
}

#endregion

#region Tax & Pricing




public class TaxRateDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("rate")]
    public decimal Rate { get; set; }

    [JsonPropertyName("is_default_sales")]
    public bool? IsDefaultSales { get; set; }

    [JsonPropertyName("is_default_purchases")]
    public bool? IsDefaultPurchases { get; set; }

    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}

public class TaxRateCreateRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("rate")]
    public decimal Rate { get; set; }
}

public class TaxRateListQuery
{
    [JsonPropertyName("rate")]
    public decimal? Rate { get; set; }

    [JsonPropertyName("ids")]
    public List<long>? Ids { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("is_default_sales")]
    public bool? IsDefaultSales { get; set; }

    [JsonPropertyName("is_default_purchases")]
    public bool? IsDefaultPurchases { get; set; }

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
public class PriceListDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("is_active")]
    public bool? IsActive { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}

public class PriceListCreateRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class PriceListUpdateRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("is_active")]
    public bool? IsActive { get; set; }
}

public class PriceListListQuery
{
    [JsonPropertyName("ids")]
    public List<long>? Ids { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("is_active")]
    public bool? IsActive { get; set; }

    [JsonPropertyName("limit")]
    public int? Limit { get; set; }

    [JsonPropertyName("page")]
    public int? Page { get; set; }
}

public class PriceListRowDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("price_list_id")]
    public long PriceListId { get; set; }

    [JsonPropertyName("variant_id")]
    public long VariantId { get; set; }

    [JsonPropertyName("adjustment_method")]
    public string? AdjustmentMethod { get; set; } 

    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }
}

public class PriceListRowCreateRequest
{
    [JsonPropertyName("price_list_id")]
    public long PriceListId { get; set; }

    [JsonPropertyName("price_list_rows")]
    public List<PriceListRowDto> PriceListRows { get; set; } = new();
}

public class PriceListRowUpdateRequest
{
    [JsonPropertyName("variant_id")]
    public long? VariantId { get; set; }

    [JsonPropertyName("adjustment_method")]
    public string? AdjustmentMethod { get; set; }

    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }
}

public class PriceListRowListQuery
{
    [JsonPropertyName("ids")]
    public List<long>? Ids { get; set; }

    [JsonPropertyName("variant_ids")]
    public List<long>? VariantIds { get; set; }

    [JsonPropertyName("price_list_ids")]
    public List<long>? PriceListIds { get; set; }

    [JsonPropertyName("limit")]
    public int? Limit { get; set; }

    [JsonPropertyName("page")]
    public int? Page { get; set; }
}

public class PriceListCustomerDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("price_list_id")]
    public long PriceListId { get; set; }

    [JsonPropertyName("customer_id")]
    public long CustomerId { get; set; }
}

public class PriceListCustomerCreateRequest
{
    [JsonPropertyName("price_list_id")]
    public long PriceListId { get; set; }

    [JsonPropertyName("price_list_customers")]
    public List<PriceListCustomerDto> PriceListCustomers { get; set; } = new();
}

public class PriceListCustomerUpdateRequest
{
    [JsonPropertyName("customer_id")]
    public long? CustomerId { get; set; }
}

public class PriceListCustomerListQuery
{
    [JsonPropertyName("ids")]
    public List<long>? Ids { get; set; }

    [JsonPropertyName("customer_ids")]
    public List<long>? CustomerIds { get; set; }

    [JsonPropertyName("price_list_ids")]
    public List<long>? PriceListIds { get; set; }

    [JsonPropertyName("limit")]
    public int? Limit { get; set; }

    [JsonPropertyName("page")]
    public int? Page { get; set; }
}

#endregion

#region Misc




public class CustomFieldsCollectionDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
public class WebhookDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("token")]
    public string? Token { get; set; }

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("subscribed_events")]
    public List<string> SubscribedEvents { get; set; } = new();

    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}

public class WebhookCreateRequest
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("subscribed_events")]
    public List<string> SubscribedEvents { get; set; } = new();

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

public class WebhookUpdateRequest
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("enabled")]
    public bool? Enabled { get; set; }

    [JsonPropertyName("subscribed_events")]
    public List<string>? SubscribedEvents { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

public class WebhookListQuery
{
    [JsonPropertyName("ids")]
    public List<long>? Ids { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("enabled")]
    public bool? Enabled { get; set; }

    [JsonPropertyName("limit")]
    public int? Limit { get; set; }

    [JsonPropertyName("page")]
    public int? Page { get; set; }
}

public class WebhookLogsExportRequest
{
    [JsonPropertyName("webhook_id")]
    public long? WebhookId { get; set; }

    [JsonPropertyName("event")]
    public string? Event { get; set; }

    [JsonPropertyName("status_code")]
    public int? StatusCode { get; set; }

    [JsonPropertyName("delivered")]
    public bool? Delivered { get; set; }

    [JsonPropertyName("created_at_min")]
    public DateTime? CreatedAtMin { get; set; }

    [JsonPropertyName("created_at_max")]
    public DateTime? CreatedAtMax { get; set; }
}



public class FactoryDto
{
    [JsonPropertyName("id")]
    public long? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}

#endregion

#region Serial Number




public class SerialNumberDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("transaction_id")]
    public long TransactionId { get; set; }

    [JsonPropertyName("serial_number")]
    public string SerialNumber { get; set; } = string.Empty;

    [JsonPropertyName("resource_type")]
    public string ResourceType { get; set; } = string.Empty;

    [JsonPropertyName("resource_id")]
    public long ResourceId { get; set; }

    [JsonPropertyName("transaction_date")]
    public DateTime TransactionDate { get; set; }

    [JsonPropertyName("quantity_change")]
    public decimal QuantityChange { get; set; }
}

public class SerialNumberListQuery
{
    [JsonPropertyName("resource_type")]
    public string ResourceType { get; set; } = string.Empty;

    [JsonPropertyName("resource_id")]
    public long ResourceId { get; set; }

    [JsonPropertyName("limit")]
    public int? Limit { get; set; }

    [JsonPropertyName("page")]
    public int? Page { get; set; }
}

public class SerialNumberAssignRequest
{
    [JsonPropertyName("resource_type")]
    public string ResourceType { get; set; } = string.Empty;

    [JsonPropertyName("resource_id")]
    public long ResourceId { get; set; }

    [JsonPropertyName("serial_numbers")]
    public List<string> SerialNumbers { get; set; } = new();
}

public class SerialNumberUnassignRequest
{
    [JsonPropertyName("resource_type")]
    public string ResourceType { get; set; } = string.Empty;

    [JsonPropertyName("resource_id")]
    public long ResourceId { get; set; }

    [JsonPropertyName("ids")]
    public List<string> Ids { get; set; } = new();
}

public class SerialNumberStockDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("serial_number")]
    public string SerialNumber { get; set; } = string.Empty;

    [JsonPropertyName("in_stock")]
    public bool InStock { get; set; }

    [JsonPropertyName("transactions")]
    public List<SerialNumberDto>? Transactions { get; set; }
}

#endregion

#region User




public class UserDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}

public class UserListQuery
{
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

public class CreateUserDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Email { get; set; }
}

public class UpdateUserDto
{
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
}

#endregion
