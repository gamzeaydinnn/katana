//Senkronizasyon sürecinde kritik bir hata oluştuğunda sorumlu kişilere e-posta göndermek için kullanılacak. SyncService tarafından çağrılacak.

using System.Threading.Tasks;
using Katana.Core.Entities;

namespace Katana.Infrastructure.Notifications;

public class EmailNotificationService
{
    /// <summary>
    /// Genel e-posta gönderim methodu (ileride SMTP ile genişletilebilir)
    /// </summary>
    public async Task SendEmailAsync(string to, string subject, string body)
    {
        await Task.Run(() =>
        {
            Console.WriteLine("📩 Email Sent");
            Console.WriteLine($"To: {to}");
            Console.WriteLine($"Subject: {subject}");
            Console.WriteLine($"Body: {body}");
        });
    }

    /// <summary>
    /// Düşük stok uyarısı bildirimi
    /// </summary>
    public async Task SendLowStockAlert(Product product)
    {
        var subject = $"⚠️ Düşük Stok Uyarısı: {product.Name}";
        var body = $"'{product.Name}' stokta azaldı. Güncel stok: {product.Stock}";
        await SendEmailAsync("admin@katana.com", subject, body);
    }

    /// <summary>
    /// Vadesi gelen faturalar için uyarı bildirimi
    /// </summary>
    public async Task SendInvoiceDueAlert(string invoiceNumber, DateTime dueDate)
    {
        var subject = $"📅 Fatura Vadesi Yaklaşıyor: {invoiceNumber}";
        var body = $"Fatura {invoiceNumber}, {dueDate:dd.MM.yyyy} tarihinde vadesini dolduracak.";
        await SendEmailAsync("accounting@katana.com", subject, body);
    }

    /// <summary>
    /// Genel sistem hatası uyarısı
    /// </summary>
    public async Task SendSystemAlert(string message)
    {
        var subject = "🚨 Sistem Uyarısı";
        await SendEmailAsync("support@katana.com", subject, message);
    }
}
