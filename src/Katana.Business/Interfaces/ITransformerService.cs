using Katana.Core.DTOs;
using Katana.Core.Entities;

namespace Katana.Business.Interfaces
{
    public interface ITransformerService
    {
        Task<List<Product>> ToProductsAsync(IEnumerable<ProductDto> dtos);
        Task<List<Invoice>> ToInvoicesAsync(IEnumerable<InvoiceDto> dtos);
        Task<List<Customer>> ToCustomersAsync(IEnumerable<CustomerDto> dtos);
    }
}
