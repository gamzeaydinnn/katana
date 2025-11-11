using Katana.Business.DTOs;
using Katana.Core.DTOs;

namespace Katana.Business.Interfaces;

public interface ILucaService
{
    // Katana → Luca (Push)
    Task<SyncResultDto> SendInvoicesAsync(List<LucaInvoiceDto> invoices);
    Task<SyncResultDto> SendStockMovementsAsync(List<LucaStockDto> stockMovements);
    Task<SyncResultDto> SendCustomersAsync(List<LucaCustomerDto> customers);
    
    // Luca → Katana (Pull)
    Task<List<LucaInvoiceDto>> FetchInvoicesAsync(DateTime? fromDate = null);
    Task<List<LucaStockDto>> FetchStockMovementsAsync(DateTime? fromDate = null);
    Task<List<LucaCustomerDto>> FetchCustomersAsync(DateTime? fromDate = null);
    
    Task<bool> TestConnectionAsync();
}


