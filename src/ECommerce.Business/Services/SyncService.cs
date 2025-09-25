using ECommerce.Core.DTOs;
using ECommerce.Core.Interfaces;
using ECommerce.Core.Entities;
using ECommerce.Core.Helpers;
using ECommerce.Data.Context;
using ECommerce.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ECommerce.Business.Services;

public class SyncService : ISyncService
{
    private readonly IKatanaService _katanaService;
    private readonly ILucaService _lucaService;
    private readonly IMappingService _mappingService;
    private readonly IntegrationDbContext _context;
    private readonly ILogger<SyncService> _logger;
    private readonly SemaphoreSlim _syncSemaphore = new(1, 1);

    public SyncService(
        IKatanaService katanaService,
        ILucaService lucaService,
        IMappingService mappingService,
        IntegrationDbContext context,
        ILogger<SyncService> logger)
    {
        _katanaService = katanaService;
        _lucaService = lucaService;
        _mappingService = mappingService;
        _context = context;
        _logger = logger;
    }

    public async Task<SyncResultDto> SyncStockAsync(DateTime? fromDate = null)
    {
        return await ExecuteSyncAsync("STOCK", async () =>
        {
            fromDate ??= await GetLastSyncTimeAsync("STOCK") ?? DateTime.UtcNow.AddDays(-1);
            var toDate = DateTime.UtcNow;

            _logger.LogInformation("Starting stock sync from {FromDate} to {ToDate}", fromDate, toDate);

            // Get stock changes from Katana
            var katanaStocks = await _katanaService.GetStockChangesAsync(fromDate.Value, toDate);
            
            if (!katanaStocks.Any())
            {
                return new SyncResultDto
                {
                    IsSuccess = true,
                    SyncType = "STOCK",
                    Message = "No stock changes found to sync",
                    ProcessedRecords = 0,
                    SuccessfulRecords = 0,
                    FailedRecords = 0
                };
            }

            // Validate and filter stock data
            var validStocks = katanaStocks.Where(MappingHelper.IsValidKatanaStock).ToList();
            var invalidCount = katanaStocks.Count - validStocks.Count;

            if (invalidCount > 0)
            {
                _logger.LogWarning("Found {InvalidCount} invalid stock records", invalidCount);
            }

            // Convert to database entities
            var stockEntities = new List<Stock>();
            var locationMapping = await _mappingService.GetLocationMappingAsync();

            foreach (var katanaStock in validStocks)
            {
                var product = await GetOrCreateProductAsync(katanaStock.ProductSKU, katanaStock.ProductName);
                if (product != null)
                {
                    var stockEntity = MappingHelper.MapToStock(katanaStock, product.Id);
                    stockEntities.Add(stockEntity);
                }
            }

            // Save to database
            await _context.Stocks.AddRangeAsync(stockEntities);
            await _context.SaveChangesAsync();

            // Convert to Luca format and send
            var lucaStocks = new List<LucaStockDto>();
            foreach (var stock in stockEntities)
            {
                var product = await _context.Products.FindAsync(stock.ProductId);
                if (product != null)
                {
                    var lucaStock = MappingHelper.MapToLucaStock(stock, product, locationMapping);
                    lucaStocks.Add(lucaStock);
                }
            }

            var syncResult = await _lucaService.SendStockMovementsAsync(lucaStocks);

            // Update sync status for successful records
            if (syncResult.IsSuccess)
            {
                var stockIds = stockEntities.Select(s => s.Id).ToList();
                var stocksToUpdate = await _context.Stocks
                    .Where(s => stockIds.Contains(s.Id))
                    .ToListAsync();
                
                foreach (var stock in stocksToUpdate)
                {
                    stock.IsSynced = true;
                    stock.SyncedAt = DateTime.UtcNow;
                }
                
                await _context.SaveChangesAsync();
            }

            return syncResult;
        });
    }

    public async Task<SyncResultDto> SyncInvoicesAsync(DateTime? fromDate = null)
    {
        return await ExecuteSyncAsync("INVOICE", async () =>
        {
            fromDate ??= await GetLastSyncTimeAsync("INVOICE") ?? DateTime.UtcNow.AddDays(-7);
            var toDate = DateTime.UtcNow;

            _logger.LogInformation("Starting invoice sync from {FromDate} to {ToDate}", fromDate, toDate);

            // Get invoices from Katana
            var katanaInvoices = await _katanaService.GetInvoicesAsync(fromDate.Value, toDate);
            
            if (!katanaInvoices.Any())
            {
                return new SyncResultDto
                {
                    IsSuccess = true,
                    SyncType = "INVOICE",
                    Message = "No invoices found to sync",
                    ProcessedRecords = 0,
                    SuccessfulRecords = 0,
                    FailedRecords = 0
                };
            }

            // Validate invoice data
            var validInvoices = katanaInvoices.Where(MappingHelper.IsValidKatanaInvoice).ToList();
            var invalidCount = katanaInvoices.Count - validInvoices.Count;

            if (invalidCount > 0)
            {
                _logger.LogWarning("Found {InvalidCount} invalid invoice records", invalidCount);
            }

            // Process invoices
            var lucaInvoices = new List<LucaInvoiceDto>();
            var skuMapping = await _mappingService.GetSkuToAccountMappingAsync();

            foreach (var katanaInvoice in validInvoices)
            {
                // Create or get customer
                var customer = await GetOrCreateCustomerAsync(katanaInvoice);
                
                // Create invoice entity
                var invoice = MappingHelper.MapToInvoice(katanaInvoice, customer.Id);
                _context.Invoices.Add(invoice);
                await _context.SaveChangesAsync();

                // Create invoice items
                var invoiceItems = new List<InvoiceItem>();
                foreach (var katanaItem in katanaInvoice.Items)
                {
                    var product = await GetOrCreateProductAsync(katanaItem.ProductSKU, katanaItem.ProductName);
                    if (product != null)
                    {
                        var invoiceItem = new InvoiceItem
                        {
                            InvoiceId = invoice.Id,
                            ProductId = product.Id,
                            ProductName = katanaItem.ProductName,
                            ProductSKU = katanaItem.ProductSKU,
                            Quantity = katanaItem.Quantity,
                            UnitPrice = katanaItem.UnitPrice,
                            TaxRate = katanaItem.TaxRate,
                            TaxAmount = katanaItem.TaxAmount,
                            TotalAmount = katanaItem.TotalAmount
                        };
                        invoiceItems.Add(invoiceItem);
                    }
                }

                await _context.InvoiceItems.AddRangeAsync(invoiceItems);
                await _context.SaveChangesAsync();

                // Convert to Luca format
                var lucaInvoice = MappingHelper.MapToLucaInvoice(invoice, customer, invoiceItems, skuMapping);
                lucaInvoices.Add(lucaInvoice);
            }

            // Send to Luca
            var syncResult = await _lucaService.SendInvoicesAsync(lucaInvoices);

            // Update sync status for successful records
            if (syncResult.IsSuccess)
            {
                var invoiceNumbers = validInvoices.Select(i => i.InvoiceNo).ToList();
                var invoicesToUpdate = await _context.Invoices
                    .Where(i => invoiceNumbers.Contains(i.InvoiceNo))
                    .ToListAsync();
                
                foreach (var invoice in invoicesToUpdate)
                {
                    invoice.IsSynced = true;
                    invoice.SyncedAt = DateTime.UtcNow;
                }
                
                await _context.SaveChangesAsync();
            }

            return syncResult;
        });
    }

    public async Task<SyncResultDto> SyncCustomersAsync(DateTime? fromDate = null)
    {
        return await ExecuteSyncAsync("CUSTOMER", async () =>
        {
            fromDate ??= await GetLastSyncTimeAsync("CUSTOMER") ?? DateTime.UtcNow.AddDays(-1);

            _logger.LogInformation("Starting customer sync from {FromDate}", fromDate);

            // Get unsynchronized customers from database
            var customers = await _context.Customers
                .Where(c => c.IsActive 
                    && !c.IsSynced 
                    && (fromDate == null || c.UpdatedAt >= fromDate))
                .ToListAsync();

            if (!customers.Any())
            {
                return new SyncResultDto
                {
                    IsSuccess = true,
                    SyncType = "CUSTOMER",
                    Message = "No customers found to sync",
                    ProcessedRecords = 0,
                    SuccessfulRecords = 0,
                    FailedRecords = 0
                };
            }

            // Convert to Luca format
            var lucaCustomers = customers.Select(MappingHelper.MapToLucaCustomer).ToList();

            _logger.LogInformation("Sending {Count} customers to Luca", lucaCustomers.Count);

            // Send to Luca
            var syncResult = await _lucaService.SendCustomersAsync(lucaCustomers);

            // Update sync status for successful records
            if (syncResult.IsSuccess)
            {
                foreach (var customer in customers)
                {
                    customer.IsSynced = true;
                    customer.SyncedAt = DateTime.UtcNow;
                }
                
                await _context.SaveChangesAsync();
            }

            return syncResult;
        });
    }

    public async Task<BatchSyncResultDto> SyncAllAsync(DateTime? fromDate = null)
    {
        var results = new List<SyncResultDto>();
        var overallStartTime = DateTime.UtcNow;

        _logger.LogInformation("Starting complete sync operation");

        try
        {
            // Run syncs in sequence to avoid conflicts
            results.Add(await SyncCustomersAsync(fromDate));
            results.Add(await SyncStockAsync(fromDate));
            results.Add(await SyncInvoicesAsync(fromDate));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during batch sync operation");
        }

        return new BatchSyncResultDto
        {
            Results = results,
            BatchTime = overallStartTime
        };
    }

    public async Task<List<SyncStatusDto>> GetSyncStatusAsync()
    {
        var statusList = new List<SyncStatusDto>();
        var syncTypes = new[] { "STOCK", "INVOICE", "CUSTOMER" };

        foreach (var syncType in syncTypes)
        {
            var lastLog = await _context.IntegrationLogs
                .Where(l => l.SyncType == syncType)
                .OrderByDescending(l => l.StartTime)
                .FirstOrDefaultAsync();

            var pendingRecords = syncType switch
            {
                "STOCK" => await _context.Stocks.CountAsync(s => !s.IsSynced),
                "INVOICE" => await _context.Invoices.CountAsync(i => !i.IsSynced),
                "CUSTOMER" => await _context.Customers.CountAsync(c => c.IsActive && !c.IsSynced),
                _ => 0
            };

            statusList.Add(new SyncStatusDto
            {
                SyncType = syncType,
                LastSyncTime = lastLog?.StartTime,
                IsRunning = lastLog?.Status == "RUNNING",
                CurrentStatus = lastLog?.Status,
                PendingRecords = pendingRecords,
                NextScheduledSync = CalculateNextSyncTime(syncType)
            });
        }

        return statusList;
    }

    public async Task<bool> IsSyncRunningAsync(string syncType)
    {
        var runningLog = await _context.IntegrationLogs
            .Where(l => l.SyncType == syncType && l.Status == "RUNNING")
            .FirstOrDefaultAsync();

        return runningLog != null;
    }

    private async Task<SyncResultDto> ExecuteSyncAsync(string syncType, Func<Task<SyncResultDto>> syncAction)
    {
        await _syncSemaphore.WaitAsync();

        try
        {
            // Check if sync is already running
            if (await IsSyncRunningAsync(syncType))
            {
                return new SyncResultDto
                {
                    IsSuccess = false,
                    SyncType = syncType,
                    Message = $"{syncType} sync is already running",
                    Errors = new List<string> { "Sync already in progress" }
                };
            }

            // Create integration log
            var log = new IntegrationLog
            {
                SyncType = syncType,
                Status = "RUNNING",
                StartTime = DateTime.UtcNow,
                TriggeredBy = "System"
            };

            _context.IntegrationLogs.Add(log);
            await _context.SaveChangesAsync();

            try
            {
                var result = await syncAction();

                // Update log with results
                log.Status = result.IsSuccess ? "SUCCESS" : "FAILED";
                log.EndTime = DateTime.UtcNow;
                log.ProcessedRecords = result.ProcessedRecords;
                log.SuccessfulRecords = result.SuccessfulRecords;
                log.FailedRecordsCount = result.FailedRecords;
                log.ErrorMessage = result.Errors.Any() ? string.Join("; ", result.Errors) : null;
                log.Details = JsonSerializer.Serialize(result);

                await _context.SaveChangesAsync();

                return result;
            }
            catch (Exception ex)
            {
                // Update log with error
                log.Status = "FAILED";
                log.EndTime = DateTime.UtcNow;
                log.ErrorMessage = ex.Message;

                await _context.SaveChangesAsync();

                throw;
            }
        }
        finally
        {
            _syncSemaphore.Release();
        }
    }

    private async Task<DateTime?> GetLastSyncTimeAsync(string syncType)
    {
        var lastLog = await _context.IntegrationLogs
            .Where(l => l.SyncType == syncType && l.Status == "SUCCESS")
            .OrderByDescending(l => l.EndTime)
            .FirstOrDefaultAsync();

        return lastLog?.EndTime;
    }

    private async Task<Product?> GetOrCreateProductAsync(string sku, string name)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.SKU == sku);
        
        if (product == null)
        {
            product = new Product
            {
                SKU = sku,
                Name = name,
                Price = 0,
                Stock = 0,
                CategoryId = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();
        }

        return product;
    }

    private async Task<Customer> GetOrCreateCustomerAsync(KatanaInvoiceDto katanaInvoice)
    {
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.TaxNo == katanaInvoice.CustomerTaxNo);
        
        if (customer == null)
        {
            customer = MappingHelper.MapToCustomer(katanaInvoice);
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
        }

        return customer;
    }

    private DateTime? CalculateNextSyncTime(string syncType)
    {
        // This would typically be calculated based on configuration
        return DateTime.UtcNow.AddHours(6);
    }
}