using Katana.Core.Entities;
using Katana.Infrastructure.Notifications;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Katana.Core.Entities;

namespace Katana.Business.Services;

public class NotificationService
{
    private readonly EmailNotificationService _emailService;

    public NotificationService(EmailNotificationService emailService)
    {
        _emailService = emailService;
    }

    /// <summary>
    /// Düşük stoklu ürünleri kontrol eder ve uyarı gönderir.
    /// </summary>
    public async Task CheckLowStockAsync(IEnumerable<Product> products)
    {
        foreach (var p in products.Where(x => x.Stock < 10 && x.IsActive))
        {
            await _emailService.SendLowStockAlert(p);
        }
    }

    /// <summary>
    /// Vadesi yaklaşan faturalar için bildirim gönderir.
    /// </summary>
    public async Task CheckInvoiceDueAsync(IEnumerable<Invoice> invoices)
    {
        foreach (var inv in invoices.Where(i => i.DueDate <= DateTime.UtcNow.AddDays(3)))
        {
            await _emailService.SendInvoiceDueAlert(inv.InvoiceNumber, inv.DueDate);
        }
    }

    /// <summary>
    /// Genel sistem hatası bildirimi.
    /// </summary>
    public async Task SendSystemAlertAsync(string message)
    {
        await _emailService.SendSystemAlert(message);
    }
}
