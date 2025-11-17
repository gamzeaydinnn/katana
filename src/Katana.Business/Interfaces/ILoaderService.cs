using Katana.Core.Entities;

namespace Katana.Business.Interfaces
{
    public interface ILoaderService
    {
        Task<int> LoadProductsAsync(IEnumerable<Product> products, int batchSize = 100, CancellationToken ct = default);
        Task<int> LoadInvoicesAsync(IEnumerable<Invoice> invoices, int batchSize = 50, CancellationToken ct = default);
        Task<int> LoadCustomersAsync(IEnumerable<Customer> customers, int batchSize = 50, CancellationToken ct = default);
        // Push product records (create / update) to Luca
        Task<int> LoadProductsToLucaAsync(IEnumerable<Product> products, int batchSize = 100, CancellationToken ct = default);
    }
}
