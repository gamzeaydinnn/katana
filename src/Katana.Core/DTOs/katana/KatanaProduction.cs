using System.Text.Json.Serialization;

namespace Katana.Core.DTOs;

#region BOM

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

public class BomRowBatchCreateRequest
{
    [JsonPropertyName("data")]
    public List<BomRowCreateRequest> Data { get; set; } = new();
}

public class BomRowUpdateRequest
{
    [JsonPropertyName("ingredient_variant_id")]
    public long? IngredientVariantId { get; set; }

    [JsonPropertyName("quantity")]
    public decimal? Quantity { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}

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

#endregion

#region Material




public class MaterialDto
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

    [JsonPropertyName("default_supplier_id")]
    public long? DefaultSupplierId { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("additional_info")]
    public string? AdditionalInfo { get; set; }

    [JsonPropertyName("purchase_uom")]
    public string? PurchaseUom { get; set; }

    [JsonPropertyName("purchase_uom_conversion_rate")]
    public decimal? PurchaseUomConversionRate { get; set; }

    [JsonPropertyName("batch_tracked")]
    public bool? BatchTracked { get; set; }

    [JsonPropertyName("variants")]
    public List<MaterialVariantDto> Variants { get; set; } = new();

    [JsonPropertyName("configs")]
    public List<MaterialConfigDto>? Configs { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("archived_at")]
    public DateTime? ArchivedAt { get; set; }

    [JsonPropertyName("custom_field_collection_id")]
    public long? CustomFieldCollectionId { get; set; }
}

public class MaterialConfigDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("values")]
    public List<string> Values { get; set; } = new();
}

public class MaterialVariantDto
{
    [JsonPropertyName("sku")]
    public string Sku { get; set; } = string.Empty;

    [JsonPropertyName("purchase_price")]
    public decimal? PurchasePrice { get; set; }

    [JsonPropertyName("internal_barcode")]
    public string? InternalBarcode { get; set; }

    [JsonPropertyName("registered_barcode")]
    public string? RegisteredBarcode { get; set; }

    [JsonPropertyName("supplier_item_codes")]
    public List<string>? SupplierItemCodes { get; set; }

    [JsonPropertyName("lead_time")]
    public decimal? LeadTime { get; set; }

    [JsonPropertyName("minimum_order_quantity")]
    public decimal? MinimumOrderQuantity { get; set; }

    [JsonPropertyName("config_attributes")]
    public List<MaterialVariantConfigAttributeDto>? ConfigAttributes { get; set; }

    [JsonPropertyName("custom_fields")]
    public List<MaterialVariantCustomFieldDto>? CustomFields { get; set; }
}

public class MaterialVariantConfigAttributeDto
{
    [JsonPropertyName("config_name")]
    public string ConfigName { get; set; } = string.Empty;

    [JsonPropertyName("config_value")]
    public string ConfigValue { get; set; } = string.Empty;
}

public class MaterialVariantCustomFieldDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}

public class MaterialCreateRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("uom")]
    public string Uom { get; set; } = string.Empty;

    [JsonPropertyName("category_name")]
    public string? CategoryName { get; set; }

    [JsonPropertyName("default_supplier_id")]
    public long? DefaultSupplierId { get; set; }

    [JsonPropertyName("additional_info")]
    public string? AdditionalInfo { get; set; }

    [JsonPropertyName("batch_tracked")]
    public bool? BatchTracked { get; set; }

    [JsonPropertyName("is_sellable")]
    public bool? IsSellable { get; set; }

    [JsonPropertyName("purchase_uom")]
    public string? PurchaseUom { get; set; }

    [JsonPropertyName("purchase_uom_conversion_rate")]
    public decimal? PurchaseUomConversionRate { get; set; }

    [JsonPropertyName("configs")]
    public List<MaterialConfigDto>? Configs { get; set; }

    [JsonPropertyName("custom_field_collection_id")]
    public long? CustomFieldCollectionId { get; set; }

    [JsonPropertyName("variants")]
    public List<MaterialVariantDto> Variants { get; set; } = new();
}

public class MaterialListQuery
{
    [JsonPropertyName("ids")]
    public List<long>? Ids { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("uom")]
    public string? Uom { get; set; }

    [JsonPropertyName("default_supplier_id")]
    public long? DefaultSupplierId { get; set; }

    [JsonPropertyName("is_sellable")]
    public bool? IsSellable { get; set; }

    [JsonPropertyName("batch_tracked")]
    public bool? BatchTracked { get; set; }

    [JsonPropertyName("purchase_uom")]
    public string? PurchaseUom { get; set; }

    [JsonPropertyName("purchase_uom_conversion_rate")]
    public decimal? PurchaseUomConversionRate { get; set; }

    [JsonPropertyName("extend")]
    public List<string>? Extend { get; set; }

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

public class MaterialRetrieveQuery
{
    [JsonPropertyName("extend")]
    public List<string>? Extend { get; set; }
}

public class MaterialUpdateRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("uom")]
    public string? Uom { get; set; }

    [JsonPropertyName("category_name")]
    public string? CategoryName { get; set; }

    [JsonPropertyName("default_supplier_id")]
    public long? DefaultSupplierId { get; set; }

    [JsonPropertyName("additional_info")]
    public string? AdditionalInfo { get; set; }

    [JsonPropertyName("batch_tracked")]
    public bool? BatchTracked { get; set; }

    [JsonPropertyName("is_sellable")]
    public bool? IsSellable { get; set; }

    [JsonPropertyName("is_archived")]
    public bool? IsArchived { get; set; }

    [JsonPropertyName("purchase_uom")]
    public string? PurchaseUom { get; set; }

    [JsonPropertyName("purchase_uom_conversion_rate")]
    public decimal? PurchaseUomConversionRate { get; set; }

    [JsonPropertyName("configs")]
    public List<MaterialConfigDto>? Configs { get; set; }

    [JsonPropertyName("custom_field_collection_id")]
    public long? CustomFieldCollectionId { get; set; }
}

#endregion

#region Recipe







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

#endregion

#region Operator




public class OperatorDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("working_area")]
    public string? WorkingArea { get; set; } 

    [JsonPropertyName("resource_id")]
    public long? ResourceId { get; set; }
}

public class OperatorListQuery
{
    [JsonPropertyName("working_area")]
    public string? WorkingArea { get; set; }

    [JsonPropertyName("resource_id")]
    public long? ResourceId { get; set; }

    [JsonPropertyName("limit")]
    public int? Limit { get; set; }

    [JsonPropertyName("page")]
    public int? Page { get; set; }
}

#endregion
