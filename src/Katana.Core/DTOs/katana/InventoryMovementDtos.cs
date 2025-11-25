using System.Text.Json.Serialization;

namespace Katana.Core.DTOs;

/// <summary>
/// Envanter hareketi DTO'su.
/// </summary>
public class InventoryMovementDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("variant_id")]
    public long VariantId { get; set; }

    [JsonPropertyName("location_id")]
    public long LocationId { get; set; }

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }

    [JsonPropertyName("resource_type")]
    public string? ResourceType { get; set; }

    [JsonPropertyName("resource_id")]
    public long? ResourceId { get; set; }

    [JsonPropertyName("caused_by_order_no")]
    public string? CausedByOrderNo { get; set; }

    [JsonPropertyName("caused_by_resource_id")]
    public long? CausedByResourceId { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Envanter hareketi listeleme filtreleri.
/// </summary>
public class InventoryMovementListQuery
{
    [JsonPropertyName("ids")]
    public List<long>? Ids { get; set; }

    [JsonPropertyName("variant_ids")]
    public List<long>? VariantIds { get; set; }

    [JsonPropertyName("location_id")]
    public long? LocationId { get; set; }

    [JsonPropertyName("resource_type")]
    public string? ResourceType { get; set; }

    [JsonPropertyName("resource_id")]
    public long? ResourceId { get; set; }

    [JsonPropertyName("caused_by_order_no")]
    public string? CausedByOrderNo { get; set; }

    [JsonPropertyName("caused_by_resource_id")]
    public long? CausedByResourceId { get; set; }

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
