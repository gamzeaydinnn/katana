using System.Diagnostics;
using Katana.Business.Interfaces;
using System.Linq;
using Katana.Core.DTOs;
using Katana.Core.Entities;
using Katana.Core.Enums;
using Katana.Core.Helpers;
using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Katana.Business.UseCases.Sync;

/// <summary>
/// ETL bileşenlerini koordine ederek Katana ↔ Luca senkronizasyonunu gerçekleştirir.
/// </summary>
public class SyncService : ISyncService, IIntegrationService
{
    private readonly IExtractorService _extractorService;
    private readonly ITransformerService _transformerService;
    private readonly ILoaderService _loaderService;
    private readonly ILucaService _lucaService;
    private readonly IntegrationDbContext _dbContext;
    private readonly ILogger<SyncService> _logger;

    public SyncService(
        IExtractorService extractorService,
        ITransformerService transformerService,
        ILoaderService loaderService,
        ILucaService lucaService,
        IntegrationDbContext dbContext,
        ILogger<SyncService> logger)
    {
        _extractorService = extractorService;
        _transformerService = transformerService;
        _loaderService = loaderService;
        _lucaService = lucaService;
        _dbContext = dbContext;
        _logger = logger;
    }

    public Task<SyncResultDto> SyncStockAsync(DateTime? fromDate = null) =>
        ExecuteSyncAsync("STOCK", async ct =>
        {
            var productDtos = await _extractorService.ExtractProductsAsync(fromDate, ct);
            var products = await _transformerService.ToProductsAsync(productDtos);
            var successful = await _loaderService.LoadProductsAsync(products, ct: ct);
            return BuildResult("STOCK", productDtos.Count, successful);
        });

    public Task<SyncResultDto> SyncProductsAsync(DateTime? fromDate = null) =>
        ExecuteSyncAsync("PRODUCT", async ct =>
        {
            _logger.LogInformation("🔄 Starting PRODUCT sync: Katana → Luca");

            var productDtos = await _extractorService.ExtractProductsAsync(fromDate, ct);
            _logger.LogInformation("📥 Extracted {Count} products from Katana", productDtos.Count);

            var products = await _transformerService.ToProductsAsync(productDtos);
            _logger.LogInformation("🔀 Transformed {Count} products", products.Count());

            var successful = await _loaderService.LoadProductsToLucaAsync(products, ct: ct);
            _logger.LogInformation("✅ Successfully sent {Successful}/{Total} products to Luca", successful, productDtos.Count);

            return BuildResult("PRODUCT", productDtos.Count, successful);
        });

    public Task<SyncResultDto> SyncInvoicesAsync(DateTime? fromDate = null) =>
        ExecuteSyncAsync("INVOICE", async ct =>
        {
            var invoiceDtos = await _extractorService.ExtractInvoicesAsync(fromDate, ct);
            var invoices = await _transformerService.ToInvoicesAsync(invoiceDtos);
            var successful = await _loaderService.LoadInvoicesAsync(invoices, ct: ct);
            return BuildResult("INVOICE", invoiceDtos.Count, successful);
        });

    public Task<SyncResultDto> SyncCustomersAsync(DateTime? fromDate = null) =>
        ExecuteSyncAsync("CUSTOMER", async ct =>
        {
            var customerDtos = await _extractorService.ExtractCustomersAsync(fromDate, ct);
            var customers = await _transformerService.ToCustomersAsync(customerDtos);
            var successful = await _loaderService.LoadCustomersAsync(customers, ct: ct);
            return BuildResult("CUSTOMER", customerDtos.Count, successful);
        });

    public async Task<BatchSyncResultDto> SyncAllAsync(DateTime? fromDate = null)
    {
        var results = new List<SyncResultDto>
        {
            await SyncCustomersAsync(fromDate),
            await SyncStockAsync(fromDate),
            await SyncInvoicesAsync(fromDate),
            await SyncProductsAsync(fromDate)
        };

        return new BatchSyncResultDto
        {
            Results = results,
            BatchTime = DateTime.UtcNow
        };
    }

    public async Task<List<SyncStatusDto>> GetSyncStatusAsync()
    {
        var latestLogs = await _dbContext.SyncOperationLogs
            .OrderByDescending(log => log.StartTime)
            .GroupBy(log => log.SyncType)
            .Select(group => group.First())
            .ToListAsync();

        return latestLogs.Select(log => new SyncStatusDto
        {
            SyncType = log.SyncType,
            LastSyncTime = log.EndTime,
            IsRunning = string.Equals(log.Status, "RUNNING", StringComparison.OrdinalIgnoreCase),
            CurrentStatus = log.Status,
            PendingRecords = 0
        }).ToList();
    }

    public async Task<bool> IsSyncRunningAsync(string syncType)
    {
        var normalizedType = syncType.ToUpperInvariant();
        return await _dbContext.SyncOperationLogs
            .AnyAsync(log => log.SyncType == normalizedType && log.Status == "RUNNING");
    }

    private async Task<SyncResultDto> ExecuteSyncAsync(
        string syncType,
        Func<CancellationToken, Task<SyncResultDto>> operation)
    {
        var stopwatch = Stopwatch.StartNew();
        var logEntry = await StartOperationLogAsync(syncType);
        var cts = new CancellationTokenSource();

        try
        {
            var result = await operation(cts.Token);
            stopwatch.Stop();

            result.SyncType = syncType;
            result.SyncTime = DateTime.UtcNow;
            result.Duration = stopwatch.Elapsed;

            await FinalizeOperationAsync(
                logEntry,
                "SUCCESS",
                result.ProcessedRecords,
                result.SuccessfulRecords,
                result.FailedRecords,
                null);

            _logger.LogInformation("SyncService => {SyncType} sync completed. Processed={Processed} Success={Success}",
                syncType, result.ProcessedRecords, result.SuccessfulRecords);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            await FinalizeOperationAsync(
                logEntry,
                "FAILED",
                0,
                0,
                0,
                ex.Message);

            _logger.LogError(ex, "SyncService => {SyncType} sync failed.", syncType);

            return new SyncResultDto
            {
                SyncType = syncType,
                IsSuccess = false,
                Message = ex.Message,
                SyncTime = DateTime.UtcNow,
                Duration = stopwatch.Elapsed,
                Errors = { ex.ToString() }
            };
        }
    }

    private async Task<SyncOperationLog> StartOperationLogAsync(string syncType)
    {
        var log = new SyncOperationLog
        {
            SyncType = syncType,
            Status = "RUNNING",
            StartTime = DateTime.UtcNow
        };

        _dbContext.SyncOperationLogs.Add(log);
        await _dbContext.SaveChangesAsync();
        return log;
    }

    private async Task FinalizeOperationAsync(
        SyncOperationLog log,
        string status,
        int processed,
        int successful,
        int failed,
        string? errorMessage)
    {
        log.Status = status;
        log.ProcessedRecords = processed;
        log.SuccessfulRecords = successful;
        log.FailedRecords = failed;
        log.EndTime = DateTime.UtcNow;
        log.ErrorMessage = Truncate(errorMessage, 2000);

        await _dbContext.SaveChangesAsync();
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength ? value : value.Substring(0, maxLength);
    }

    private static SyncResultDto BuildResult(string syncType, int processed, int successful) =>
        new()
        {
            SyncType = syncType,
            IsSuccess = successful == processed,
            ProcessedRecords = processed,
            SuccessfulRecords = successful,
            FailedRecords = processed - successful,
            Message = successful == processed
                ? $"{processed} kayıt senkronize edildi."
                : $"{processed} kaydın {successful} tanesi senkronize edildi."
        };

    // Luca → Katana (Reverse Sync) implementations
    public async Task<SyncResultDto> SyncStockFromLucaAsync(DateTime? fromDate = null)
    {
        var stopwatch = Stopwatch.StartNew();
        var logEntry = await StartOperationLogAsync("LUCA_TO_KATANA_STOCK");

        try
        {
            _logger.LogInformation("Starting Luca → Katana stock sync");
            
            // Fetch from Luca
            var lucaStockDtos = await _lucaService.FetchStockMovementsAsync(fromDate);

            _logger.LogInformation("Fetched {Count} stock movements from Luca", lucaStockDtos.Count);

            var processed = lucaStockDtos.Count;
            var successful = 0;
            var errors = new List<string>();
            var toInsert = new List<Katana.Core.Entities.StockMovement>();

            foreach (var dto in lucaStockDtos)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(dto.ProductCode))
                    {
                        errors.Add($"Skipped stock movement with empty ProductCode (Ref={dto.Reference})");
                        continue;
                    }

                    var product = await _dbContext.Products.FirstOrDefaultAsync(p => p.SKU == dto.ProductCode);
                    if (product == null)
                    {
                        errors.Add($"Product not found for SKU={dto.ProductCode}. Skipping stock movement.");
                        continue;
                    }

                    // If Luca sent a BALANCE-type movement, compute the delta against current balance
                    var movementTypeNormalized = dto.MovementType?.ToUpperInvariant() ?? string.Empty;
                    int changeQuantity;
                    if (movementTypeNormalized == "BALANCE")
                    {
                        var currentBalance = await _dbContext.StockMovements
                            .Where(sm => sm.ProductId == product.Id)
                            .SumAsync(sm => (int?)sm.ChangeQuantity) ?? product.StockSnapshot;

                        changeQuantity = dto.Quantity - currentBalance;
                    }
                    else
                    {
                        changeQuantity = movementTypeNormalized == "OUT" ? -Math.Abs(dto.Quantity) : Math.Abs(dto.Quantity);
                    }

                    var movement = MappingHelper.MapFromLucaStock(dto, product.Id);
                    // Override computed change from mapping if BALANCE computed a delta
                    movement.ChangeQuantity = changeQuantity;

                    toInsert.Add(movement);
                }
                catch (Exception ex)
                {
                    errors.Add($"Error processing SKU={dto.ProductCode}: {ex.Message}");
                }
            }

            if (toInsert.Any())
            {
                await _dbContext.StockMovements.AddRangeAsync(toInsert);
                await _dbContext.SaveChangesAsync();
                successful = toInsert.Count;
            }

            stopwatch.Stop();

            await FinalizeOperationAsync(logEntry, "SUCCESS", processed, successful, processed - successful, errors.Any() ? string.Join("; ", errors) : null);

            return new SyncResultDto
            {
                SyncType = "LUCA_TO_KATANA_STOCK",
                IsSuccess = successful == processed,
                ProcessedRecords = processed,
                SuccessfulRecords = successful,
                FailedRecords = processed - successful,
                Message = errors.Any() ? "Some records were skipped or failed; check logs." : $"Luca'dan {processed} stok hareketi alındı",
                Duration = stopwatch.Elapsed,
                Errors = { }
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            await FinalizeOperationAsync(logEntry, "FAILED", 0, 0, 0, ex.Message);
            _logger.LogError(ex, "Luca → Katana stock sync failed");
            
            return new SyncResultDto
            {
                SyncType = "LUCA_TO_KATANA_STOCK",
                IsSuccess = false,
                Message = ex.Message,
                Duration = stopwatch.Elapsed,
                Errors = { ex.ToString() }
            };
        }
    }

    public async Task<SyncResultDto> SyncInvoicesFromLucaAsync(DateTime? fromDate = null)
    {
        var stopwatch = Stopwatch.StartNew();
        var logEntry = await StartOperationLogAsync("LUCA_TO_KATANA_INVOICE");

        try
        {
            _logger.LogInformation("Starting Luca → Katana invoice sync");
            
            var lucaInvoiceDtos = await _lucaService.FetchInvoicesAsync(fromDate);
            _logger.LogInformation("Fetched {Count} invoices from Luca", lucaInvoiceDtos.Count);
            
            var processed = lucaInvoiceDtos.Count;
            var successful = 0;
            var errors = new List<string>();

            foreach (var dto in lucaInvoiceDtos)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(dto.DocumentNo))
                    {
                        errors.Add("Skipped invoice with empty DocumentNo");
                        continue;
                    }

                    // Ensure customer exists (prefer TaxNo)
                    var taxNo = dto.CustomerTaxNo ?? dto.CustomerCode ?? string.Empty;
                    Customer? customer = null;
                    if (!string.IsNullOrWhiteSpace(taxNo))
                    {
                        customer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.TaxNo == taxNo);
                    }

                    if (customer == null)
                    {
                        // Create a customer from invoice header info
                        var custDto = new LucaCustomerDto
                        {
                            CustomerCode = dto.CustomerCode ?? taxNo,
                            Title = dto.CustomerTitle ?? dto.CustomerCode ?? "",
                            TaxNo = taxNo
                        };

                        var newCustomer = MappingHelper.MapFromLucaCustomer(custDto);
                        _dbContext.Customers.Add(newCustomer);
                        await _dbContext.SaveChangesAsync();
                        customer = newCustomer;
                    }

                    // Check existing invoice by invoice no
                    var existing = await _dbContext.Invoices.FirstOrDefaultAsync(i => i.InvoiceNo == dto.DocumentNo);
                    if (existing == null)
                    {
                        var invoiceEntity = MappingHelper.MapFromLucaInvoice(dto, customer.Id);
                        _dbContext.Invoices.Add(invoiceEntity);
                    }
                    else
                    {
                        // Update minimal fields
                        existing.Amount = dto.NetAmount;
                        existing.TaxAmount = dto.TaxAmount;
                        existing.TotalAmount = dto.GrossAmount;
                        existing.InvoiceDate = dto.DocumentDate;
                        existing.DueDate = dto.DueDate;
                        existing.Currency = dto.Currency ?? existing.Currency;
                        existing.UpdatedAt = DateTime.UtcNow;
                    }

                    successful++;
                }
                catch (Exception ex)
                {
                    errors.Add($"Error processing invoice {dto.DocumentNo}: {ex.Message}");
                }
            }

            if (processed > 0)
            {
                await _dbContext.SaveChangesAsync();
            }

            stopwatch.Stop();
            await FinalizeOperationAsync(logEntry, "SUCCESS", processed, successful, processed - successful, errors.Any() ? string.Join("; ", errors) : null);

            return new SyncResultDto
            {
                SyncType = "LUCA_TO_KATANA_INVOICE",
                IsSuccess = errors.Count == 0,
                ProcessedRecords = processed,
                SuccessfulRecords = successful,
                FailedRecords = errors.Count,
                Message = errors.Any() ? "Bazı faturalar atlandı veya hata aldı." : $"Luca'dan {successful} fatura alındı",
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            await FinalizeOperationAsync(logEntry, "FAILED", 0, 0, 0, ex.Message);
            _logger.LogError(ex, "Luca → Katana invoice sync failed");
            
            return new SyncResultDto
            {
                SyncType = "LUCA_TO_KATANA_INVOICE",
                IsSuccess = false,
                Message = ex.Message,
                Duration = stopwatch.Elapsed,
                Errors = { ex.ToString() }
            };
        }
    }

    public async Task<SyncResultDto> SyncDespatchFromLucaAsync(DateTime? fromDate = null)
    {
        var stopwatch = Stopwatch.StartNew();
        var logEntry = await StartOperationLogAsync("LUCA_TO_KATANA_DESPATCH");

        try
        {
            _logger.LogInformation("Starting Luca → Katana despatch (irsaliye) sync");

            var despatchDtos = await _lucaService.FetchDeliveryNotesAsync(fromDate);
            _logger.LogInformation("Fetched {Count} delivery notes from Luca", despatchDtos.Count);

            var processed = despatchDtos.Count;
            var successful = 0;
            var errors = new List<string>();

            foreach (var dto in despatchDtos)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(dto.DocumentNo))
                    {
                        errors.Add("Skipped despatch with empty DocumentNo");
                        continue;
                    }

                    // Ensure customer exists (prefer CustomerCode)
                    var customerCode = dto.CustomerCode ?? dto.CustomerTitle ?? string.Empty;
                    Customer? customer = null;
                    if (!string.IsNullOrWhiteSpace(customerCode))
                    {
                        customer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.TaxNo == customerCode || c.Title == customerCode);
                    }

                    if (customer == null)
                    {
                        var custDto = new LucaCustomerDto
                        {
                            CustomerCode = dto.CustomerCode ?? dto.CustomerTitle ?? string.Empty,
                            Title = dto.CustomerTitle ?? dto.CustomerCode ?? string.Empty,
                            TaxNo = dto.CustomerCode ?? string.Empty
                        };

                        var newCustomer = MappingHelper.MapFromLucaCustomer(custDto);
                        _dbContext.Customers.Add(newCustomer);
                        await _dbContext.SaveChangesAsync();
                        customer = newCustomer;
                    }

                    // Check existing invoice/despatch by document no
                    var existing = await _dbContext.Invoices.FirstOrDefaultAsync(i => i.InvoiceNo == dto.DocumentNo);
                    if (existing == null)
                    {
                        // Compute totals from lines
                        decimal net = 0m, tax = 0m, gross = 0m;
                        var invoiceEntity = new Invoice
                        {
                            InvoiceNo = dto.DocumentNo,
                            CustomerId = customer.Id,
                            Status = "DESPATCH",
                            InvoiceDate = dto.DocumentDate == default ? DateTime.UtcNow : dto.DocumentDate,
                            Currency = "TRY",
                            IsSynced = true,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        // Add invoice first to get its Id after SaveChanges
                        _dbContext.Invoices.Add(invoiceEntity);
                        await _dbContext.SaveChangesAsync();

                        var items = new List<InvoiceItem>();
                        foreach (var line in dto.Lines)
                        {
                            try
                            {
                                var rawSku = line.ProductCode ?? string.Empty;
                                var allowedChars = rawSku.Trim().Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray();
                                var sku = new string(allowedChars).ToUpperInvariant();
                                Product? product = null;
                                if (!string.IsNullOrWhiteSpace(sku))
                                {
                                    product = await _dbContext.Products.FirstOrDefaultAsync(p => p.SKU == sku);
                                }

                                if (product == null)
                                {
                                    // create placeholder product
                                    var p = new Product
                                    {
                                        SKU = sku,
                                        Name = line.ProductName ?? sku,
                                        Description = "Created from Luca despatch",
                                        Price = line.UnitPrice ?? 0m,
                                        IsActive = true,
                                        CreatedAt = DateTime.UtcNow,
                                        UpdatedAt = DateTime.UtcNow
                                    };
                                    _dbContext.Products.Add(p);
                                    await _dbContext.SaveChangesAsync();
                                    product = p;
                                }

                                var qty = (int)Math.Round(line.Quantity);
                                var unitPrice = line.UnitPrice ?? 0m;
                                var lineNet = unitPrice * qty;
                                var lineTax = (decimal)((line.TaxRate ?? 0.0) / 100.0) * lineNet;
                                var lineGross = lineNet + lineTax;

                                items.Add(new InvoiceItem
                                {
                                    InvoiceId = invoiceEntity.Id,
                                    ProductId = product.Id,
                                    ProductName = product.Name,
                                    ProductSKU = product.SKU,
                                    Quantity = qty,
                                    UnitPrice = unitPrice,
                                    TaxRate = (decimal?)((line.TaxRate ?? 0.0) / 100.0) ?? 0.0m,
                                    TaxAmount = lineTax,
                                    TotalAmount = lineGross,
                                    Unit = "ADET"
                                });

                                net += lineNet;
                                tax += lineTax;
                                gross += lineGross;
                            }
                            catch (Exception ex)
                            {
                                errors.Add($"Error processing line for despatch {dto.DocumentNo}: {ex.Message}");
                            }
                        }

                        if (items.Any())
                        {
                            await _dbContext.InvoiceItems.AddRangeAsync(items);
                        }

                        // Update invoice totals
                        invoiceEntity.Amount = net;
                        invoiceEntity.TaxAmount = tax;
                        invoiceEntity.TotalAmount = gross;

                        await _dbContext.SaveChangesAsync();
                    }
                    else
                    {
                        // Update existing - minimal
                        existing.UpdatedAt = DateTime.UtcNow;
                    }

                    successful++;
                }
                catch (Exception ex)
                {
                    errors.Add($"Error processing despatch {dto.DocumentNo}: {ex.Message}");
                }
            }

            stopwatch.Stop();
            await FinalizeOperationAsync(logEntry, "SUCCESS", processed, successful, processed - successful, errors.Any() ? string.Join("; ", errors) : null);

            return new SyncResultDto
            {
                SyncType = "LUCA_TO_KATANA_DESPATCH",
                IsSuccess = errors.Count == 0,
                ProcessedRecords = processed,
                SuccessfulRecords = successful,
                FailedRecords = errors.Count,
                Message = errors.Any() ? "Bazı irsaliyeler atlandı veya hata aldı." : $"Luca'dan {successful} irsaliye alındı",
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            await FinalizeOperationAsync(logEntry, "FAILED", 0, 0, 0, ex.Message);
            _logger.LogError(ex, "Luca → Katana despatch sync failed");

            return new SyncResultDto
            {
                SyncType = "LUCA_TO_KATANA_DESPATCH",
                IsSuccess = false,
                Message = ex.Message,
                Duration = stopwatch.Elapsed,
                Errors = { ex.ToString() }
            };
        }
    }

    public async Task<SyncResultDto> SyncCustomersFromLucaAsync(DateTime? fromDate = null)
    {
        var stopwatch = Stopwatch.StartNew();
        var logEntry = await StartOperationLogAsync("LUCA_TO_KATANA_CUSTOMER");

        try
        {
            _logger.LogInformation("Starting Luca → Katana customer sync");
            
            var lucaCustomerDtos = await _lucaService.FetchCustomersAsync(fromDate);
            _logger.LogInformation("Fetched {Count} customers from Luca", lucaCustomerDtos.Count);
            var processed = lucaCustomerDtos.Count;
            var successful = 0;
            var errors = new List<string>();

            foreach (var dto in lucaCustomerDtos)
            {
                try
                {
                    var taxNo = dto.TaxNo ?? dto.CustomerCode ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(taxNo))
                    {
                        errors.Add($"Skipped customer with empty TaxNo/Code (Title={dto.Title})");
                        continue;
                    }

                    var existing = await _dbContext.Customers.FirstOrDefaultAsync(c => c.TaxNo == taxNo);
                    if (existing == null)
                    {
                        var customerEntity = MappingHelper.MapFromLucaCustomer(dto);
                        _dbContext.Customers.Add(customerEntity);
                    }
                    else
                    {
                        existing.Title = dto.Title ?? existing.Title;
                        existing.Phone = dto.Phone ?? existing.Phone;
                        existing.Email = dto.Email ?? existing.Email;
                        existing.Address = dto.Address ?? existing.Address;
                        existing.City = dto.City ?? existing.City;
                        existing.Country = dto.Country ?? existing.Country;
                        existing.UpdatedAt = DateTime.UtcNow;
                    }

                    successful++;
                }
                catch (Exception ex)
                {
                    errors.Add($"Error processing customer {dto.CustomerCode ?? dto.TaxNo}: {ex.Message}");
                }
            }

            if (processed > 0)
            {
                await _dbContext.SaveChangesAsync();
            }

            stopwatch.Stop();
            await FinalizeOperationAsync(logEntry, "SUCCESS", processed, successful, processed - successful, errors.Any() ? string.Join("; ", errors) : null);

            return new SyncResultDto
            {
                SyncType = "LUCA_TO_KATANA_CUSTOMER",
                IsSuccess = errors.Count == 0,
                ProcessedRecords = processed,
                SuccessfulRecords = successful,
                FailedRecords = errors.Count,
                Message = errors.Any() ? "Bazı müşteriler atlandı veya hata aldı." : $"Luca'dan {successful} müşteri alındı",
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            await FinalizeOperationAsync(logEntry, "FAILED", 0, 0, 0, ex.Message);
            _logger.LogError(ex, "Luca → Katana customer sync failed");
            
            return new SyncResultDto
            {
                SyncType = "LUCA_TO_KATANA_CUSTOMER",
                IsSuccess = false,
                Message = ex.Message,
                Duration = stopwatch.Elapsed,
                Errors = { ex.ToString() }
            };
        }
    }

    public async Task<BatchSyncResultDto> SyncAllFromLucaAsync(DateTime? fromDate = null)
    {
        var results = new List<SyncResultDto>
        {
            await SyncCustomersFromLucaAsync(fromDate),
            await SyncStockFromLucaAsync(fromDate),
            await SyncInvoicesFromLucaAsync(fromDate),
            await SyncDespatchFromLucaAsync(fromDate),
            await SyncProductsFromLucaAsync(fromDate)
        };

        return new BatchSyncResultDto
        {
            Results = results,
            BatchTime = DateTime.UtcNow
        };
    }

    public async Task<SyncResultDto> SyncProductsFromLucaAsync(DateTime? fromDate = null)
    {
        var stopwatch = Stopwatch.StartNew();
        var logEntry = await StartOperationLogAsync("LUCA_TO_KATANA_PRODUCT");

        try
        {
            _logger.LogInformation("Starting Luca → Katana PRODUCT sync");

            var lucaProducts = await _lucaService.FetchProductsAsync(fromDate);
            _logger.LogInformation("Fetched {Count} products from Luca", lucaProducts.Count);

            var successful = 0;
            var errors = new List<string>();

            foreach (var lucaDto in lucaProducts)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(lucaDto.ProductCode))
                    {
                        errors.Add($"Skipped product with empty code (LucaId={lucaDto.SkartId})");
                        continue;
                    }

                    var sku = lucaDto.ProductCode;
                    var existing = await _dbContext.Products.FirstOrDefaultAsync(p => p.SKU == sku);

                    if (existing == null)
                    {
                        var newProduct = MappingHelper.MapFromLucaProduct(lucaDto);
                        _dbContext.Products.Add(newProduct);
                    }
                    else
                    {
                        existing.Name = lucaDto.ProductName;
                        existing.UpdatedAt = DateTime.UtcNow;
                        existing.IsActive = true;
                    }

                    successful++;
                }
                catch (Exception ex)
                {
                    errors.Add($"Error syncing product {lucaDto.ProductCode}: {ex.Message}");
                }
            }

            await _dbContext.SaveChangesAsync();
            stopwatch.Stop();

            await FinalizeOperationAsync(logEntry, "SUCCESS", lucaProducts.Count, successful, lucaProducts.Count - successful, errors.Any() ? string.Join("; ", errors) : null);

            return new SyncResultDto
            {
                SyncType = "LUCA_TO_KATANA_PRODUCT",
                IsSuccess = errors.Count == 0,
                ProcessedRecords = lucaProducts.Count,
                SuccessfulRecords = successful,
                FailedRecords = errors.Count,
                Message = errors.Any() ? "Bazı kayıtlar atlandı veya hata aldı." : $"Luca'dan {successful} ürün başarıyla aktarıldı.",
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            await FinalizeOperationAsync(logEntry, "FAILED", 0, 0, 0, ex.Message);
            _logger.LogError(ex, "Luca → Katana product sync failed");

            return new SyncResultDto
            {
                SyncType = "LUCA_TO_KATANA_PRODUCT",
                IsSuccess = false,
                Message = ex.Message,
                Duration = stopwatch.Elapsed,
                Errors = { ex.ToString() }
            };
        }
    }
}
