using System.Text.Json.Serialization;

namespace Katana.Core.DTOs;

/// <summary>
/// Lokasyon DTO'su.
/// </summary>
public class LocationDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("legal_name")]
    public string? LegalName { get; set; }

    [JsonPropertyName("address_id")]
    public long? AddressId { get; set; }

    [JsonPropertyName("address")]
    public LocationAddressDto? Address { get; set; }

    [JsonPropertyName("is_primary")]
    public bool? IsPrimary { get; set; }

    [JsonPropertyName("sales_allowed")]
    public bool? SalesAllowed { get; set; }

    [JsonPropertyName("manufacturing_allowed")]
    public bool? ManufacturingAllowed { get; set; }

    [JsonPropertyName("purchase_allowed")]
    public bool? PurchaseAllowed { get; set; }

    [JsonPropertyName("rank")]
    public int? Rank { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("deleted_at")]
    public DateTime? DeletedAt { get; set; }
}

/// <summary>
/// Lokasyon adres DTO'su.
/// </summary>
public class LocationAddressDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

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
/// Lokasyon listeleme filtreleri.
/// </summary>
public class LocationListQuery
{
    [JsonPropertyName("ids")]
    public List<long>? Ids { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("legal_name")]
    public string? LegalName { get; set; }

    [JsonPropertyName("address_id")]
    public long? AddressId { get; set; }

    [JsonPropertyName("sales_allowed")]
    public bool? SalesAllowed { get; set; }

    [JsonPropertyName("manufacturing_allowed")]
    public bool? ManufacturingAllowed { get; set; }

    [JsonPropertyName("purchases_allowed")]
    public bool? PurchasesAllowed { get; set; }

    [JsonPropertyName("rank")]
    public int? Rank { get; set; }

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
