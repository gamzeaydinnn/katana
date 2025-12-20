using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Katana.Core.Entities;
using Katana.Core.Interfaces;

namespace Katana.Infrastructure.Notifications;
public class EmailNotificationService : INotificationService
{
    public async Task CheckLowStockAsync(IEnumerable<Product> products)
    {
        foreach (var p in products.Where(x => x.Stock < 10 && x.IsActive))
        {
            await SendEmailAsync(
                "admin@katana.com",
                $"⚠️ Düşük Stok Uyarısı: {p.Name}",
                $"'{p.Name}' stokta azaldı. Güncel stok: {p.Stock}"
            );
        }
    }

    public async Task CheckInvoiceDueAsync(IEnumerable<Invoice> invoices)
    {
        foreach (var inv in invoices.Where(i => i.DueDate <= DateTime.UtcNow.AddDays(3)))
        {
            await SendEmailAsync(
                "accounting@katana.com",
                $"📅 Fatura Vadesi Yaklaşıyor: {inv.InvoiceNo}",
                $"Fatura {inv.InvoiceNo}, {inv.DueDate:dd.MM.yyyy} tarihinde vadesini dolduracak."
            );
        }
    }

    public async Task SendSystemAlertAsync(string message)
    {
        await SendEmailAsync(
            "support@katana.com",
            "🚨 Sistem Uyarısı",
            message
        );
    }

    
    private async Task SendEmailAsync(string to, string subject, string body)
    {
        await Task.Run(() =>
        {
            Console.WriteLine("📩 E-posta Gönderildi:");
            Console.WriteLine($"To: {to}");
            Console.WriteLine($"Subject: {subject}");
            Console.WriteLine($"Body: {body}");
        });
    }
}
