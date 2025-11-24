using System.Text.Json.Serialization;

namespace Katana.Core.DTOs;

/// <summary>
/// BOM satırı (listeleme/okuma için).
/// </summary>
public class BomRowDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("product_variant_id")]
    public long ProductVariantId { get; set; }

    [JsonPropertyName("product_item_id")]
    public long ProductItemId { get; set; }

    [JsonPropertyName("ingredient_variant_id")]
    public long IngredientVariantId { get; set; }

    [JsonPropertyName("quantity")]
    public decimal? Quantity { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Tekli BOM satırı oluşturma isteği.
/// </summary>
public class BomRowCreateRequest
{
    [JsonPropertyName("product_item_id")]
    public long ProductItemId { get; set; }

    [JsonPropertyName("product_variant_id")]
    public long ProductVariantId { get; set; }

    [JsonPropertyName("ingredient_variant_id")]
    public long IngredientVariantId { get; set; }

    [JsonPropertyName("quantity")]
    public decimal? Quantity { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}

/// <summary>
/// Çoklu BOM satırı oluşturma isteği (batch create).
/// </summary>
public class BomRowBatchCreateRequest
{
    [JsonPropertyName("data")]
    public List<BomRowCreateRequest> Data { get; set; } = new();
}

/// <summary>
/// BOM satırı güncelleme isteği (partial).
/// </summary>
public class BomRowUpdateRequest
{
    [JsonPropertyName("ingredient_variant_id")]
    public long? IngredientVariantId { get; set; }

    [JsonPropertyName("quantity")]
    public decimal? Quantity { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}

/// <summary>
/// BOM satırı listeleme filtreleri.
/// </summary>
public class BomRowListQuery
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("product_item_id")]
    public long? ProductItemId { get; set; }

    [JsonPropertyName("product_variant_id")]
    public long? ProductVariantId { get; set; }

    [JsonPropertyName("ingredient_variant_id")]
    public long? IngredientVariantId { get; set; }

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
