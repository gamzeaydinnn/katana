using System.Text.Json.Serialization;

namespace Katana.Core.DTOs;

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
    public string? AdjustmentMethod { get; set; } // fixed | percentage | markup

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
