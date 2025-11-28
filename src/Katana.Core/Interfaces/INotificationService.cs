using Katana.Core.Entities;

namespace Katana.Core.Interfaces;

public interface INotificationService
{
    
    
    
    
    Task CheckLowStockAsync(IEnumerable<Product> products);

    
    
    
    
    Task CheckInvoiceDueAsync(IEnumerable<Invoice> invoices);

    
    
    
    
    Task SendSystemAlertAsync(string message);
}
