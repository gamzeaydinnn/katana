using Katana.Business.DTOs;
using Katana.Core.DTOs;

namespace Katana.Business.Interfaces;

public interface ILucaService
{
    Task<SyncResultDto> SendInvoicesAsync(List<LucaInvoiceDto> invoices);
    Task<SyncResultDto> SendStockMovementsAsync(List<LucaStockDto> stockMovements);
    Task<SyncResultDto> SendCustomersAsync(List<LucaCustomerDto> customers);
    Task<bool> TestConnectionAsync();
}


