namespace Katana.Core.DTOs;

/// <summary>
/// Fatura detay DTO - Tam bilgi
/// </summary>
public class InvoiceDto
{
    public int Id { get; set; }
    public string InvoiceNo { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerTaxNo { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string Currency { get; set; } = "TRY";
    public string? Notes { get; set; }
    public bool IsSynced { get; set; }
    public DateTime? SyncedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<InvoiceItemDto> Items { get; set; } = new();
}

/// <summary>
/// Fatura kalemi DTO
/// </summary>
public class InvoiceItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSKU { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Unit { get; set; } = "ADET";
}

/// <summary>
/// Fatura oluşturma DTO
/// </summary>
public class CreateInvoiceDto
{
    public string InvoiceNo { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public DateTime InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string Currency { get; set; } = "TRY";
    public string? Notes { get; set; }
    public List<CreateInvoiceItemDto> Items { get; set; } = new();
}

/// <summary>
/// Fatura kalemi oluşturma DTO
/// </summary>
public class CreateInvoiceItemDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxRate { get; set; } = 0.18m;
}

/// <summary>
/// Fatura güncelleme DTO
/// </summary>
public class UpdateInvoiceDto
{
    public string? Notes { get; set; }
    public DateTime? DueDate { get; set; }
}

/// <summary>
/// Fatura durum değiştirme DTO
/// </summary>
public class UpdateInvoiceStatusDto
{
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Fatura özet DTO - Liste görünümü için
/// </summary>
public class InvoiceSummaryDto
{
    public int Id { get; set; }
    public string InvoiceNo { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public bool IsSynced { get; set; }
    public int ItemCount { get; set; }
}

/// <summary>
/// Fatura istatistik DTO
/// </summary>
public class InvoiceStatisticsDto
{
    public int TotalInvoices { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal UnpaidAmount { get; set; }
    public decimal OverdueAmount { get; set; }
    public int DraftCount { get; set; }
    public int SentCount { get; set; }
    public int PaidCount { get; set; }
    public int OverdueCount { get; set; }
}