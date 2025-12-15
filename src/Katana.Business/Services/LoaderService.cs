using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Katana.Core.Entities;
using Katana.Core.Enums;
using Katana.Core.Helpers;
using Katana.Business.Mappers;
using Katana.Data.Configuration;
using Katana.Data.Context;
using Katana.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Katana.Business.Services;

public class LoaderService : ILoaderService
{
    private readonly ILucaService _lucaService;
    private readonly IntegrationDbContext _dbContext;
    private readonly ILogger<LoaderService> _logger;
    private readonly InventorySettings _inventorySettings;
    private readonly LucaApiSettings _lucaSettings;
    private readonly KatanaMappingSettings _katanaMappingSettings;

    public LoaderService(
        ILucaService lucaService,
        IntegrationDbContext dbContext,
        IOptions<InventorySettings> inventoryOptions,
        IOptions<LucaApiSettings> lucaOptions,
        IOptions<KatanaMappingSettings> mappingOptions,
        ILogger<LoaderService> logger)
    {
        _lucaService = lucaService;
        _dbContext = dbContext;
        _inventorySettings = inventoryOptions.Value;
        _lucaSettings = lucaOptions.Value;
        _katanaMappingSettings = mappingOptions.Value;
        _logger = logger;
    }

    public async Task<int> LoadProductsAsync(IEnumerable<Product> products, IReadOnlyDictionary<string, string>? locationMappings = null, int batchSize = 100, CancellationToken ct = default)
    {
        var productList = products?.ToList() ?? new List<Product>();
        if (!productList.Any())
        {
            _logger.LogInformation("LoaderService => No products to load.");
            return 0;
        }

        var warehouseMappings = await ResolveMappingsAsync(locationMappings, "LOCATION_WAREHOUSE", ct);
        ApplyWarehouseDefaults(warehouseMappings);

        var productIds = productList.Select(p => p.Id).Where(id => id > 0).Distinct().ToList();
        var stockRows = productIds.Any()
            ? await _dbContext.Stocks
                .Include(s => s.Product)
                .Where(s => productIds.Contains(s.ProductId))
                .ToListAsync(ct)
            : new List<Stock>();

        var lucaStocks = new List<LucaStockDto>();
        foreach (var stock in stockRows)
        {
            var owningProduct = stock.Product ?? productList.FirstOrDefault(p => p.Id == stock.ProductId);
            if (owningProduct == null)
            {
                _logger.LogWarning("LoaderService => Stock row #{StockId} missing product reference. ProductId={ProductId}", stock.Id, stock.ProductId);
                continue;
            }

            lucaStocks.Add(MappingHelper.MapToLucaStock(stock, owningProduct, warehouseMappings));
        }

        if (!lucaStocks.Any())
        {
            foreach (var product in productList)
            {
                var fallbackStock = new Stock
                {
                    ProductId = product.Id,
                    Product = product,
                    Location = "DEFAULT",
                    Quantity = product.StockSnapshot,
                    Type = product.StockSnapshot >= 0 ? "IN" : "OUT",
                    Timestamp = DateTime.UtcNow,
                    Reference = "SNAPSHOT"
                };

                lucaStocks.Add(MappingHelper.MapToLucaStock(fallbackStock, product, warehouseMappings));
            }
        }

        LogStockPreview(lucaStocks);

        var result = await _lucaService.SendStockMovementsAsync(lucaStocks);
        await WriteIntegrationLogAsync("STOCK", result, ct);

        _logger.LogInformation("LoaderService => Products synced. Success={Success} Failed={Failed}",
            result.SuccessfulRecords, result.FailedRecords);

        if (result.FailedRecords > 0)
        {
            try
            {
                _logger.LogWarning("LoaderService => Some stock movements failed. FailedCount={Failed}. Errors={Errors}", result.FailedRecords, string.Join(";", result.Errors ?? new List<string>()));
            }
            catch { }
        }

        return result.SuccessfulRecords;
    }

    public async Task<int> LoadInvoicesAsync(IEnumerable<Invoice> invoices, IReadOnlyDictionary<string, string>? skuAccountMappings = null, int batchSize = 50, CancellationToken ct = default)
    {
        var invoiceList = invoices?.ToList() ?? new List<Invoice>();
        if (!invoiceList.Any())
        {
            _logger.LogInformation("LoaderService => No invoices to load.");
            return 0;
        }

        var customerIds = invoiceList.Select(i => i.CustomerId).Distinct().ToList();
        var customers = await _dbContext.Customers
            .Where(c => customerIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, ct);

        var accountMappings = await ResolveMappingsAsync(skuAccountMappings, "SKU_ACCOUNT", ct);
        var belgeSeri = string.IsNullOrWhiteSpace(_lucaSettings.DefaultBelgeSeri)
            ? "EFA2025"
            : _lucaSettings.DefaultBelgeSeri.Trim();
        var defaultWarehouseCode = ResolveWarehouseCode();

        var lucaInvoices = new List<LucaCreateInvoiceHeaderRequest>();
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
            var belgeTurDetayId = KatanaToLucaMapper.GetBelgeTurDetayIdForInvoiceType(invoice);
            var request = KatanaToLucaMapper.MapInvoiceToCreateRequest(
                invoice,
                customer,
                invoiceItems,
                accountMappings,
                belgeSeri,
                belgeTurDetayId,
                defaultWarehouseCode);

            lucaInvoices.Add(request);
        }

        var result = await _lucaService.SendInvoicesAsync(lucaInvoices);
        await WriteIntegrationLogAsync("INVOICE", result, ct);

        _logger.LogInformation("LoaderService => Invoices synced. Success={Success} Failed={Failed}",
            result.SuccessfulRecords, result.FailedRecords);

        return result.SuccessfulRecords;
    }

    public async Task<int> LoadCustomersAsync(IEnumerable<Customer> customers, IReadOnlyDictionary<string, string>? customerTypeMappings = null, int batchSize = 50, CancellationToken ct = default)
    {
        var customerList = customers?.ToList() ?? new List<Customer>();
        if (!customerList.Any())
        {
            _logger.LogInformation("LoaderService => No customers to load.");
            return 0;
        }

        var resolvedMappings = await ResolveMappingsAsync(customerTypeMappings, "CUSTOMER_TYPE", ct);
        var lucaCustomers = customerList
            .Select(c => KatanaToLucaMapper.MapCustomerToCreateRequest(c, resolvedMappings))
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

        // Load PRODUCT_CATEGORY mappings to pass to the mapper.
        var productCategoryMappings = await ResolveMappingsAsync(null, "PRODUCT_CATEGORY", ct);

        var lucaStockCards = new List<LucaCreateStokKartiRequest>();
        foreach (var product in productList)
        {
            // Create a temporary DTO to reuse the mapper logic from SyncService
            var productDto = new KatanaProductDto
            {
                SKU = product.SKU,
                Name = product.Name,
                Category = product.Category, // Assuming Product entity has a string Category property
                CategoryId = product.CategoryId,
                Barcode = product.Barcode,
                Price = product.Price,
                CostPrice = product.CostPrice,
                PurchasePrice = product.PurchasePrice
            };

            var card = KatanaToLucaMapper.MapKatanaProductToStockCard(
                productDto,
                _lucaSettings,
                productCategoryMappings,
                _katanaMappingSettings,
                unitMappings: _lucaSettings.UnitMapping);
            
            lucaStockCards.Add(card);
        }

        var validCards = new List<LucaCreateStokKartiRequest>();
        foreach (var card in lucaStockCards)
        {
            var (isValid, errors) = MappingHelper.ValidateLucaStockCard(card);
            if (!isValid)
            {
                _logger.LogWarning("LoaderService => Stock card validation failed for KartKodu={KartKodu}: {Errors}", card.KartKodu, string.Join(";", errors));
                continue;
            }
            validCards.Add(card);
        }

        try
        {
            var skus = string.Join(',', lucaStockCards.Take(12).Select(p => p.KartKodu));
            var sample = JsonSerializer.Serialize(lucaStockCards.Take(6).Select(p => new { p.KartKodu, p.KartAdi }));
            _logger.LogInformation("LoaderService => Preparing {Count} products to push. SKUs={SKUs}; Sample={Sample}", lucaStockCards.Count, skus, sample);
        }
        catch { }

        var result = await _lucaService.SendStockCardsAsync(validCards);
        await WriteIntegrationLogAsync("PRODUCT", result, ct);

        _logger.LogInformation("LoaderService => Products pushed. Success={Success} Failed={Failed}",
            result.SuccessfulRecords, result.FailedRecords);

        if (result.FailedRecords > 0)
        {
            try
            {
                _logger.LogWarning("LoaderService => Some products failed to push. FailedCount={Failed}. Errors={Errors}", result.FailedRecords, string.Join(";", result.Errors ?? new List<string>()));
            }
            catch { }
        }

        return result.SuccessfulRecords;
    }

    private void LogStockPreview(List<LucaStockDto> lucaStocks)
    {
        if (lucaStocks == null || lucaStocks.Count == 0)
        {
            return;
        }

        try
        {
            var sampleSkus = string.Join(',', lucaStocks.Take(8).Select(s => s.ProductCode));
            var samplePayload = JsonSerializer.Serialize(lucaStocks.Take(5).Select(s => new { s.ProductCode, s.Quantity, s.WarehouseCode }));
            _logger.LogInformation("LoaderService => Preparing {Count} stock movements. SampleSKUs={SampleSkus}; SamplePayload={SamplePayload}", lucaStocks.Count, sampleSkus, samplePayload);
        }
        catch
        {
            
        }
    }

    private void ApplyWarehouseDefaults(Dictionary<string, string> warehouseMappings)
    {
        var defaultWarehouse = ResolveWarehouseCode();
        if (!warehouseMappings.ContainsKey("DEFAULT"))
        {
            warehouseMappings["DEFAULT"] = defaultWarehouse;
        }
    }

    private string ResolveWarehouseCode()
    {
        if (!string.IsNullOrWhiteSpace(_inventorySettings?.DefaultWarehouseCode))
        {
            return _inventorySettings.DefaultWarehouseCode.Trim();
        }

        return "0001-0001";
    }

    private async Task<Dictionary<string, string>> ResolveMappingsAsync(
        IReadOnlyDictionary<string, string>? provided,
        string mappingType,
        CancellationToken ct)
    {
        if (provided != null)
        {
            return new Dictionary<string, string>(provided, StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var normalized = string.IsNullOrWhiteSpace(mappingType)
                ? string.Empty
                : mappingType.Trim().ToUpperInvariant();

            var entries = await _dbContext.MappingTables
                .Where(m => m.IsActive && m.MappingType != null && m.MappingType.ToUpper() == normalized)
                .Select(m => new { m.SourceValue, m.TargetValue })
                .ToListAsync(ct);

            return entries
                .Where(e => !string.IsNullOrWhiteSpace(e.SourceValue))
                .ToDictionary(
                    e => e.SourceValue,
                    e => e.TargetValue,
                    StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LoaderService => Failed to load mapping table {MappingType}. Falling back to empty mapping.", mappingType);
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
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
            ErrorMessage = result.IsSuccess ? null : Truncate(result.Errors is null ? null : string.Join(Environment.NewLine, result.Errors), 1900),
            Details = Truncate(result.Message, 4000)
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
                    ErrorMessage = Truncate(error, 1900) ?? string.Empty,
                    FailedAt = DateTime.UtcNow,
                    Status = "FAILED"
                });
            }
        }

        await _dbContext.SaveChangesAsync(ct);
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        if (maxLength <= 0) return null;
        return value.Length <= maxLength ? value : value.Substring(0, maxLength);
    }
}
