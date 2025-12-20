using Katana.Core.Entities;
using Katana.Core.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Katana.Business.Services;





public class NotificationService
{
    private readonly INotificationService _notificationService;

    public NotificationService(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    
    
    
    public async Task CheckLowStockAsync(IEnumerable<Product> products)
    {
        await _notificationService.CheckLowStockAsync(products);
    }

    
    
    
    public async Task CheckInvoiceDueAsync(IEnumerable<Invoice> invoices)
    {
        await _notificationService.CheckInvoiceDueAsync(invoices);
    }

    
    
    
    public async Task SendSystemAlertAsync(string message)
    {
        await _notificationService.SendSystemAlertAsync(message);
    }
}
