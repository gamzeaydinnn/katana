using Katana.Core.DTOs;
using Katana.Core.Entities;

namespace Katana.Business.Interfaces;

public interface IValidationService
{
    Task<ValidationResultDto> ValidateStockMappingAsync(Stock stock, Product product);
    Task<ValidationResultDto> ValidateInvoiceMappingAsync(Invoice invoice, Customer customer);
    Task<ValidationResultDto> ValidateCustomerMappingAsync(Customer customer);
    Task<List<ValidationResultDto>> ValidateBatchDataAsync<T>(List<T> data) where T : class;
}
