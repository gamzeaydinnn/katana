namespace Katana.Core.DTOs;

/// <summary>
/// Varyantları ana ürün altında gruplandıran DTO
/// </summary>
public class ProductVariantGroup
{
    public long ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductSKU { get; set; }
    public int VariantCount { get; set; }
    public List<VariantDetail> Variants { get; set; } = new();
    public decimal TotalStock { get; set; }
    public decimal TotalAvailable { get; set; }
    public decimal TotalCommitted { get; set; }
    public bool HasOrphanVariants { get; set; }
}

/// <summary>
/// Varyant detay bilgileri
/// </summary>
public class VariantDetail
{
    public long VariantId { get; set; }
    public long? ProductId { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string? Name { get; set; }
    public List<VariantAttribute> Attributes { get; set; } = new();
    public decimal InStock { get; set; }
    public decimal Available { get; set; }
    public decimal Committed { get; set; }
    public decimal? SalesPrice { get; set; }
    public decimal? PurchasePrice { get; set; }
    public string? Unit { get; set; }
    public bool IsOrphan { get; set; }
    public DateTime? LastSyncedAt { get; set; }
}

/// <summary>
/// Varyant özelliği (renk, beden vb.)
/// </summary>
public class VariantAttribute
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Code { get; set; }
}

/// <summary>
/// Varyant duplicate grubu
/// </summary>
public class VariantDuplicateGroup
{
    public string GroupKey { get; set; } = string.Empty;
    public double SimilarityScore { get; set; }
    public VariantDetail? RecommendedCanonical { get; set; }
    public List<VariantDetail> Duplicates { get; set; } = new();
    public int TotalOrderLines { get; set; }
    public int TotalStockMovements { get; set; }
}

/// <summary>
/// Varyant birleştirme sonucu
/// </summary>
public class VariantMergeResult
{
    public bool Success { get; set; }
    public long CanonicalProductId { get; set; }
    public List<long> MergedProductIds { get; set; } = new();
    public int TransferredOrderLines { get; set; }
    public int TransferredStockMovements { get; set; }
    public int UpdatedLucaMappings { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// Aktif sipariş referansı
/// </summary>
public class ActiveOrderReference
{
    public int OrderId { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string? CustomerName { get; set; }
    public decimal Quantity { get; set; }
}
