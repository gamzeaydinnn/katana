using Katana.Core.Entities;

namespace Katana.Core.Interfaces;

/// <summary>
/// Sistem genelinde e-posta veya diğer kanallarla bildirim göndermek için kullanılır.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Düşük stoklu ürünleri kontrol eder ve uyarı gönderir.
    /// </summary>
    /// <param name="products">Ürün listesi</param>
    Task CheckLowStockAsync(IEnumerable<Product> products);

    /// <summary>
    /// Vadesi yaklaşan faturalar için bildirim gönderir.
    /// </summary>
    /// <param name="invoices">Fatura listesi</param>
    Task CheckInvoiceDueAsync(IEnumerable<Invoice> invoices);

    /// <summary>
    /// Sistemsel hatalar için e-posta uyarısı gönderir.
    /// </summary>
    /// <param name="message">Hata mesajı</param>
    Task SendSystemAlertAsync(string message);
}
