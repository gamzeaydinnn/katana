using System.Text.Json.Serialization;

namespace Katana.Core.DTOs;

/// <summary>
/// Outsourced purchase order recipe row DTO.
/// </summary>
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
