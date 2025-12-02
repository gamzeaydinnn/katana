using Katana.Core.DTOs;
using Katana.Core.Entities;

namespace Katana.Core.Interfaces;

public interface ICustomerService
{
    Task<IEnumerable<CustomerDto>> GetAllCustomersAsync();
    Task<IEnumerable<CustomerSummaryDto>> GetActiveCustomersAsync();
    Task<CustomerDto?> GetCustomerByIdAsync(int id);
    Task<Customer?> GetCustomerEntityByIdAsync(int id);
    Task<CustomerDto?> GetCustomerByTaxNoAsync(string taxNo);
    Task<IEnumerable<CustomerDto>> SearchCustomersAsync(string searchTerm);
    Task<CustomerDto> CreateCustomerAsync(CreateCustomerDto dto);
    Task<CustomerDto> UpdateCustomerAsync(int id, UpdateCustomerDto dto);
    Task<bool> DeleteCustomerAsync(int id);
    Task<bool> ActivateCustomerAsync(int id);
    Task<bool> DeactivateCustomerAsync(int id);
    Task<CustomerStatisticsDto> GetCustomerStatisticsAsync();
    Task<decimal> GetCustomerBalanceAsync(int customerId);
}
