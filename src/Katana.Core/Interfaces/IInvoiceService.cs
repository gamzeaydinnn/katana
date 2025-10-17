using Katana.Core.DTOs;

namespace Katana.Core.Interfaces;

/// <summary>
/// Fatura yönetimi servisi arayüzü
/// </summary>
public interface IInvoiceService
{
    // Okuma operasyonları
    Task<List<InvoiceSummaryDto>> GetAllInvoicesAsync();
    Task<InvoiceDto?> GetInvoiceByIdAsync(int id);
    Task<InvoiceDto?> GetInvoiceByNumberAsync(string invoiceNo);
    Task<List<InvoiceSummaryDto>> GetInvoicesByCustomerIdAsync(int customerId);
    Task<List<InvoiceSummaryDto>> GetInvoicesByStatusAsync(string status);
    Task<List<InvoiceSummaryDto>> GetInvoicesByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<List<InvoiceSummaryDto>> GetOverdueInvoicesAsync();
    Task<List<InvoiceSummaryDto>> GetUnsyncedInvoicesAsync();
    
    // Yazma operasyonları
    Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceDto dto);
    Task<InvoiceDto?> UpdateInvoiceAsync(int id, UpdateInvoiceDto dto);
    Task<bool> UpdateInvoiceStatusAsync(int id, UpdateInvoiceStatusDto dto);
    Task<bool> DeleteInvoiceAsync(int id);
    Task<bool> MarkAsSyncedAsync(int id);
    
    // İstatistikler
    Task<InvoiceStatisticsDto> GetInvoiceStatisticsAsync();
    Task<InvoiceStatisticsDto> GetInvoiceStatisticsByDateRangeAsync(DateTime startDate, DateTime endDate);
}
