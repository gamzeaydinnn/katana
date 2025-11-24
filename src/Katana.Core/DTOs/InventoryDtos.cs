using System.Text.Json.Serialization;

namespace Katana.Core.DTOs;

/// <summary>
/// Stok envanter kayıt DTO'su.
/// </summary>
public class InventoryDto
{
    [JsonPropertyName("variant_id")]
    public long VariantId { get; set; }

    [JsonPropertyName("location_id")]
    public long LocationId { get; set; }

    [JsonPropertyName("safety_stock_level")]
    public decimal? SafetyStockLevel { get; set; }

    [JsonPropertyName("reorder_point")]
    public decimal? ReorderPoint { get; set; }

    [JsonPropertyName("average_cost")]
    public decimal? AverageCost { get; set; }

    [JsonPropertyName("value_in_stock")]
    public decimal? ValueInStock { get; set; }

    [JsonPropertyName("quantity_in_stock")]
    public decimal? QuantityInStock { get; set; }

    [JsonPropertyName("quantity_committed")]
    public decimal? QuantityCommitted { get; set; }

    [JsonPropertyName("quantity_expected")]
    public decimal? QuantityExpected { get; set; }

    [JsonPropertyName("quantity_missing_or_excess")]
    public decimal? QuantityMissingOrExcess { get; set; }

    [JsonPropertyName("quantity_potential")]
    public decimal? QuantityPotential { get; set; }
}

/// <summary>
/// Envanter listeleme filtreleri.
/// </summary>
public class InventoryListQuery
{
    [JsonPropertyName("location_id")]
    public long? LocationId { get; set; }

    [JsonPropertyName("variant_id")]
    public List<long>? VariantIds { get; set; }

    [JsonPropertyName("include_archived")]
    public bool? IncludeArchived { get; set; }

    [JsonPropertyName("extend")]
    public List<string>? Extend { get; set; }

    [JsonPropertyName("limit")]
    public int? Limit { get; set; }

    [JsonPropertyName("page")]
    public int? Page { get; set; }
}

/// <summary>
/// Safety stock update isteği.
/// </summary>
public class InventorySafetyStockUpdateRequest
{
    [JsonPropertyName("location_id")]
    public long LocationId { get; set; }

    [JsonPropertyName("variant_id")]
    public long VariantId { get; set; }

    [JsonPropertyName("value")]
    public decimal Value { get; set; }
}

/// <summary>
/// Reorder point (deprecated) update isteği.
/// </summary>
public class InventoryReorderPointUpdateRequest
{
    [JsonPropertyName("location_id")]
    public long LocationId { get; set; }

    [JsonPropertyName("variant_id")]
    public long VariantId { get; set; }

    [JsonPropertyName("value")]
    public decimal Value { get; set; }
}

/// <summary>
/// Negatif stok kaydı DTO'su.
/// </summary>
public class NegativeStockDto
{
    [JsonPropertyName("location_id")]
    public long LocationId { get; set; }

    [JsonPropertyName("variant_id")]
    public long VariantId { get; set; }

    [JsonPropertyName("latest_negative_stock_date")]
    public DateTime? LatestNegativeStockDate { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("sku")]
    public string? Sku { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }
}

/// <summary>
/// Negatif stok listeleme filtreleri.
/// </summary>
public class NegativeStockListQuery
{
    [JsonPropertyName("location_id")]
    public long? LocationId { get; set; }

    [JsonPropertyName("variant_id")]
    public long? VariantId { get; set; }

    [JsonPropertyName("latest_negative_stock_date_max")]
    public DateTime? LatestNegativeStockDateMax { get; set; }

    [JsonPropertyName("latest_negative_stock_date_min")]
    public DateTime? LatestNegativeStockDateMin { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("sku")]
    public string? Sku { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("limit")]
    public int? Limit { get; set; }

    [JsonPropertyName("page")]
    public int? Page { get; set; }
}
