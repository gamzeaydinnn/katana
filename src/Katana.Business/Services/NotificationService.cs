using Katana.Core.Entities;
using Katana.Core.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Katana.Business.Services;

/// <summary>
/// Uygulamanın genelinde bildirimleri (e-posta vb.) yönetmek için kullanılır.
/// Infrastructure katmanındaki e-posta servislerini interface aracılığıyla çağırır.
/// </summary>
public class NotificationService
{
    private readonly INotificationService _notificationService;

    public NotificationService(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>
    /// Düşük stoklu ürünleri kontrol eder ve uyarı gönderir.
    /// </summary>
    public async Task CheckLowStockAsync(IEnumerable<Product> products)
    {
        await _notificationService.CheckLowStockAsync(products);
    }

    /// <summary>
    /// Vadesi yaklaşan faturalar için bildirim gönderir.
    /// </summary>
    public async Task CheckInvoiceDueAsync(IEnumerable<Invoice> invoices)
    {
        await _notificationService.CheckInvoiceDueAsync(invoices);
    }

    /// <summary>
    /// Genel sistem hatası bildirimi gönderir.
    /// </summary>
    public async Task SendSystemAlertAsync(string message)
    {
        await _notificationService.SendSystemAlertAsync(message);
    }
}
