using System;

namespace Katana.Core.DTOs;

/// <summary>
/// Katana'dan veya Luca'dan alınan stok hareketi verisini temsil eder.
/// </summary>
public class StockMovementDto
{
    public int Id { get; set; }

    /// <summary>
    /// Ürünün benzersiz SKU (stok kodu)
    /// </summary>
    public string SKU { get; set; } = string.Empty;

    /// <summary>
    /// Ürünün adı
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Hareket tipi (IN, OUT, TRANSFER, RETURN)
    /// </summary>
    public string MovementType { get; set; } = "IN";

    /// <summary>
    /// Miktar (pozitif ya da negatif olabilir)
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Birim fiyat
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Toplam tutar (UnitPrice * Quantity)
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// İşlem tarihi
    /// </summary>
    public DateTime MovementDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Kaynağın adı (örneğin: "PurchaseOrder", "SalesOrder")
    /// </summary>
    public string SourceType { get; set; } = string.Empty;

    /// <summary>
    /// Kaynağın kimliği (örneğin: fatura no, sipariş no)
    /// </summary>
    public string SourceReference { get; set; } = string.Empty;

    /// <summary>
    /// Depo veya lokasyon kodu
    /// </summary>
    public string WarehouseCode { get; set; } = "MAIN";

    /// <summary>
    /// Açıklama veya not
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Luca sistemine gönderilecek stok hareket DTO’su
/// </summary>
public class LucaStockMovementDto
{
    public string SKU { get; set; } = string.Empty;
    public string AccountCode { get; set; } = string.Empty;
    public string MovementType { get; set; } = string.Empty;
        public int Quantity { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime MovementDate { get; set; } = DateTime.UtcNow;
    public string Warehouse { get; set; } = string.Empty;
}

/// <summary>
/// Stok senkronizasyon sonucu DTO’su
/// </summary>
public class StockMovementSyncResultDto
{
    public int ProcessedRecords { get; set; }
    public int SuccessfulRecords { get; set; }
    public int FailedRecords { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsSuccess => FailedRecords == 0;
}
