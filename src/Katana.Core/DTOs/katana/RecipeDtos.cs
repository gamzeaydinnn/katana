using System.Text.Json.Serialization;

namespace Katana.Core.DTOs;

/// <summary>
/// Deprecated recipe row DTO (use BOM rows instead).
/// </summary>
public class RecipeRowDto
{
    [JsonPropertyName("recipe_row_id")]
    public string RecipeRowId { get; set; } = string.Empty;

    [JsonPropertyName("product_id")]
    public long ProductId { get; set; }

    [JsonPropertyName("product_variant_id")]
    public long ProductVariantId { get; set; }

    [JsonPropertyName("ingredient_variant_id")]
    public long IngredientVariantId { get; set; }

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

public class RecipeCreateRequest
{
    [JsonPropertyName("keep_current_rows")]
    public bool? KeepCurrentRows { get; set; } = true;

    [JsonPropertyName("rows")]
    public List<RecipeCreateRow> Rows { get; set; } = new();
}

public class RecipeCreateRow
{
    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }

    [JsonPropertyName("ingredient_variant_id")]
    public long IngredientVariantId { get; set; }

    [JsonPropertyName("product_variant_id")]
    public long ProductVariantId { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}

public class RecipeRowUpdateRequest
{
    [JsonPropertyName("quantity")]
    public decimal? Quantity { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("ingredient_variant_id")]
    public long? IngredientVariantId { get; set; }
}

public class RecipeListQuery
{
    [JsonPropertyName("product_variant_ids")]
    public List<long>? ProductVariantIds { get; set; }

    [JsonPropertyName("recipe_row_id")]
    public string? RecipeRowId { get; set; }

    [JsonPropertyName("product_id")]
    public long? ProductId { get; set; }

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
