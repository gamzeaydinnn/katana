using System.Collections.Generic;
using Katana.Core.Entities;

namespace Katana.Business.Interfaces
{
    public interface ILoaderService
    {
        Task<int> LoadProductsAsync(IEnumerable<Product> products, IReadOnlyDictionary<string, string>? locationMappings = null, int batchSize = 100, CancellationToken ct = default);
        Task<int> LoadInvoicesAsync(IEnumerable<Invoice> invoices, IReadOnlyDictionary<string, string>? skuAccountMappings = null, int batchSize = 50, CancellationToken ct = default);
        Task<int> LoadCustomersAsync(IEnumerable<Customer> customers, IReadOnlyDictionary<string, string>? customerTypeMappings = null, int batchSize = 50, CancellationToken ct = default);
        // Push product records (create / update) to Luca
        Task<int> LoadProductsToLucaAsync(IEnumerable<Product> products, int batchSize = 100, CancellationToken ct = default);
    }
}
