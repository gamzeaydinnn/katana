using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Katana.Core.Entities;
using Katana.Core.Enums;
using Katana.Core.Helpers;
using Katana.Data.Context;
using Katana.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Katana.Business.Services;

/// <summary>
/// Dönüştürülmüş verileri Luca tarafına aktarır ve işlem sonuçlarını loglar.
/// </summary>
public class LoaderService : ILoaderService
{
    private readonly ILucaService _lucaService;
    private readonly IntegrationDbContext _dbContext;
    private readonly ILogger<LoaderService> _logger;

    public LoaderService(
        ILucaService lucaService,
        IntegrationDbContext dbContext,
        ILogger<LoaderService> logger)
    {
        _lucaService = lucaService;
        _dbContext = dbContext;
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

        var lucaStocks = productList.Select(product => new LucaStockDto
        {
            ProductCode = product.SKU,
            ProductName = product.Name,
            WarehouseCode = "MAIN",
            Quantity = Math.Max(product.Stock, 0),
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

        var accountMappings = await _dbContext.MappingTables
            .Where(m => m.MappingType == "SKU_ACCOUNT" && m.IsActive)
            .ToDictionaryAsync(m => m.SourceValue, m => m.TargetValue, ct);

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
