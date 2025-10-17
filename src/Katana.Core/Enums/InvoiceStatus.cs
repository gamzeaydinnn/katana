namespace Katana.Core.Enums;

/// <summary>
/// Fatura durumu
/// </summary>
public enum InvoiceStatus
{
    /// <summary>
    /// Taslak - Henüz gönderilmemiş
    /// </summary>
    Draft = 1,
    
    /// <summary>
    /// Gönderildi - Müşteriye gönderildi
    /// </summary>
    Sent = 2,
    
    /// <summary>
    /// Ödendi - Ödeme tamamlandı
    /// </summary>
    Paid = 3,
    
    /// <summary>
    /// Kısmen Ödendi - Kısmi ödeme alındı
    /// </summary>
    PartiallyPaid = 4,
    
    /// <summary>
    /// Vadesi Geçti - Ödeme vadesi geçti
    /// </summary>
    Overdue = 5,
    
    /// <summary>
    /// İptal Edildi
    /// </summary>
    Cancelled = 6
}
