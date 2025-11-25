using System.Text.Json.Serialization;

namespace Katana.Core.DTOs;

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
    public string? Type { get; set; } // service

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
