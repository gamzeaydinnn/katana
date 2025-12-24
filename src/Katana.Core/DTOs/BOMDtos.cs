namespace Katana.Core.DTOs;

/// <summary>
/// BOM gereksinim hesaplama sonucu
/// </summary>
public class BOMRequirementResult
{
    public int SalesOrderId { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public List<BOMLineRequirement> LineRequirements { get; set; } = new();
    public List<MaterialRequirement> TotalMaterialRequirements { get; set; } = new();
    public bool HasShortages { get; set; }
    public List<StockShortage> Shortages { get; set; } = new();
    public decimal TotalEstimatedCost { get; set; }
}

/// <summary>
/// Sipariş satırı için BOM gereksinimleri
/// </summary>
public class BOMLineRequirement
{
    public int OrderLineId { get; set; }
    public long VariantId { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal OrderedQuantity { get; set; }
    public bool HasBOM { get; set; }
    public List<MaterialRequirement> Materials { get; set; } = new();
    public decimal EstimatedLineCost { get; set; }
}

/// <summary>
/// Malzeme gereksinimi
/// </summary>
public class MaterialRequirement
{
    public long ComponentVariantId { get; set; }
    public string ComponentSKU { get; set; } = string.Empty;
    public string ComponentName { get; set; } = string.Empty;
    public decimal RequiredQuantity { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal AvailableStock { get; set; }
    public decimal ShortageQuantity { get; set; }
    public string Unit { get; set; } = "ADET";
    public decimal? UnitCost { get; set; }
    public decimal? TotalCost { get; set; }
    public decimal BOMRatio { get; set; } = 1;
}

/// <summary>
/// Stok eksikliği
/// </summary>
public class StockShortage
{
    public long VariantId { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Required { get; set; }
    public decimal Available { get; set; }
    public decimal Shortage { get; set; }
    public bool SuggestPurchaseOrder { get; set; }
    public decimal? EstimatedPurchaseCost { get; set; }
    public string? PreferredSupplier { get; set; }
    public int? LeadTimeDays { get; set; }
}

/// <summary>
/// BOM bileşeni
/// </summary>
public class BOMComponent
{
    public long ComponentId { get; set; }
    public long ParentVariantId { get; set; }
    public long ComponentVariantId { get; set; }
    public string ComponentSKU { get; set; } = string.Empty;
    public string ComponentName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "ADET";
    public decimal? UnitCost { get; set; }
    public bool IsActive { get; set; } = true;
    public int? SortOrder { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Malzeme tüketimi (üretim için)
/// </summary>
public class MaterialConsumption
{
    public long ComponentVariantId { get; set; }
    public string ComponentSKU { get; set; } = string.Empty;
    public decimal PlannedQuantity { get; set; }
    public decimal ActualQuantity { get; set; }
    public decimal ScrapQuantity { get; set; }
    public string Unit { get; set; } = "ADET";
    public decimal? UnitCost { get; set; }
    public string? LotNumber { get; set; }
}

/// <summary>
/// Üretim emri senkronizasyon sonucu
/// </summary>
public class ManufacturingOrderSyncResult
{
    public bool Success { get; set; }
    public long ManufacturingOrderId { get; set; }
    public string? LucaDocumentId { get; set; }
    public string? LucaDocumentNo { get; set; }
    public decimal ProducedQuantity { get; set; }
    public decimal ScrapQuantity { get; set; }
    public List<MaterialConsumption> ConsumedMaterials { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public int RetryCount { get; set; }
    public DateTime? LastRetryAt { get; set; }
}
