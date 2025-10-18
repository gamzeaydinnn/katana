using Katana.Core.DTOs;

namespace Katana.Business.Interfaces
{
    public interface IExtractorService
    {
        Task<List<ProductDto>> ExtractProductsAsync(CancellationToken ct = default);
        Task<List<InvoiceDto>> ExtractInvoicesAsync(CancellationToken ct = default);
        Task<List<CustomerDto>> ExtractCustomersAsync(CancellationToken ct = default);
    }
}
