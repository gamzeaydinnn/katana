using System.Text.Json.Serialization;

namespace Katana.Core.DTOs;

public class VariantDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("product_id")]
    public long? ProductId { get; set; }

    [JsonPropertyName("material_id")]
    public long? MaterialId { get; set; }

    [JsonPropertyName("sku")]
    public string Sku { get; set; } = string.Empty;

    [JsonPropertyName("sales_price")]
    public decimal? SalesPrice { get; set; }

    [JsonPropertyName("purchase_price")]
    public decimal? PurchasePrice { get; set; }

    [JsonPropertyName("internal_barcode")]
    public string? InternalBarcode { get; set; }

    [JsonPropertyName("registered_barcode")]
    public string? RegisteredBarcode { get; set; }

    [JsonPropertyName("supplier_item_codes")]
    public List<string>? SupplierItemCodes { get; set; }

    [JsonPropertyName("config_attributes")]
    public List<VariantConfigAttributeDto>? ConfigAttributes { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; } // product | material

    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [JsonPropertyName("minimum_order_quantity")]
    public decimal? MinimumOrderQuantity { get; set; }

    [JsonPropertyName("lead_time")]
    public int? LeadTime { get; set; }

    [JsonPropertyName("custom_fields")]
    public List<MaterialVariantCustomFieldDto>? CustomFields { get; set; }
}

public class VariantConfigAttributeDto
{
    [JsonPropertyName("config_name")]
    public string ConfigName { get; set; } = string.Empty;

    [JsonPropertyName("config_value")]
    public string ConfigValue { get; set; } = string.Empty;
}

public class VariantCreateRequest
{
    [JsonPropertyName("sku")]
    public string Sku { get; set; } = string.Empty;

    [JsonPropertyName("sales_price")]
    public decimal? SalesPrice { get; set; }

    [JsonPropertyName("purchase_price")]
    public decimal? PurchasePrice { get; set; }

    [JsonPropertyName("product_id")]
    public long? ProductId { get; set; }

    [JsonPropertyName("material_id")]
    public long? MaterialId { get; set; }

    [JsonPropertyName("supplier_item_codes")]
    public List<string>? SupplierItemCodes { get; set; }

    [JsonPropertyName("internal_barcode")]
    public string? InternalBarcode { get; set; }

    [JsonPropertyName("registered_barcode")]
    public string? RegisteredBarcode { get; set; }

    [JsonPropertyName("lead_time")]
    public int? LeadTime { get; set; }

    [JsonPropertyName("minimum_order_quantity")]
    public decimal? MinimumOrderQuantity { get; set; }

    [JsonPropertyName("config_attributes")]
    public List<VariantConfigAttributeDto>? ConfigAttributes { get; set; }

    [JsonPropertyName("custom_fields")]
    public List<MaterialVariantCustomFieldDto>? CustomFields { get; set; }
}

public class VariantUpdateRequest
{
    [JsonPropertyName("sku")]
    public string? Sku { get; set; }

    [JsonPropertyName("sales_price")]
    public decimal? SalesPrice { get; set; }

    [JsonPropertyName("purchase_price")]
    public decimal? PurchasePrice { get; set; }

    [JsonPropertyName("product_id")]
    public long? ProductId { get; set; }

    [JsonPropertyName("material_id")]
    public long? MaterialId { get; set; }

    [JsonPropertyName("supplier_item_codes")]
    public List<string>? SupplierItemCodes { get; set; }

    [JsonPropertyName("internal_barcode")]
    public string? InternalBarcode { get; set; }

    [JsonPropertyName("registered_barcode")]
    public string? RegisteredBarcode { get; set; }

    [JsonPropertyName("lead_time")]
    public int? LeadTime { get; set; }

    [JsonPropertyName("minimum_order_quantity")]
    public decimal? MinimumOrderQuantity { get; set; }

    [JsonPropertyName("config_attributes")]
    public List<VariantConfigAttributeDto>? ConfigAttributes { get; set; }

    [JsonPropertyName("custom_fields")]
    public List<MaterialVariantCustomFieldDto>? CustomFields { get; set; }
}

public class VariantListQuery
{
    [JsonPropertyName("ids")]
    public List<long>? Ids { get; set; }

    [JsonPropertyName("product_id")]
    public long? ProductId { get; set; }

    [JsonPropertyName("material_id")]
    public long? MaterialId { get; set; }

    [JsonPropertyName("sku")]
    public List<string>? Sku { get; set; }

    [JsonPropertyName("sales_price")]
    public decimal? SalesPrice { get; set; }

    [JsonPropertyName("purchase_price")]
    public decimal? PurchasePrice { get; set; }

    [JsonPropertyName("internal_barcode")]
    public string? InternalBarcode { get; set; }

    [JsonPropertyName("registered_barcode")]
    public string? RegisteredBarcode { get; set; }

    [JsonPropertyName("supplier_item_codes")]
    public List<string>? SupplierItemCodes { get; set; }

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

public class VariantRetrieveQuery
{
    [JsonPropertyName("extend")]
    public List<string>? Extend { get; set; }
}

public class VariantBinLocationLinkDto
{
    [JsonPropertyName("location_id")]
    public long LocationId { get; set; }

    [JsonPropertyName("variant_id")]
    public long VariantId { get; set; }

    [JsonPropertyName("bin_name")]
    public string BinName { get; set; } = string.Empty;
}

public class VariantBinLocationUnlinkDto
{
    [JsonPropertyName("location_id")]
    public long LocationId { get; set; }

    [JsonPropertyName("variant_id")]
    public long VariantId { get; set; }
}
