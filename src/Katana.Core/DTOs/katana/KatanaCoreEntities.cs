using System.Text.Json.Serialization;

namespace Katana.Core.DTOs;

#region Product & Variant

public class ProductDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("sku")]
    public string SKU { get; set; } = string.Empty;
    
    [JsonIgnore]
    public string Sku
    {
        get => SKU;
        set => SKU = value;
    }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("uom")]
    public string Uom { get; set; } = string.Empty;

    [JsonPropertyName("category_name")]
    public string? CategoryName { get; set; }

    [JsonPropertyName("is_producible")]
    public bool? IsProducible { get; set; }

    [JsonPropertyName("default_supplier_id")]
    public long? DefaultSupplierId { get; set; }

    [JsonPropertyName("is_purchasable")]
    public bool? IsPurchasable { get; set; }

    [JsonPropertyName("is_auto_assembly")]
    public bool? IsAutoAssembly { get; set; }

    [JsonPropertyName("is_sellable")]
    public bool? IsSellable { get; set; }

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

    [JsonPropertyName("serial_tracked")]
    public bool? SerialTracked { get; set; }

    [JsonPropertyName("operations_in_sequence")]
    public bool? OperationsInSequence { get; set; }

    [JsonPropertyName("variants")]
    public List<ProductVariantDto> Variants { get; set; } = new();

    [JsonPropertyName("configs")]
    public List<ProductConfigDto>? Configs { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [JsonPropertyName("archived_at")]
    public DateTime? ArchivedAt { get; set; }

    [JsonPropertyName("custom_field_collection_id")]
    public long? CustomFieldCollectionId { get; set; }

    
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public int CategoryId { get; set; }
    public string? MainImageUrl { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class ProductConfigDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("values")]
    public List<string> Values { get; set; } = new();
}

public class ProductVariantDto
{
    [JsonPropertyName("sku")]
    public string Sku { get; set; } = string.Empty;

    [JsonPropertyName("purchase_price")]
    public decimal? PurchasePrice { get; set; }

    [JsonPropertyName("sales_price")]
    public decimal? SalesPrice { get; set; }

    [JsonPropertyName("config_attributes")]
    public List<ProductVariantConfigAttributeDto>? ConfigAttributes { get; set; }

    [JsonPropertyName("internal_barcode")]
    public string? InternalBarcode { get; set; }

    [JsonPropertyName("registered_barcode")]
    public string? RegisteredBarcode { get; set; }

    [JsonPropertyName("supplier_item_codes")]
    public List<string>? SupplierItemCodes { get; set; }

    [JsonPropertyName("custom_fields")]
    public List<MaterialVariantCustomFieldDto>? CustomFields { get; set; }
}

public class ProductSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public bool IsActive { get; set; }
}

public class CreateProductDto
{
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public int CategoryId { get; set; }
    public string? MainImageUrl { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateProductDto
{
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public int CategoryId { get; set; }
    public string? MainImageUrl { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class ProductStatisticsDto
{
    public int TotalProducts { get; set; }
    public int ActiveProducts { get; set; }
    public int InactiveProducts { get; set; }
    public int LowStockProducts { get; set; }
    public int OutOfStockProducts { get; set; }
    public decimal TotalInventoryValue { get; set; }
}

public class ProductVariantConfigAttributeDto
{
    [JsonPropertyName("config_name")]
    public string ConfigName { get; set; } = string.Empty;

    [JsonPropertyName("config_value")]
    public string ConfigValue { get; set; } = string.Empty;
}

public class ProductCreateRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("uom")]
    public string Uom { get; set; } = string.Empty;

    [JsonPropertyName("category_name")]
    public string? CategoryName { get; set; }

    [JsonPropertyName("is_sellable")]
    public bool? IsSellable { get; set; }

    [JsonPropertyName("is_producible")]
    public bool? IsProducible { get; set; }

    [JsonPropertyName("is_purchasable")]
    public bool? IsPurchasable { get; set; }

    [JsonPropertyName("is_auto_assembly")]
    public bool? IsAutoAssembly { get; set; }

    [JsonPropertyName("default_supplier_id")]
    public long? DefaultSupplierId { get; set; }

    [JsonPropertyName("additional_info")]
    public string? AdditionalInfo { get; set; }

    [JsonPropertyName("batch_tracked")]
    public bool? BatchTracked { get; set; }

    [JsonPropertyName("serial_tracked")]
    public bool? SerialTracked { get; set; }

    [JsonPropertyName("operations_in_sequence")]
    public bool? OperationsInSequence { get; set; }

    [JsonPropertyName("purchase_uom")]
    public string? PurchaseUom { get; set; }

    [JsonPropertyName("purchase_uom_conversion_rate")]
    public decimal? PurchaseUomConversionRate { get; set; }

    [JsonPropertyName("lead_time")]
    public int? LeadTime { get; set; }

    [JsonPropertyName("minimum_order_quantity")]
    public decimal? MinimumOrderQuantity { get; set; }

    [JsonPropertyName("configs")]
    public List<ProductConfigDto>? Configs { get; set; }

    [JsonPropertyName("custom_field_collection_id")]
    public long? CustomFieldCollectionId { get; set; }

    [JsonPropertyName("variants")]
    public List<ProductVariantDto> Variants { get; set; } = new();
}

public class ProductUpdateRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("uom")]
    public string? Uom { get; set; }

    [JsonPropertyName("category_name")]
    public string? CategoryName { get; set; }

    [JsonPropertyName("is_sellable")]
    public bool? IsSellable { get; set; }

    [JsonPropertyName("is_producible")]
    public bool? IsProducible { get; set; }

    [JsonPropertyName("is_purchasable")]
    public bool? IsPurchasable { get; set; }

    [JsonPropertyName("is_auto_assembly")]
    public bool? IsAutoAssembly { get; set; }

    [JsonPropertyName("is_archived")]
    public bool? IsArchived { get; set; }

    [JsonPropertyName("default_supplier_id")]
    public long? DefaultSupplierId { get; set; }

    [JsonPropertyName("additional_info")]
    public string? AdditionalInfo { get; set; }

    [JsonPropertyName("batch_tracked")]
    public bool? BatchTracked { get; set; }

    [JsonPropertyName("serial_tracked")]
    public bool? SerialTracked { get; set; }

    [JsonPropertyName("operations_in_sequence")]
    public bool? OperationsInSequence { get; set; }

    [JsonPropertyName("purchase_uom")]
    public string? PurchaseUom { get; set; }

    [JsonPropertyName("purchase_uom_conversion_rate")]
    public decimal? PurchaseUomConversionRate { get; set; }

    [JsonPropertyName("configs")]
    public List<ProductConfigDto>? Configs { get; set; }

    [JsonPropertyName("custom_field_collection_id")]
    public long? CustomFieldCollectionId { get; set; }
}

public class ProductListQuery
{
    [JsonPropertyName("ids")]
    public List<long>? Ids { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("uom")]
    public string? Uom { get; set; }

    [JsonPropertyName("is_sellable")]
    public bool? IsSellable { get; set; }

    [JsonPropertyName("is_producible")]
    public bool? IsProducible { get; set; }

    [JsonPropertyName("is_purchasable")]
    public bool? IsPurchasable { get; set; }

    [JsonPropertyName("is_auto_assembly")]
    public bool? IsAutoAssembly { get; set; }

    [JsonPropertyName("default_supplier_id")]
    public long? DefaultSupplierId { get; set; }

    [JsonPropertyName("batch_tracked")]
    public bool? BatchTracked { get; set; }

    [JsonPropertyName("serial_tracked")]
    public bool? SerialTracked { get; set; }

    [JsonPropertyName("operations_in_sequence")]
    public bool? OperationsInSequence { get; set; }

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

public class ProductRetrieveQuery
{
    [JsonPropertyName("extend")]
    public List<string>? Extend { get; set; }
}




public class ProductOperationRowDto
{
    [JsonPropertyName("product_operation_row_id")]
    public long ProductOperationRowId { get; set; }

    [JsonPropertyName("product_id")]
    public long ProductId { get; set; }

    [JsonPropertyName("product_variant_id")]
    public long ProductVariantId { get; set; }

    [JsonPropertyName("operation_id")]
    public long? OperationId { get; set; }

    [JsonPropertyName("operation_name")]
    public string? OperationName { get; set; }

    [JsonPropertyName("resource_id")]
    public long? ResourceId { get; set; }

    [JsonPropertyName("resource_name")]
    public string? ResourceName { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; } 

    [JsonPropertyName("cost_per_hour")]
    public decimal? CostPerHour { get; set; }

    [JsonPropertyName("cost_parameter")]
    public decimal? CostParameter { get; set; }

    [JsonPropertyName("planned_time_per_unit")]
    public long? PlannedTimePerUnit { get; set; }

    [JsonPropertyName("planned_time_parameter")]
    public long? PlannedTimeParameter { get; set; }

    [JsonPropertyName("planned_cost_per_unit")]
    public decimal? PlannedCostPerUnit { get; set; }

    [JsonPropertyName("rank")]
    public int? Rank { get; set; }

    [JsonPropertyName("group_boundary")]
    public int? GroupBoundary { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}

public class ProductOperationRowCreateRequest
{
    [JsonPropertyName("keep_current_rows")]
    public bool? KeepCurrentRows { get; set; } = true;

    [JsonPropertyName("rows")]
    public List<ProductOperationRowCreateRow> Rows { get; set; } = new();
}

public class ProductOperationRowCreateRow
{
    [JsonPropertyName("product_variant_id")]
    public long ProductVariantId { get; set; }

    [JsonPropertyName("operation_id")]
    public long? OperationId { get; set; }

    [JsonPropertyName("operation_name")]
    public string? OperationName { get; set; }

    [JsonPropertyName("resource_id")]
    public long? ResourceId { get; set; }

    [JsonPropertyName("resource_name")]
    public string? ResourceName { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; } = "process";

    [JsonPropertyName("cost_parameter")]
    public decimal? CostParameter { get; set; }

    [JsonPropertyName("cost_per_hour")]
    public decimal? CostPerHour { get; set; }

    [JsonPropertyName("planned_time_parameter")]
    public long? PlannedTimeParameter { get; set; }

    [JsonPropertyName("planned_time_per_unit")]
    public long? PlannedTimePerUnit { get; set; }
}

public class ProductOperationRowUpdateRequest
{
    [JsonPropertyName("operation_id")]
    public long? OperationId { get; set; }

    [JsonPropertyName("operation_name")]
    public string? OperationName { get; set; }

    [JsonPropertyName("resource_id")]
    public long? ResourceId { get; set; }

    [JsonPropertyName("resource_name")]
    public string? ResourceName { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("cost_parameter")]
    public decimal? CostParameter { get; set; }

    [JsonPropertyName("planned_time_parameter")]
    public long? PlannedTimeParameter { get; set; }

    [JsonPropertyName("cost_per_hour")]
    public decimal? CostPerHour { get; set; }

    [JsonPropertyName("planned_time_per_unit")]
    public long? PlannedTimePerUnit { get; set; }
}

public class ProductOperationRowListQuery
{
    [JsonPropertyName("product_id")]
    public long? ProductId { get; set; }

    [JsonPropertyName("product_operation_row_id")]
    public long? ProductOperationRowId { get; set; }

    [JsonPropertyName("product_variant_id")]
    public long? ProductVariantId { get; set; }

    [JsonPropertyName("operation_id")]
    public long? OperationId { get; set; }

    [JsonPropertyName("operation_name")]
    public string? OperationName { get; set; }

    [JsonPropertyName("resource_id")]
    public long? ResourceId { get; set; }

    [JsonPropertyName("resource_name")]
    public string? ResourceName { get; set; }

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

public class ProductOperationRerankRequest
{
    [JsonPropertyName("rank_product_operation_id")]
    public long RankProductOperationId { get; set; }

    [JsonPropertyName("preceding_product_operation_id")]
    public long? PrecedingProductOperationId { get; set; }

    [JsonPropertyName("should_group")]
    public bool? ShouldGroup { get; set; }
}




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
    public string? Type { get; set; } 

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

#endregion

#region Location







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







public class BinLocationDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("bin_name")]
    public string BinName { get; set; } = string.Empty;

    [JsonPropertyName("location_id")]
    public long LocationId { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}




public class BinLocationListQuery
{
    [JsonPropertyName("location_id")]
    public string? LocationId { get; set; }

    [JsonPropertyName("bin_name")]
    public string? BinName { get; set; }

    [JsonPropertyName("include_deleted")]
    public bool? IncludeDeleted { get; set; }

    [JsonPropertyName("limit")]
    public int? Limit { get; set; }

    [JsonPropertyName("page")]
    public int? Page { get; set; }
}




public class BinLocationUpdateRequest
{
    [JsonPropertyName("bin_name")]
    public string? BinName { get; set; }

    [JsonPropertyName("location_id")]
    public long? LocationId { get; set; }
}

#endregion
