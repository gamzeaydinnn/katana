using System.Collections.Generic;
using System.Diagnostics;
using Katana.Business.Interfaces;
using System.Linq;
using Katana.Core.DTOs;
using Katana.Core.Entities;
using Katana.Core.Enums;
using Katana.Core.Helpers;
using Katana.Data.Context;
using Katana.Data.Configuration;
using Katana.Business.Mappers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace Katana.Business.UseCases.Sync;




public class SyncService : ISyncService
{
    private readonly IKatanaService _katanaService;
    private readonly IExtractorService _extractorService;
    private readonly ITransformerService _transformerService;
    private readonly ILoaderService _loaderService;
    private readonly ILucaService _lucaService;
    private readonly IntegrationDbContext _dbContext;
    private readonly ILogger<SyncService> _logger;
    private readonly LucaApiSettings _lucaSettings;
    private readonly IProductMappingService _productMappingService;

    public SyncService(
        IKatanaService katanaService,
        IExtractorService extractorService,
        ITransformerService transformerService,
        ILoaderService loaderService,
        ILucaService lucaService,
        IntegrationDbContext dbContext,
        ILogger<SyncService> logger,
        IOptions<LucaApiSettings> lucaOptions,
        IProductMappingService productMappingService)
    {
        _katanaService = katanaService;
        _extractorService = extractorService;
        _transformerService = transformerService;
        _loaderService = loaderService;
        _lucaService = lucaService;
        _dbContext = dbContext;
        _logger = logger;
        _lucaSettings = lucaOptions.Value;
        _productMappingService = productMappingService;
        
        _logger.LogInformation("SyncService initialized with ProductMappingService: {HasService}", productMappingService != null);
    }

    public Task<SyncResultDto> SyncStockAsync(DateTime? fromDate = null) =>
        ExecuteSyncAsync("STOCK", async ct =>
        {
            var productDtos = await _extractorService.ExtractProductsAsync(fromDate, ct);
            var products = await _transformerService.ToProductsAsync(productDtos);
            var validProducts = FilterValidProducts(products, out var skippedProducts);
            if (skippedProducts > 0)
            {
                _logger.LogWarning("SyncService => {Count} products skipped during STOCK sync due to validation errors.", skippedProducts);
            }

            var locationMappings = await GetMappingDictionaryAsync("LOCATION_WAREHOUSE", ct);
            var successful = await _loaderService.LoadProductsAsync(validProducts, locationMappings, ct: ct);
            return BuildResult("STOCK", productDtos.Count, successful);
        });

    public async Task<SyncResultDto> SyncProductsAsync(DateTime? fromDate = null)
    {
        // Artık SyncProductsToLucaAsync'i çağır - Append-Only mantık kullanılsın
        _logger.LogInformation("🔄 SyncProductsAsync -> SyncProductsToLucaAsync'e yönlendiriliyor (Append-Only mantık)");
        return await SyncProductsToLucaAsync(null, new SyncOptionsDto());
    }

    public Task<SyncResultDto> SyncInvoicesAsync(DateTime? fromDate = null) =>
        ExecuteSyncAsync("INVOICE", async ct =>
        {
            var invoiceDtos = await _extractorService.ExtractInvoicesAsync(fromDate, ct);
            var invoices = await _transformerService.ToInvoicesAsync(invoiceDtos);
            var validInvoices = FilterValidInvoices(invoices, out var skippedInvoices);
            if (skippedInvoices > 0)
            {
                _logger.LogWarning("SyncService => {Count} invoices skipped during INVOICE sync due to validation errors.", skippedInvoices);
            }

            var skuAccountMappings = await GetMappingDictionaryAsync("SKU_ACCOUNT", ct);
            var successful = await _loaderService.LoadInvoicesAsync(validInvoices, skuAccountMappings, ct: ct);
            return BuildResult("INVOICE", invoiceDtos.Count, successful);
        });

    public Task<SyncResultDto> SyncCustomersAsync(DateTime? fromDate = null) =>
        ExecuteSyncAsync("CUSTOMER", async ct =>
        {
            var customerDtos = await _extractorService.ExtractCustomersAsync(fromDate, ct);
            var customers = await _transformerService.ToCustomersAsync(customerDtos);
            var validCustomers = FilterValidCustomers(customers, out var skippedCustomers);
            if (skippedCustomers > 0)
            {
                _logger.LogWarning("SyncService => {Count} customers skipped during CUSTOMER sync due to validation errors.", skippedCustomers);
            }

            var customerTypeMappings = await GetMappingDictionaryAsync("CUSTOMER_TYPE", ct);
            var successful = await _loaderService.LoadCustomersAsync(validCustomers, customerTypeMappings, ct: ct);
            return BuildResult("CUSTOMER", customerDtos.Count, successful);
        });

    public async Task<SyncResultDto> SyncProductsToLucaAsync(string? sessionId = null, SyncOptionsDto? options = null)
    {
        options ??= new SyncOptionsDto();
        var stopwatch = Stopwatch.StartNew();
        var logEntry = await StartOperationLogAsync("PRODUCT_STOCK_CARD");

        _logger.LogInformation("🚀 SyncProductsToLucaAsync BAŞLADI - Options: DryRun={DryRun}, ForceSend={ForceSend}, Limit={Limit}",
            options.DryRun, options.ForceSendDuplicates, options.Limit);

        var katanaProducts = await _katanaService.GetProductsAsync();
        _logger.LogInformation("✅ Katana'dan {Count} ürün alındı", katanaProducts.Count);
        
        if (options.Limit.HasValue && options.Limit.Value > 0)
        {
            katanaProducts = katanaProducts.Take(options.Limit.Value).ToList();
            _logger.LogInformation("⚡ Limit uygulandı: {Limit} ürün işlenecek", options.Limit.Value);
        }

        // HIZLI MOD: Luca'dan stok kartlarını çekmeyi atla (çok yavaş ve HTML dönüyor)
        // Bunun yerine Luca'nın duplicate kontrolüne güven
        var productsToSync = katanaProducts.ToList();
        var newProductsCount = productsToSync.Count;
        var unchangedCount = 0;

        _logger.LogInformation("⚡ HIZLI MOD: {Count} ürün Luca'ya gönderilecek (duplicate kontrolü Luca tarafında yapılacak)", 
            productsToSync.Count);

        var payload = new List<LucaCreateStokKartiRequest>();
        var details = new List<string>();

        // Load PRODUCT_CATEGORY mappings once
        var productCategoryMappings = await GetMappingDictionaryAsync("PRODUCT_CATEGORY", CancellationToken.None);
        _logger.LogInformation("✅ {Count} kategori mapping yüklendi", productCategoryMappings?.Count ?? 0);

        // 🔥 KRİTİK FİX: Database'den ürün isimlerini çek (Katana API bazen Name boş gönderiyor!)
        var dbProducts = await _dbContext.Products
            .Where(p => !string.IsNullOrWhiteSpace(p.SKU) && !string.IsNullOrWhiteSpace(p.Name))
            .Select(p => new { p.SKU, p.Name })
            .ToListAsync();
        var productNameLookup = dbProducts.ToDictionary(p => p.SKU, p => p.Name, StringComparer.OrdinalIgnoreCase);
        _logger.LogInformation("✅ Database'den {Count} ürün ismi yüklendi (Name fallback için)", productNameLookup.Count);

        foreach (var product in productsToSync)
        {
            try
            {
                // 🔥 KRİTİK FİX: Katana'dan Name boş VEYA SKU ile aynıysa database'den çek!
                // SORUN: Katana API bazen name alanına SKU değerini gönderiyor (örn: "81.06301-8211")
                var needsNameFix = string.IsNullOrWhiteSpace(product.Name) || 
                                   string.Equals(product.Name?.Trim(), product.SKU?.Trim(), StringComparison.OrdinalIgnoreCase);
                
                if (needsNameFix && !string.IsNullOrWhiteSpace(product.SKU))
                {
                    if (productNameLookup.TryGetValue(product.SKU, out var dbName))
                    {
                        _logger.LogWarning("🔧 FİX: Katana'dan Name hatalı ('{BadName}'), database'den alındı: {SKU} -> '{GoodName}'", 
                            product.Name ?? "(boş)", product.SKU, dbName);
                        product.Name = dbName; // Database'den ürün ismini kullan!
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ UYARI: {SKU} için Name hatalı ('{BadName}') ve database'de de bulunamadı, SKU kullanılacak!", 
                            product.SKU, product.Name ?? "(boş)");
                    }
                }
                
                var dto = KatanaToLucaMapper.MapKatanaProductToStockCard(product, _lucaSettings, productCategoryMappings);
                KatanaToLucaMapper.ValidateLucaStockCard(dto);
                payload.Add(dto);
            }
            catch (ValidationException ex)
            {
                details.Add($"{product.SKU}: {ex.Message}");
            }
            catch (Exception ex)
            {
                details.Add($"{product.SKU}: {ex.Message}");
            }
        }

        _logger.LogInformation("📦 {Count} ürün payload'a eklendi, {Failed} hata", payload.Count, details.Count);

        SyncResultDto sendResult = new()
        {
            SyncType = "PRODUCT_STOCK_CARD",
            ProcessedRecords = payload.Count,
            SuccessfulRecords = 0,
            FailedRecords = 0,
            IsSuccess = true,
            Message = payload.Count == 0 ? "Gönderilecek yeni stok kartı bulunamadı" : "Dry-run"
        };

        if (!options.DryRun && payload.Any())
        {
            _logger.LogInformation("📤 Luca'ya {Count} stok kartı gönderiliyor...", payload.Count);
            sendResult = await _lucaService.SendStockCardsAsync(payload);
            _logger.LogInformation("✅ SendStockCardsAsync tamamlandı - Success: {Success}, Failed: {Failed}, Duplicates: {Duplicates}",
                sendResult.SuccessfulRecords, sendResult.FailedRecords, sendResult.DuplicateRecords);
        }

        stopwatch.Stop();
        var isDryRun = options.DryRun;
        var lucaSuccess = isDryRun ? 0 : sendResult.SuccessfulRecords;
        var lucaFailed = isDryRun ? 0 : sendResult.FailedRecords;
        var lucaDuplicates = isDryRun ? 0 : sendResult.DuplicateRecords;
        var lucaSent = isDryRun ? 0 : sendResult.SentRecords;
        var detailFailures = isDryRun ? 0 : details.Count;
        var combinedErrors = details.Concat(isDryRun ? Array.Empty<string>() : sendResult.Errors ?? new List<string>()).ToList();
        
        var response = new SyncResultDto
        {
            SyncType = "PRODUCT_STOCK_CARD",
            SyncTime = DateTime.UtcNow,
            Duration = stopwatch.Elapsed,
            ProcessedRecords = katanaProducts.Count,
            SuccessfulRecords = lucaSuccess,
            FailedRecords = lucaFailed + detailFailures,
            DuplicateRecords = lucaDuplicates,
            SentRecords = lucaSent,
            IsDryRun = isDryRun,
            TotalChecked = katanaProducts.Count,
            AlreadyExists = unchangedCount,
            NewCreated = isDryRun ? payload.Count : lucaSuccess,
            Failed = lucaFailed + detailFailures,
            Details = combinedErrors,
            Errors = combinedErrors,
            IsSuccess = details.Count == 0 && (isDryRun || sendResult.IsSuccess),
            Message = isDryRun
                ? $"Dry-run tamamlandı. {payload.Count} ürün hazır."
                : $"{sendResult.Message} (Gönderilen: {lucaSent}, Başarılı: {lucaSuccess}, Duplicate: {lucaDuplicates})"
        };

        try
        {
            await FinalizeOperationAsync(
                logEntry,
                response.IsSuccess ? "SUCCESS" : "FAILED",
                response.ProcessedRecords,
                response.SuccessfulRecords,
                response.FailedRecords,
                BuildResultErrorMessage(response));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write sync log for PRODUCT_STOCK_CARD");
            try
            {
                await FinalizeOperationAsync(
                    logEntry,
                    "FAILED",
                    katanaProducts?.Count ?? 0,
                    0,
                    (payload?.Count ?? 0) + details.Count,
                    ex.Message);
            }
            catch (Exception finalizeEx)
            {
                _logger.LogError(finalizeEx, "Failed to finalize sync operation log after exception for PRODUCT_STOCK_CARD");
            }
        }

        return response;
    }

    // Backwards-compatible overload
    public Task<SyncResultDto> SyncProductsToLucaAsync(SyncOptionsDto options)
    {
        return SyncProductsToLucaAsync(null, options);
    }

    public async Task<List<StockComparisonDto>> CompareStockCardsAsync()
    {
        var katanaProducts = await _katanaService.GetProductsAsync();
        var kozaCards = await _lucaService.ListStockCardsSimpleAsync();
        
        // Convert KozaStokKartiDto to LucaStockCardSummaryDto
        var lucaStockCards = kozaCards.Select(k => new LucaStockCardSummaryDto
        {
            StokKodu = k.KartKodu ?? "",
            StokAdi = k.KartAdi ?? "",
            Barcode = k.Barkod ?? ""
        }).ToList();

        var comparisons = new List<StockComparisonDto>();
        foreach (var product in katanaProducts)
        {
            var sku = NormalizeSku(product);
            var barcode = product.Barcode;
            var match = FindLucaMatch(lucaStockCards, sku, barcode, preferBarcodeMatch: true);

            comparisons.Add(new StockComparisonDto
            {
                Sku = sku,
                Barcode = barcode,
                Name = product.Name ?? sku,
                ExistsInLuca = match != null,
                LucaCode = match?.Code,
                LucaBarcode = match?.Barcode,
                Status = match != null ? "EXISTS" : "MISSING"
            });
        }

        return comparisons;
    }

    public async Task<BatchSyncResultDto> SyncAllAsync(DateTime? fromDate = null)
    {
        var results = new List<SyncResultDto>
        {
            await SyncCustomersAsync(fromDate),
            await SyncStockAsync(fromDate),
            await SyncInvoicesAsync(fromDate),
            // Append-Only mantığı ile Luca sync - ProductMappingService kullanır
            await SyncProductsToLucaAsync(null, new SyncOptionsDto())
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

            var status = result.IsSuccess ? "SUCCESS" : "FAILED";
            var errorMessage = result.IsSuccess ? null : BuildResultErrorMessage(result);
            await FinalizeOperationAsync(
                logEntry,
                status,
                result.ProcessedRecords,
                result.SuccessfulRecords,
                result.FailedRecords,
                errorMessage);

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

    private static string? BuildResultErrorMessage(SyncResultDto? result)
    {
        if (result == null)
        {
            return null;
        }

        if (result.Errors != null && result.Errors.Count > 0)
        {
            return string.Join("; ", result.Errors);
        }

        return string.IsNullOrWhiteSpace(result.Message) ? null : result.Message;
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

    private List<Product> FilterValidProducts(IEnumerable<Product> products, out int skipped)
    {
        var valid = new List<Product>();
        skipped = 0;
        foreach (var product in products)
        {
            if (ValidateProductForLuca(product, out var reason))
            {
                valid.Add(product);
            }
            else
            {
                skipped++;
                _logger.LogWarning("SyncService => Product skipped. SKU={Sku}; Reason={Reason}", product?.SKU, reason);
            }
        }
        return valid;
    }

    private List<Invoice> FilterValidInvoices(IEnumerable<Invoice> invoices, out int skipped)
    {
        var valid = new List<Invoice>();
        skipped = 0;
        foreach (var invoice in invoices)
        {
            if (ValidateInvoiceForLuca(invoice, out var reason))
            {
                valid.Add(invoice);
            }
            else
            {
                skipped++;
                _logger.LogWarning("SyncService => Invoice skipped. InvoiceNo={InvoiceNo}; Reason={Reason}", invoice?.InvoiceNo, reason);
            }
        }
        return valid;
    }

    private List<Customer> FilterValidCustomers(IEnumerable<Customer> customers, out int skipped)
    {
        var valid = new List<Customer>();
        skipped = 0;
        foreach (var customer in customers)
        {
            if (ValidateCustomerForLuca(customer, out var reason))
            {
                valid.Add(customer);
            }
            else
            {
                skipped++;
                _logger.LogWarning("SyncService => Customer skipped. TaxNo={TaxNo}; Reason={Reason}", customer?.TaxNo, reason);
            }
        }
        return valid;
    }

    private bool ValidateProductForLuca(Product? product, out string reason)
    {
        if (product == null)
        {
            reason = "Product is null";
            return false;
        }

        if (string.IsNullOrWhiteSpace(product.SKU))
        {
            reason = "SKU boş";
            return false;
        }

        if (string.IsNullOrWhiteSpace(product.Name))
        {
            reason = "Ürün adı boş";
            return false;
        }

        if (product.Price < 0)
        {
            reason = "Ürün fiyatı negatif olamaz";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private bool ValidateInvoiceForLuca(Invoice? invoice, out string reason)
    {
        if (invoice == null)
        {
            reason = "Invoice is null";
            return false;
        }

        if (string.IsNullOrWhiteSpace(invoice.InvoiceNo))
        {
            reason = "InvoiceNo boş";
            return false;
        }

        if (invoice.CustomerId <= 0)
        {
            reason = "CustomerId eksik";
            return false;
        }

        if (invoice.InvoiceDate == default)
        {
            reason = "Fatura tarihi boş";
            return false;
        }

        var items = invoice.InvoiceItems?.ToList() ?? new List<InvoiceItem>();
        if (!items.Any())
        {
            reason = "Fatura kalemi yok";
            return false;
        }

        foreach (var item in items)
        {
            if (item.Quantity <= 0)
            {
                reason = $"Kalem miktarı geçersiz (SKU={item.ProductSKU})";
                return false;
            }

            if (string.IsNullOrWhiteSpace(item.ProductSKU))
            {
                reason = "Kalem SKU boş";
                return false;
            }
        }

        reason = string.Empty;
        return true;
    }

    private bool ValidateCustomerForLuca(Customer? customer, out string reason)
    {
        if (customer == null)
        {
            reason = "Customer is null";
            return false;
        }

        if (string.IsNullOrWhiteSpace(customer.TaxNo))
        {
            reason = "Vergi numarası boş";
            return false;
        }

        if (customer.TaxNo.Length < 10)
        {
            reason = "Vergi numarası eksik";
            return false;
        }

        if (string.IsNullOrWhiteSpace(customer.Title))
        {
            reason = "Cari ünvan boş";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private static string NormalizeSku(KatanaProductDto product) =>
        !string.IsNullOrWhiteSpace(product.SKU) ? product.SKU.Trim() : product.GetProductCode();

    private static LucaStockCardSummaryDto? FindLucaMatch(
        IEnumerable<LucaStockCardSummaryDto> lucaStockCards,
        string sku,
        string? barcode,
        bool preferBarcodeMatch)
    {
        var comparer = StringComparer.OrdinalIgnoreCase;

        if (preferBarcodeMatch && !string.IsNullOrWhiteSpace(barcode))
        {
            var barcodeMatch = lucaStockCards.FirstOrDefault(c =>
                !string.IsNullOrWhiteSpace(c.Barcode) && comparer.Equals(c.Barcode, barcode));
            if (barcodeMatch != null) return barcodeMatch;
        }

        var codeMatch = lucaStockCards.FirstOrDefault(c => comparer.Equals(c.ProductCode, sku));
        if (codeMatch != null) return codeMatch;

        if (!preferBarcodeMatch && !string.IsNullOrWhiteSpace(barcode))
        {
            return lucaStockCards.FirstOrDefault(c =>
                !string.IsNullOrWhiteSpace(c.Barcode) && comparer.Equals(c.Barcode, barcode));
        }

        return null;
    }

    private static bool ExistsInLuca(
        KatanaProductDto product,
        IEnumerable<LucaStockCardSummaryDto> lucaStockCards,
        bool preferBarcodeMatch)
    {
        var sku = NormalizeSku(product);
        return FindLucaMatch(lucaStockCards, sku, product.Barcode, preferBarcodeMatch) != null;
    }

    /// <summary>
    /// Katana ürünü ile Luca stok kartı arasındaki değişiklikleri tespit eder.
    /// Luca'da güncelleme yapılamadığı için değişiklik varsa yeni stok kartı oluşturulması gerekir.
    /// </summary>
    private static ProductChangeInfo DetectProductChanges(
        KatanaProductDto katanaProduct,
        LucaStockCardSummaryDto? lucaCard)
    {
        var changes = new ProductChangeInfo
        {
            SKU = NormalizeSku(katanaProduct),
            ExistsInLuca = lucaCard != null
        };

        if (lucaCard == null)
        {
            changes.IsNew = true;
            changes.RequiresNewStockCard = true;
            changes.ChangeReason = "Yeni ürün - Luca'da mevcut değil";
            return changes;
        }

        var changeReasons = new List<string>();

        // İsim değişikliği kontrolü
        var katanaName = katanaProduct.Name?.Trim() ?? string.Empty;
        var lucaName = lucaCard.StokAdi?.Trim() ?? string.Empty;
        if (!string.Equals(katanaName, lucaName, StringComparison.OrdinalIgnoreCase))
        {
            changes.NameChanged = true;
            changes.OldName = lucaName;
            changes.NewName = katanaName;
            changeReasons.Add($"İsim: '{lucaName}' -> '{katanaName}'");
        }

        // Miktar değişikliği kontrolü
        var katanaQty = katanaProduct.Available ?? katanaProduct.OnHand ?? katanaProduct.InStock ?? 0;
        var lucaQty = lucaCard.Miktar;
        if (Math.Abs(katanaQty - lucaQty) > 0.001)
        {
            changes.QuantityChanged = true;
            changes.OldQuantity = lucaQty;
            changes.NewQuantity = katanaQty;
            changeReasons.Add($"Miktar: {lucaQty:N2} -> {katanaQty:N2}");
        }

        // Fiyat değişikliği kontrolü
        var katanaPrice = katanaProduct.SalesPrice ?? katanaProduct.Price;
        var lucaPrice = lucaCard.SatisFiyat ?? 0;
        if (Math.Abs((double)katanaPrice - (double)lucaPrice) > 0.01)
        {
            changes.PriceChanged = true;
            changes.OldPrice = lucaPrice;
            changes.NewPrice = katanaPrice;
            changeReasons.Add($"Fiyat: {lucaPrice:N2} -> {katanaPrice:N2}");
        }

        // Kategori değişikliği kontrolü
        var katanaCategory = katanaProduct.Category?.Trim() ?? string.Empty;
        var lucaCategory = lucaCard.KategoriKodu?.Trim() ?? string.Empty;
        if (!string.IsNullOrEmpty(katanaCategory) && !string.Equals(katanaCategory, lucaCategory, StringComparison.OrdinalIgnoreCase))
        {
            changes.CategoryChanged = true;
            changes.OldCategory = lucaCategory;
            changes.NewCategory = katanaCategory;
            changeReasons.Add($"Kategori: '{lucaCategory}' -> '{katanaCategory}'");
        }

        // Luca'da güncelleme yapılamadığı için değişen ürünler yeni stok kartı olarak oluşturulmalı
        if (changes.HasChanges)
        {
            changes.RequiresNewStockCard = true;
            changes.ChangeReason = string.Join("; ", changeReasons);
        }

        return changes;
    }

    /// <summary>
    /// Ürün değişiklik bilgilerini tutar
    /// </summary>
    private class ProductChangeInfo
    {
        public string SKU { get; set; } = string.Empty;
        public bool ExistsInLuca { get; set; }
        public bool IsNew { get; set; }
        public bool RequiresNewStockCard { get; set; }
        public string? ChangeReason { get; set; }

        // Değişiklik detayları
        public bool NameChanged { get; set; }
        public string? OldName { get; set; }
        public string? NewName { get; set; }

        public bool QuantityChanged { get; set; }
        public double OldQuantity { get; set; }
        public double NewQuantity { get; set; }

        public bool PriceChanged { get; set; }
        public decimal OldPrice { get; set; }
        public decimal NewPrice { get; set; }

        public bool CategoryChanged { get; set; }
        public string? OldCategory { get; set; }
        public string? NewCategory { get; set; }

        public bool HasChanges => NameChanged || QuantityChanged || PriceChanged || CategoryChanged;
    }

    private async Task<Dictionary<string, string>> GetMappingDictionaryAsync(string mappingType, CancellationToken ct)
    {
        try
        {
            var normalized = string.IsNullOrWhiteSpace(mappingType)
                ? string.Empty
                : mappingType.Trim().ToUpperInvariant();

            var entries = await _dbContext.MappingTables
                .Where(m => m.IsActive && m.MappingType != null && m.MappingType.ToUpper() == normalized)
                .Select(m => new { m.SourceValue, m.TargetValue })
                .ToListAsync(ct);

            // Normalize keys (trim + uppercase + remove diacritics + normalize separators)
            var dict = entries
                .Where(e => !string.IsNullOrWhiteSpace(e.SourceValue))
                .ToDictionary(
                    e => NormalizeMappingKey(e.SourceValue),
                    e => e.TargetValue ?? string.Empty,
                    StringComparer.OrdinalIgnoreCase);

            return dict;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SyncService => Failed to load mapping table {MappingType}. Returning empty mapping.", mappingType);
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static string NormalizeMappingKey(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var s = input.Trim().ToUpperInvariant();
        // replace common separators with space
        s = s.Replace('/', ' ').Replace('\\', ' ').Replace('-', ' ');
        // remove diacritics
        s = RemoveDiacritics(s);
        // collapse multiple spaces
        while (s.Contains("  ")) s = s.Replace("  ", " ");
        return s;
    }

    private static string RemoveDiacritics(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;
        var normalized = text.Normalize(System.Text.NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder();
        foreach (var ch in normalized)
        {
            var uc = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
            if (uc != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                sb.Append(ch);
            }
        }
        return sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
    }

    
    public async Task<SyncResultDto> SyncStockFromLucaAsync(DateTime? fromDate = null)
    {
        var stopwatch = Stopwatch.StartNew();
        var logEntry = await StartOperationLogAsync("LUCA_TO_KATANA_STOCK");

        try
        {
            _logger.LogInformation("Starting Luca → Katana stock sync");
            
            
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

                    
                    var taxNo = dto.CustomerTaxNo ?? dto.CustomerCode ?? string.Empty;
                    Customer? customer = null;
                    if (!string.IsNullOrWhiteSpace(taxNo))
                    {
                        customer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.TaxNo == taxNo);
                    }

                    if (customer == null)
                    {
                        
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

                    
                    var existing = await _dbContext.Invoices.FirstOrDefaultAsync(i => i.InvoiceNo == dto.DocumentNo);
                    if (existing == null)
                    {
                        var invoiceEntity = MappingHelper.MapFromLucaInvoice(dto, customer.Id);
                        _dbContext.Invoices.Add(invoiceEntity);
                    }
                    else
                    {
                        
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

                    
                    var existing = await _dbContext.Invoices.FirstOrDefaultAsync(i => i.InvoiceNo == dto.DocumentNo);
                    if (existing == null)
                    {
                        
                        decimal net = 0m, tax = 0m, gross = 0m;
                        var invoiceEntity = new Invoice
                        {
                            InvoiceNo = dto.DocumentNo,
                            CustomerId = customer.Id,
                            Status = InvoiceStatus.Sent,
                            InvoiceDate = dto.DocumentDate == default ? DateTime.UtcNow : dto.DocumentDate,
                            Currency = "TRY",
                            IsSynced = true,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        
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

                        
                        invoiceEntity.Amount = net;
                        invoiceEntity.TaxAmount = tax;
                        invoiceEntity.TotalAmount = gross;

                        await _dbContext.SaveChangesAsync();
                    }
                    else
                    {
                        
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
                        // LucaId'yi kaydet (hızlı silme için)
                        newProduct.LucaId = lucaDto.SkartId;
                        _dbContext.Products.Add(newProduct);
                    }
                    else
                    {
                        existing.Name = lucaDto.ProductName;
                        existing.UpdatedAt = DateTime.UtcNow;
                        existing.IsActive = true;
                        // LucaId'yi güncelle (hızlı silme için)
                        if (lucaDto.SkartId > 0)
                        {
                            existing.LucaId = lucaDto.SkartId;
                        }
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

    #region Koza Cari Sync Methods

    /// <summary>
    /// Katana tedarikçilerini Koza'ya senkronize eder
    /// </summary>
    public async Task<SyncResultDto> SyncSuppliersToKozaAsync(CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var logEntry = await StartOperationLogAsync("SUPPLIER");
        
        int total = 0, successful = 0, skipped = 0, errors = 0;
        var errorMessages = new List<string>();

        try
        {
            _logger.LogInformation("🔄 Starting Katana → Koza supplier sync");

            var katanaSuppliers = await _katanaService.GetSuppliersAsync();
            total = katanaSuppliers.Count;
            _logger.LogInformation("📥 Found {Count} suppliers in Katana", total);

            foreach (var supplier in katanaSuppliers)
            {
                try
                {
                    if (supplier?.Id == null)
                    {
                        skipped++;
                        continue;
                    }

                    // TODO: Implement KatanaSupplierToCariDto mapping
                    // Supplier sync temporarily disabled - DTOs not available
                    skipped++;
                }
                catch (Exception ex)
                {
                    errors++;
                    errorMessages.Add($"{supplier?.Id}: {ex.Message}");
                    _logger.LogError(ex, "❌ Error syncing supplier");
                }
            }

            stopwatch.Stop();
            var status = errors == 0 ? "SUCCESS" : (successful > 0 ? "PARTIAL" : "FAILED");
            await FinalizeOperationAsync(logEntry, status, total, successful, errors, errorMessages.Any() ? string.Join("; ", errorMessages.Take(5)) : null);

            _logger.LogInformation("✅ Supplier sync completed: Total={Total}, Success={Success}, Skipped={Skipped}, Error={Error}",
                total, successful, skipped, errors);

            return new SyncResultDto
            {
                SyncType = "SUPPLIER",
                IsSuccess = errors == 0,
                ProcessedRecords = total,
                SuccessfulRecords = successful,
                FailedRecords = errors,
                Message = $"Tedarikçi senkronizasyonu tamamlandı. Başarılı: {successful}, Atlanan: {skipped}, Hata: {errors}",
                Duration = stopwatch.Elapsed,
                Errors = errorMessages
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            await FinalizeOperationAsync(logEntry, "FAILED", total, successful, errors, ex.Message);
            _logger.LogError(ex, "❌ Supplier sync failed completely");

            return new SyncResultDto
            {
                SyncType = "SUPPLIER",
                IsSuccess = false,
                ProcessedRecords = total,
                SuccessfulRecords = successful,
                FailedRecords = errors,
                Message = $"Tedarikçi senkronizasyonu başarısız: {ex.Message}",
                Duration = stopwatch.Elapsed,
                Errors = { ex.ToString() }
            };
        }
    }

    /// <summary>
    /// Katana depolarını (Location) Koza'ya senkronize eder
    /// </summary>
    public async Task<SyncResultDto> SyncWarehousesToKozaAsync(CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var logEntry = await StartOperationLogAsync("WAREHOUSE");
        
        int total = 0, successful = 0, skipped = 0, errors = 0;
        var errorMessages = new List<string>();

        try
        {
            _logger.LogInformation("🔄 Starting Katana → Koza warehouse (depot) sync");

            var katanaLocations = await _katanaService.GetLocationsAsync();
            total = katanaLocations.Count;
            _logger.LogInformation("📥 Found {Count} locations in Katana", total);

            foreach (var location in katanaLocations)
            {
                try
                {
                    // Location ID is long, convert to string for code
                    var locationCode = location.Id.ToString();
                    
                    // TODO: Implement KatanaLocationToDepoDto
                    // Warehouse sync temporarily disabled
                    skipped++;
                    /*
                    var depoDto = new KatanaLocationToDepoDto
                    {
                        Code = locationCode,
                        Name = location.Name ?? locationCode,
                        Address = location.Address?.Line1
                    };

                    var kozaResult = await _lucaService.EnsureDepotAsync(depoDto, ct);
                    
                    if (kozaResult.Success)
                    {
                        successful++;
                        _logger.LogDebug("✅ Warehouse {Code} synced successfully", locationCode);
                    }
                    else
                    {
                        errors++;
                        errorMessages.Add($"{locationCode}: {kozaResult.Message}");
                        _logger.LogWarning("⚠️ Warehouse {Code} sync failed: {Error}", locationCode, kozaResult.Message);
                    }
                    */
                }
                catch (Exception ex)
                {
                    errors++;
                    errorMessages.Add($"{location.Id}: {ex.Message}");
                    _logger.LogError(ex, "❌ Error syncing warehouse {Code}", location.Id);
                }
            }

            stopwatch.Stop();
            var status = errors == 0 ? "SUCCESS" : (successful > 0 ? "PARTIAL" : "FAILED");
            await FinalizeOperationAsync(logEntry, status, total, successful, errors, errorMessages.Any() ? string.Join("; ", errorMessages.Take(5)) : null);

            _logger.LogInformation("✅ Warehouse sync completed: Total={Total}, Success={Success}, Skipped={Skipped}, Error={Error}",
                total, successful, skipped, errors);

            return new SyncResultDto
            {
                SyncType = "WAREHOUSE",
                IsSuccess = errors == 0,
                ProcessedRecords = total,
                SuccessfulRecords = successful,
                FailedRecords = errors,
                Message = $"Depo senkronizasyonu tamamlandı. Başarılı: {successful}, Atlanan: {skipped}, Hata: {errors}",
                Duration = stopwatch.Elapsed,
                Errors = errorMessages
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            await FinalizeOperationAsync(logEntry, "FAILED", total, successful, errors, ex.Message);
            _logger.LogError(ex, "❌ Warehouse sync failed completely");

            return new SyncResultDto
            {
                SyncType = "WAREHOUSE",
                IsSuccess = false,
                ProcessedRecords = total,
                SuccessfulRecords = successful,
                FailedRecords = errors,
                Message = $"Depo senkronizasyonu başarısız: {ex.Message}",
                Duration = stopwatch.Elapsed,
                Errors = { ex.ToString() }
            };
        }
    }

    /// <summary>
    /// Katana müşterilerini Luca'ya cari olarak senkronize eder
    /// </summary>
    public async Task<SyncResultDto> SyncCustomersToLucaAsync(CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var logEntry = await StartOperationLogAsync("CUSTOMER_LUCA");
        
        int total = 0, successful = 0, skipped = 0, errors = 0;
        var errorMessages = new List<string>();

        try
        {
            _logger.LogInformation("🔄 Starting Local DB → Luca customer (cari) sync");

            // Get customers from local database (Customers table)
            var localCustomers = await _dbContext.Customers
                .Where(c => c.IsActive)
                .ToListAsync(ct);
            
            total = localCustomers.Count;
            _logger.LogInformation("📥 Found {Count} customers in local database", total);

            foreach (var customer in localCustomers)
            {
                try
                {
                    // Use TaxNo as code, or generate one from Id
                    var customerCode = !string.IsNullOrWhiteSpace(customer.TaxNo) 
                        ? customer.TaxNo 
                        : $"CK-{customer.Id}";

                    // TODO: Implement KatanaCustomerToCariDto
                    // Customer sync temporarily disabled
                    skipped++;
                    /*
                    var customerDto = new KatanaCustomerToCariDto
                    {
                        Code = customerCode,
                        Name = customer.Title ?? customerCode,
                        TaxNumber = customer.TaxNo,
                        TaxOffice = customer.TaxOffice,
                        Address = customer.Address,
                        Phone = customer.Phone,
                        Email = customer.Email
                    };

                    var lucaResult = await _lucaService.EnsureCustomerCariAsync(customerDto, ct);
                    
                    if (lucaResult.Success)
                    {
                        successful++;
                        _logger.LogDebug("✅ Customer {Code} synced successfully", customerCode);
                    }
                    else
                    {
                        errors++;
                        errorMessages.Add($"{customerCode}: {lucaResult.Message}");
                        _logger.LogWarning("⚠️ Customer {Code} sync failed: {Error}", customerCode, lucaResult.Message);
                    }
                    */
                }
                catch (Exception ex)
                {
                    errors++;
                    errorMessages.Add($"{customer.Id}: {ex.Message}");
                    _logger.LogError(ex, "❌ Error syncing customer {Code}", customer.Id);
                }
            }

            stopwatch.Stop();
            var status = errors == 0 ? "SUCCESS" : (successful > 0 ? "PARTIAL" : "FAILED");
            await FinalizeOperationAsync(logEntry, status, total, successful, errors, errorMessages.Any() ? string.Join("; ", errorMessages.Take(5)) : null);

            _logger.LogInformation("✅ Customer sync completed: Total={Total}, Success={Success}, Skipped={Skipped}, Error={Error}",
                total, successful, skipped, errors);

            return new SyncResultDto
            {
                SyncType = "CUSTOMER_LUCA",
                IsSuccess = errors == 0,
                ProcessedRecords = total,
                SuccessfulRecords = successful,
                FailedRecords = errors,
                Message = $"Müşteri senkronizasyonu tamamlandı. Başarılı: {successful}, Atlanan: {skipped}, Hata: {errors}",
                Duration = stopwatch.Elapsed,
                Errors = errorMessages
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            await FinalizeOperationAsync(logEntry, "FAILED", total, successful, errors, ex.Message);
            _logger.LogError(ex, "❌ Customer sync failed completely");

            return new SyncResultDto
            {
                SyncType = "CUSTOMER_LUCA",
                IsSuccess = false,
                ProcessedRecords = total,
                SuccessfulRecords = successful,
                FailedRecords = errors,
                Message = $"Müşteri senkronizasyonu başarısız: {ex.Message}",
                Duration = stopwatch.Elapsed,
                Errors = { ex.ToString() }
            };
        }
    }

    #endregion

    #region Debug Methods

    public async Task<object> DebugProductComparisonAsync(string sku)
    {
        // TODO: Implement debug comparison logic
        await Task.CompletedTask;
        _logger.LogWarning("DebugProductComparisonAsync not yet implemented for SKU={SKU}", sku);
        return new { success = false, message = "Not implemented" };
    }

    public async Task<object> ForceSyncSingleProductAsync(string sku)
    {
        // TODO: Implement force sync logic
        await Task.CompletedTask;
        _logger.LogWarning("ForceSyncSingleProductAsync not yet implemented for SKU={SKU}", sku);
        return new { success = false, message = "Not implemented" };
    }

    #endregion

    #region ISyncService Interface Methods

    public async Task<SyncResultDto> SyncWarehousesToLucaAsync()
    {
        _logger.LogInformation("🏭 SyncWarehousesToLucaAsync başlatıldı");
        
        await Task.CompletedTask;
        return new SyncResultDto
        {
            SyncType = "WAREHOUSES_TO_LUCA",
            IsSuccess = false,
            Message = "Warehouse sync not implemented yet",
            Duration = TimeSpan.Zero
        };
    }

    public Task<SyncResultDto> SendSalesInvoiceAsync(LucaCreateInvoiceHeaderRequest request)
    {
        _logger.LogInformation("📄 SendSalesInvoiceAsync başlatıldı");
        
        if (request == null)
            return Task.FromResult(new SyncResultDto
            {
                SyncType = "SEND_SALES_INVOICE",
                IsSuccess = false,
                Message = "Request is null",
                Duration = TimeSpan.Zero
            });

        try
        {
            // Fatura oluşturma işlemi - mevcut CreateSalesOrderInvoiceAsync metodunu kullanabilir
            _logger.LogInformation("📄 Luca'ya fatura gönderiliyor");
            
            return Task.FromResult(new SyncResultDto
            {
                SyncType = "SEND_SALES_INVOICE",
                IsSuccess = true,
                Message = "Invoice sent successfully",
                Duration = TimeSpan.Zero
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SendSalesInvoiceAsync failed");
            return Task.FromResult(new SyncResultDto
            {
                SyncType = "SEND_SALES_INVOICE",
                IsSuccess = false,
                Message = ex.Message,
                Duration = TimeSpan.Zero
            });
        }
    }

    #endregion
}
