using Katana.Core.DTOs;

namespace Katana.Business.Interfaces
{
    public interface IExtractorService
    {
        Task<List<ProductDto>> ExtractProductsAsync(DateTime? fromDate = null, CancellationToken ct = default);
        Task<List<InvoiceDto>> ExtractInvoicesAsync(DateTime? fromDate = null, CancellationToken ct = default);
        Task<List<CustomerDto>> ExtractCustomersAsync(DateTime? fromDate = null, CancellationToken ct = default);
    }
}
