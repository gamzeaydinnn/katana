using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Katana.Core.Entities;
using Katana.Core.Enums;
using Katana.Core.Helpers;
using Katana.Data.Configuration;
using Katana.Data.Context;
using Katana.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Katana.Business.Services;

/// <summary>
/// Dönüştürülmüş verileri Luca tarafına aktarır ve işlem sonuçlarını loglar.
/// </summary>
public class LoaderService : ILoaderService
{
    private readonly ILucaService _lucaService;
    private readonly IntegrationDbContext _dbContext;
    private readonly ILogger<LoaderService> _logger;
    private readonly InventorySettings _inventorySettings;

    public LoaderService(
        ILucaService lucaService,
        IntegrationDbContext dbContext,
        IOptions<InventorySettings> inventoryOptions,
        ILogger<LoaderService> logger)
    {
        _lucaService = lucaService;
        _dbContext = dbContext;
        _inventorySettings = inventoryOptions.Value;
        _logger = logger;
    }

    public async Task<int> LoadProductsAsync(IEnumerable<Product> products, int batchSize = 100, CancellationToken ct = default)
    {
        var productList = products.ToList();
        if (!productList.Any())
        {
            _logger.LogInformation("LoaderService => No products to load.");
            return 0;
        }

        var defaultWarehouseCode = string.IsNullOrWhiteSpace(_inventorySettings?.DefaultWarehouseCode)
            ? "MAIN"
            : _inventorySettings.DefaultWarehouseCode.Trim();
        var entryWarehouseCode = string.IsNullOrWhiteSpace(_inventorySettings?.DefaultEntryWarehouseCode)
            ? defaultWarehouseCode
            : _inventorySettings.DefaultEntryWarehouseCode.Trim();
        var exitWarehouseCode = string.IsNullOrWhiteSpace(_inventorySettings?.DefaultExitWarehouseCode)
            ? entryWarehouseCode
            : _inventorySettings.DefaultExitWarehouseCode.Trim();

        // Precompute stock per product from StockMovements to avoid relying on navigation being loaded
        var productIds = productList.Select(p => p.Id).Where(id => id > 0).Distinct().ToList();
        var movementSums = new Dictionary<int, int>();
        if (productIds.Any())
        {
            movementSums = await _dbContext.StockMovements
                .Where(sm => productIds.Contains(sm.ProductId))
                .GroupBy(sm => sm.ProductId)
                .Select(g => new { ProductId = g.Key, Sum = g.Sum(x => x.ChangeQuantity) })
                .ToDictionaryAsync(x => x.ProductId, x => x.Sum);
        }

        var lucaStocks = productList.Select(product => new LucaStockDto
        {
            ProductCode = product.SKU,
            ProductName = product.Name,
            WarehouseCode = entryWarehouseCode,
            EntryWarehouseCode = entryWarehouseCode,
            ExitWarehouseCode = exitWarehouseCode,
            // Always compute balance from DB movements; fallback to product.StockSnapshot if no movements present
            Quantity = Math.Max(movementSums.GetValueOrDefault(product.Id, product.StockSnapshot), 0),
            MovementType = "BALANCE",
            MovementDate = DateTime.UtcNow,
            Description = $"Inventory sync for {product.Name}",
            Reference = "SYNC"
        }).ToList();

        var result = await _lucaService.SendStockMovementsAsync(lucaStocks);
        await WriteIntegrationLogAsync("STOCK", result, ct);

        _logger.LogInformation("LoaderService => Products synced. Success={Success} Failed={Failed}",
            result.SuccessfulRecords, result.FailedRecords);

        return result.SuccessfulRecords;
    }

    public async Task<int> LoadInvoicesAsync(IEnumerable<Invoice> invoices, int batchSize = 50, CancellationToken ct = default)
    {
        var invoiceList = invoices.ToList();
        if (!invoiceList.Any())
        {
            _logger.LogInformation("LoaderService => No invoices to load.");
            return 0;
        }

        var customerIds = invoiceList.Select(i => i.CustomerId).Distinct().ToList();
        var customers = await _dbContext.Customers
            .Where(c => customerIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, ct);

        var accountMappingsList = await _dbContext.MappingTables
            .Where(m => m.MappingType == "SKU_ACCOUNT" && m.IsActive)
            .Select(m => new { m.SourceValue, m.TargetValue })
            .ToListAsync(ct);
        var accountMappings = accountMappingsList.ToDictionary(m => m.SourceValue, m => m.TargetValue, StringComparer.OrdinalIgnoreCase);

        var lucaInvoices = new List<LucaInvoiceDto>();
        foreach (var invoice in invoiceList)
        {
            if (ct.IsCancellationRequested)
            {
                _logger.LogWarning("LoaderService => Invoice load cancelled.");
                break;
            }

            var customer = customers.GetValueOrDefault(invoice.CustomerId) ?? new Customer
            {
                TaxNo = invoice.Customer?.TaxNo ?? "0000000000",
                Title = invoice.Customer?.Title ?? "Unknown Customer"
            };

            var invoiceItems = invoice.InvoiceItems?.ToList() ?? new List<InvoiceItem>();
            lucaInvoices.Add(MappingHelper.MapToLucaInvoice(invoice, customer, invoiceItems, accountMappings));
        }

        var result = await _lucaService.SendInvoicesAsync(lucaInvoices);
        await WriteIntegrationLogAsync("INVOICE", result, ct);

        _logger.LogInformation("LoaderService => Invoices synced. Success={Success} Failed={Failed}",
            result.SuccessfulRecords, result.FailedRecords);

        return result.SuccessfulRecords;
    }

    public async Task<int> LoadCustomersAsync(IEnumerable<Customer> customers, int batchSize = 50, CancellationToken ct = default)
    {
        var customerList = customers.ToList();
        if (!customerList.Any())
        {
            _logger.LogInformation("LoaderService => No customers to load.");
            return 0;
        }

        var lucaCustomers = customerList
            .Select(MappingHelper.MapToLucaCustomer)
            .ToList();

        var result = await _lucaService.SendCustomersAsync(lucaCustomers);
        await WriteIntegrationLogAsync("CUSTOMER", result, ct);

        _logger.LogInformation("LoaderService => Customers synced. Success={Success} Failed={Failed}",
            result.SuccessfulRecords, result.FailedRecords);

        return result.SuccessfulRecords;
    }

    public async Task<int> LoadProductsToLucaAsync(IEnumerable<Product> products, int batchSize = 100, CancellationToken ct = default)
    {
        var productList = products.ToList();
        if (!productList.Any())
        {
            _logger.LogInformation("LoaderService => No products to push to Luca.");
            return 0;
        }

        var lucaProducts = productList.Select(MappingHelper.MapToLucaProduct).ToList();

        var result = await _lucaService.SendProductsAsync(lucaProducts);
        await WriteIntegrationLogAsync("PRODUCT", result, ct);

        _logger.LogInformation("LoaderService => Products pushed. Success={Success} Failed={Failed}",
            result.SuccessfulRecords, result.FailedRecords);

        return result.SuccessfulRecords;
    }

    private async Task WriteIntegrationLogAsync(string syncType, SyncResultDto result, CancellationToken ct)
    {
        var log = new IntegrationLog
        {
            SyncType = syncType,
            Status = result.IsSuccess ? SyncStatus.Success : SyncStatus.Failed,
            Source = DataSource.Katana,
            StartTime = DateTime.UtcNow.Subtract(result.Duration),
            EndTime = DateTime.UtcNow,
            ProcessedRecords = result.ProcessedRecords,
            SuccessfulRecords = result.SuccessfulRecords,
            FailedRecordsCount = result.FailedRecords,
            ErrorMessage = result.IsSuccess ? null : string.Join(Environment.NewLine, result.Errors ?? new List<string>()),
            Details = result.Message
        };

        _dbContext.IntegrationLogs.Add(log);

        if (!result.IsSuccess && (result.Errors?.Any() ?? false))
        {
            foreach (var error in result.Errors!)
            {
                _dbContext.FailedSyncRecords.Add(new FailedSyncRecord
                {
                    IntegrationLog = log,
                    RecordType = syncType,
                    RecordId = Guid.NewGuid().ToString(),
                    ErrorMessage = error,
                    FailedAt = DateTime.UtcNow,
                    Status = "FAILED"
                });
            }
        }

        await _dbContext.SaveChangesAsync(ct);
    }
}
