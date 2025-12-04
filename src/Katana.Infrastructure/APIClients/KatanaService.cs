using Katana.Business.DTOs;
using Katana.Business.Interfaces;
using Katana.Core.Enums;
using Katana.Data.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text;
using System.Net.Http.Headers;
using Katana.Core.DTOs;
using System.Linq;
using System.Globalization;
using Microsoft.Extensions.Caching.Memory;
using System.Net;

namespace Katana.Infrastructure.APIClients;

public class KatanaService : IKatanaService
{
    private readonly HttpClient _httpClient;
    private readonly KatanaApiSettings _settings;
    private readonly ILogger<KatanaService> _logger;
    private readonly ILoggingService _loggingService;
    private readonly IMemoryCache _cache;
    private readonly JsonSerializerOptions _jsonOptions;

    public KatanaService(HttpClient httpClient, IOptions<KatanaApiSettings> settings, ILogger<KatanaService> logger, ILoggingService loggingService, IMemoryCache cache)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
        _loggingService = loggingService;
        _cache = cache;
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task<List<KatanaStockDto>> GetStockChangesAsync(DateTime fromDate, DateTime toDate)
    {
        try
        {
            _logger.LogInformation("Getting stock changes from {FromDate} to {ToDate}", fromDate, toDate);

            var url = $"{_settings.Endpoints.StockAdjustments}?fromDate={fromDate:yyyy-MM-dd}&toDate={toDate:yyyy-MM-dd}";
            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var jsonContent = await response.Content.ReadAsStringAsync();
                var stockChanges = JsonSerializer.Deserialize<List<KatanaStockDto>>(jsonContent, _jsonOptions) ?? new List<KatanaStockDto>();
                
                _logger.LogInformation("Retrieved {Count} stock changes from Katana", stockChanges.Count);
                return stockChanges;
            }

            _logger.LogWarning("Failed to get stock changes from Katana. Status: {StatusCode}, Reason: {ReasonPhrase}", 
                response.StatusCode, response.ReasonPhrase);
            return new List<KatanaStockDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock changes from Katana API");
            throw;
        }
    }

    public async Task<List<KatanaProductDto>> GetProductsAsync()
    {
        var allProducts = new List<KatanaProductDto>();
        var page = 1;
        const int PAGE_SIZE = 250; // Katana MRP API maksimum sayfa boyutu
        var moreRecords = true;

        try
        {
            _logger.LogInformation("Getting products from Katana with pagination (pageSize={PageSize})", PAGE_SIZE);
            _loggingService.LogInfo("Katana API: Fetching products (paged)", null, "GetProductsAsync", LogCategory.ExternalAPI);

            while (moreRecords)
            {
                var url = $"{_settings.Endpoints.Products}?limit={PAGE_SIZE}&page={page}";
                var response = await _httpClient.GetAsync(url);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Katana API failed on page {Page}. Status: {StatusCode}", page, response.StatusCode);
                    break;
                }

                try
                {
                    using var doc = JsonDocument.Parse(responseContent);
                    var root = doc.RootElement;
                    var pageProducts = new List<KatanaProductDto>();
                    if (root.TryGetProperty("data", out var dataEl) && dataEl.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var prodEl in dataEl.EnumerateArray())
                        {
                            var mapped = MapProductElement(prodEl);
                            pageProducts.Add(mapped);
                        }
                    }

                    _logger.LogInformation("Page {Page}: +{Count} products (Total: {Total})", page, pageProducts.Count, allProducts.Count + pageProducts.Count);
                    allProducts.AddRange(pageProducts);

                    // Eğer dönen kayıt sayısı PAGE_SIZE'dan azsa, son sayfaya ulaştık demektir
                    if (pageProducts.Count < PAGE_SIZE)
                    {
                        moreRecords = false;
                    }
                    else
                    {
                        page++;
                        // Rate limit için minimal bekleme
                        await Task.Delay(50);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to parse Katana products response on page {Page}", page);
                    moreRecords = false;
                }
            }

            _logger.LogInformation("✅ TOTAL {Count} products loaded from Katana across {Pages} pages.", allProducts.Count, page);
            _loggingService.LogInfo($"Successfully fetched {allProducts.Count} products from Katana ({page} pages)", null, "GetProductsAsync", LogCategory.ExternalAPI);
            return allProducts;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Katana API connection error: {Message}", ex.Message);
            _loggingService.LogError("Katana API connection failed", ex, null, "GetProductsAsync", LogCategory.ExternalAPI);
            return new List<KatanaProductDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling Katana API");
            _loggingService.LogError("Unexpected error in Katana API call", ex, null, "GetProductsAsync", LogCategory.ExternalAPI);
            return new List<KatanaProductDto>();
        }
    }


    
    private decimal ReadDecimalProperty(JsonElement el, string propName)
    {
        if (!el.TryGetProperty(propName, out var p))
            return 0m;

        try
        {
            if (p.ValueKind == JsonValueKind.Number)
                return p.GetDecimal();
            if (p.ValueKind == JsonValueKind.String && decimal.TryParse(p.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                return d;
        }
        catch { }
        return 0m;
    }

    
    private KatanaProductDto MapProductElement(JsonElement prodEl)
    {
        var dto = new KatanaProductDto();

        
        if (prodEl.TryGetProperty("id", out var idEl))
            dto.Id = idEl.ToString();

        dto.Name = prodEl.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? string.Empty : string.Empty;
        dto.Description = prodEl.TryGetProperty("additional_info", out var ai) ? ai.GetString() : null;

        
        if (prodEl.TryGetProperty("category_id", out var catIdEl) && catIdEl.ValueKind == JsonValueKind.Number)
        {
            try { dto.CategoryId = catIdEl.GetInt32(); } catch { dto.CategoryId = 0; }
        }
        else
        {
            dto.CategoryId = 0;
        }
        
        if (prodEl.TryGetProperty("category_name", out var catNameEl))
            dto.Category = catNameEl.GetString();

        
        dto.SKU = string.Empty;
        dto.Price = 0m;
        dto.IsActive = true;
        dto.Currency = "TRY";

        
        if (prodEl.TryGetProperty("archived_at", out var archived) && archived.ValueKind != JsonValueKind.Null)
            dto.IsActive = false;
        if (prodEl.TryGetProperty("deleted_at", out var deleted) && deleted.ValueKind != JsonValueKind.Null)
            dto.IsActive = false;

        
        if (prodEl.TryGetProperty("image_url", out var imgEl) && imgEl.ValueKind == JsonValueKind.String)
            dto.ImageUrl = imgEl.GetString();
        else if (prodEl.TryGetProperty("main_image_url", out var mainImg) && mainImg.ValueKind == JsonValueKind.String)
            dto.ImageUrl = mainImg.GetString();

        
        if (prodEl.TryGetProperty("sku", out var productSkuEl) && productSkuEl.ValueKind == JsonValueKind.String)
            dto.SKU = productSkuEl.GetString() ?? string.Empty;

        
        if (prodEl.TryGetProperty("in_stock", out var prodInStockEl) && prodInStockEl.ValueKind == JsonValueKind.Number)
            dto.InStock = prodInStockEl.GetInt32();
        
        if (prodEl.TryGetProperty("on_hand", out var prodOnHandEl) && prodOnHandEl.ValueKind == JsonValueKind.Number)
            dto.OnHand = prodOnHandEl.GetInt32();
        
        if (prodEl.TryGetProperty("available", out var prodAvailEl) && prodAvailEl.ValueKind == JsonValueKind.Number)
            dto.Available = prodAvailEl.GetInt32();
        
        if (prodEl.TryGetProperty("committed", out var prodCommitEl) && prodCommitEl.ValueKind == JsonValueKind.Number)
            dto.Committed = prodCommitEl.GetInt32();

        
        if (prodEl.TryGetProperty("variants", out var variantsEl) && variantsEl.ValueKind == JsonValueKind.Array)
        {
            var firstVar = variantsEl.EnumerateArray().FirstOrDefault();
            if (firstVar.ValueKind != JsonValueKind.Undefined && firstVar.ValueKind != JsonValueKind.Null)
            {
                if (firstVar.TryGetProperty("sku", out var skuEl) && skuEl.ValueKind == JsonValueKind.String)
                    dto.SKU = skuEl.GetString() ?? string.Empty;

                
                dto.Price = ReadDecimalProperty(firstVar, "sales_price");
                dto.SalesPrice = ReadDecimalProperty(firstVar, "sales_price");
                dto.CostPrice = ReadDecimalProperty(firstVar, "cost");
                dto.PurchasePrice = dto.CostPrice;

                
                if (firstVar.TryGetProperty("unit", out var unitEl))
                    dto.Unit = unitEl.GetString();

                if (firstVar.TryGetProperty("barcode", out var barcodeEl) && barcodeEl.ValueKind == JsonValueKind.String)
                    dto.Barcode = barcodeEl.GetString();

                
                if (firstVar.TryGetProperty("in_stock", out var inStockEl) && inStockEl.ValueKind == JsonValueKind.Number)
                    dto.InStock = inStockEl.GetInt32();
                
                if (firstVar.TryGetProperty("on_hand", out var onHandEl) && onHandEl.ValueKind == JsonValueKind.Number)
                    dto.OnHand = onHandEl.GetInt32();
                
                if (firstVar.TryGetProperty("available", out var availEl) && availEl.ValueKind == JsonValueKind.Number)
                    dto.Available = availEl.GetInt32();
                
                if (firstVar.TryGetProperty("committed", out var commitEl) && commitEl.ValueKind == JsonValueKind.Number)
                    dto.Committed = commitEl.GetInt32();
            }
        }

        return dto;
    }

    
    private async Task<string?> GetVariantSkuAsync(int variantId)
    {
        var cacheKey = $"variant-sku-{variantId}";
        if (_cache.TryGetValue<string?>(cacheKey, out var cached))
            return cached;

        try
        {
            var resp = await _httpClient.GetAsync($"variants/{variantId}");
            if (!resp.IsSuccessStatusCode)
                return null;

            var content = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;
            
            JsonElement varEl = root;
            if (root.TryGetProperty("data", out var dataEl) && dataEl.ValueKind == JsonValueKind.Array)
                varEl = dataEl.EnumerateArray().FirstOrDefault();

            if (varEl.ValueKind == JsonValueKind.Object && varEl.TryGetProperty("sku", out var skuEl) && skuEl.ValueKind == JsonValueKind.String)
            {
                var sku = skuEl.GetString();
                _cache.Set(cacheKey, sku, TimeSpan.FromMinutes(10));
                return sku;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get variant sku for id {VariantId}", variantId);
        }

        return null;
    }

    
    private KatanaInvoiceDto MapInvoiceElement(JsonElement invEl, out List<int?> variantIds)
    {
        variantIds = new List<int?>();
        var dto = new KatanaInvoiceDto();

        
        if (invEl.TryGetProperty("customer_id", out var custEl) && custEl.ValueKind == JsonValueKind.Number)
        {
            try { dto.ExternalCustomerId = custEl.GetInt32(); } catch { dto.ExternalCustomerId = null; }
        }

        if (invEl.TryGetProperty("order_no", out var noEl) && noEl.ValueKind == JsonValueKind.String)
            dto.InvoiceNo = noEl.GetString() ?? string.Empty;

        dto.Amount = ReadDecimalProperty(invEl, "total");
        dto.TotalAmount = dto.Amount;

        if (invEl.TryGetProperty("order_created_date", out var createdEl) && createdEl.ValueKind == JsonValueKind.String)
            dto.InvoiceDate = DateTime.TryParse(createdEl.GetString(), out var d) ? d : DateTime.MinValue;

        dto.Currency = invEl.TryGetProperty("currency", out var curEl) && curEl.ValueKind == JsonValueKind.String ? curEl.GetString() ?? "" : "";

        dto.Items = new List<KatanaInvoiceItemDto>();
        if (invEl.TryGetProperty("sales_order_rows", out var rowsEl) && rowsEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var row in rowsEl.EnumerateArray())
            {
                var item = new KatanaInvoiceItemDto();
                
                item.ProductSKU = string.Empty;
                item.ProductName = string.Empty;
                item.Quantity = row.TryGetProperty("quantity", out var qEl) && qEl.ValueKind == JsonValueKind.Number ? qEl.GetInt32() : 0;
                item.UnitPrice = ReadDecimalProperty(row, "price_per_unit");
                item.TaxRate = ReadDecimalProperty(row, "tax_rate");
                item.TaxAmount = 0m;
                item.TotalAmount = ReadDecimalProperty(row, "total");

                if (row.TryGetProperty("variant_id", out var vidEl) && vidEl.ValueKind == JsonValueKind.Number)
                {
                    try { variantIds.Add(vidEl.GetInt32()); }
                    catch { variantIds.Add(null); }
                }
                else
                {
                    variantIds.Add(null);
                }

                dto.Items.Add(item);
            }
        }

        return dto;
    }

    public async Task<List<KatanaInvoiceDto>> GetInvoicesAsync(DateTime fromDate, DateTime toDate)
    {
        try
        {
            _logger.LogInformation("Getting invoices from {FromDate} to {ToDate}", fromDate, toDate);

            var url = $"{_settings.Endpoints.SalesOrders}?fromDate={fromDate:yyyy-MM-dd}&toDate={toDate:yyyy-MM-dd}";
            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var jsonContent = await response.Content.ReadAsStringAsync();
                try
                {
                    using var doc = JsonDocument.Parse(jsonContent);
                    var root = doc.RootElement;
                    var invoices = new List<KatanaInvoiceDto>();
                    if (root.TryGetProperty("data", out var dataEl) && dataEl.ValueKind == JsonValueKind.Array)
                    {
                        
                        var variantLookupEntries = new List<List<int?>>();
                        foreach (var invEl in dataEl.EnumerateArray())
                        {
                            var mapped = MapInvoiceElement(invEl, out var variantIds);
                            invoices.Add(mapped);
                            variantLookupEntries.Add(variantIds);
                        }

                        
                        var allVariantIds = variantLookupEntries.SelectMany(x => x).Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToList();
                        var variantSkuMap = new Dictionary<int, string>();
                        foreach (var vid in allVariantIds)
                        {
                            var sku = await GetVariantSkuAsync(vid);
                            if (!string.IsNullOrEmpty(sku))
                                variantSkuMap[vid] = sku;
                        }

                        
                        for (int i = 0; i < invoices.Count; i++)
                        {
                            var invoice = invoices[i];
                            var variantIds = variantLookupEntries[i];
                            for (int j = 0; j < invoice.Items.Count && j < variantIds.Count; j++)
                            {
                                var vid = variantIds[j];
                                if (vid.HasValue && variantSkuMap.TryGetValue(vid.Value, out var sku))
                                    invoice.Items[j].ProductSKU = sku;
                            }
                        }
                    }

                    _logger.LogInformation("Retrieved {Count} invoices from Katana", invoices.Count);
                    return invoices;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to parse Katana invoices response");
                    return new List<KatanaInvoiceDto>();
                }
            }

            _logger.LogWarning("Failed to get invoices from Katana. Status: {StatusCode}, Reason: {ReasonPhrase}", 
                response.StatusCode, response.ReasonPhrase);
            return new List<KatanaInvoiceDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting invoices from Katana API");
            throw;
        }
    }

    public async Task<KatanaProductDto?> GetProductBySkuAsync(string sku)
    {
        try
        {
            _logger.LogInformation("Getting product by SKU: {SKU}", sku);
            
            var variantUrl = $"{_settings.Endpoints.Variants}?sku={Uri.EscapeDataString(sku)}";
            var varResp = await _httpClient.GetAsync(variantUrl);
            var varContent = await varResp.Content.ReadAsStringAsync();

            if (!varResp.IsSuccessStatusCode)
            {
                _logger.LogWarning("Katana API variant lookup failed for SKU {SKU}. Status: {Status}", sku, varResp.StatusCode);
                return null;
            }

            try
            {
                using var doc = JsonDocument.Parse(varContent);
                var root = doc.RootElement;
                if (!root.TryGetProperty("data", out var dataEl) || dataEl.ValueKind != JsonValueKind.Array)
                    return null;

                var firstVar = dataEl.EnumerateArray().FirstOrDefault();
                if (firstVar.ValueKind == JsonValueKind.Undefined || firstVar.ValueKind == JsonValueKind.Null)
                    return null;

                
                if (!firstVar.TryGetProperty("product_id", out var pidEl))
                    return null;

                int productId = 0;
                try { productId = pidEl.GetInt32(); } catch { return null; }

                
                var productResp = await _httpClient.GetAsync($"{_settings.Endpoints.Products}/{productId}");
                var productContent = await productResp.Content.ReadAsStringAsync();
                if (!productResp.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Katana API product fetch failed for id {Id}. Status: {Status}", productId, productResp.StatusCode);
                    return null;
                }

                using var prodDoc = JsonDocument.Parse(productContent);
                var prodRoot = prodDoc.RootElement;
                
                JsonElement productElement = prodRoot;
                if (prodRoot.TryGetProperty("data", out var wrapped) && wrapped.ValueKind == JsonValueKind.Object)
                    productElement = wrapped;

                var mapped = MapProductElement(productElement);
                _logger.LogInformation("Retrieved product {SKU}", sku);
                return mapped;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing variant response for SKU {SKU}", sku);
                return null;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Katana API connection error for SKU {SKU}", sku);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting product {SKU}", sku);
            return null;
        }
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            _logger.LogInformation("Testing Katana API connection");

            var response = await _httpClient.GetAsync($"{_settings.Endpoints.Products}?limit=1");
            var isConnected = response.IsSuccessStatusCode;
            
            _logger.LogInformation("Katana API connection: {Status}", isConnected ? "OK" : "Failed");
            return isConnected;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing connection to Katana API");
            return false;
        }
    }

    public async Task<bool> UpdateProductAsync(int katanaProductId, string name, decimal? salesPrice, int? stock)
    {
        try
        {
            _logger.LogInformation("Updating Katana product ID: {ProductId}", katanaProductId);

            var updatePayload = new
            {
                name = name,
                variants = new[]
                {
                    new
                    {
                        sales_price = salesPrice,
                        on_hand = stock
                    }
                }
            };

            var json = JsonSerializer.Serialize(updatePayload, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PatchAsync($"{_settings.Endpoints.Products}/{katanaProductId}", content);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully updated Katana product ID: {ProductId}", katanaProductId);
                return true;
            }
            
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Failed to update Katana product ID: {ProductId}. Status: {Status}, Error: {Error}", 
                katanaProductId, response.StatusCode, errorContent);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating Katana product ID: {ProductId}", katanaProductId);
            return false;
        }
    }

    public Task<List<KatanaPurchaseOrderDto>> GetPurchaseOrdersAsync(string? status = null, DateTime? fromDate = null)
    {
        _logger.LogInformation("GetPurchaseOrdersAsync called (status: {Status}, fromDate: {FromDate}), placeholder implementation returning empty list.", status, fromDate);
        return Task.FromResult(new List<KatanaPurchaseOrderDto>());
    }

    public Task<KatanaPurchaseOrderDto?> GetPurchaseOrderByIdAsync(string id)
    {
        _logger.LogInformation("GetPurchaseOrderByIdAsync called for Id: {Id}, placeholder implementation returning null.", id);
        return Task.FromResult<KatanaPurchaseOrderDto?>(null);
    }

    public Task<string?> ReceivePurchaseOrderAsync(string id)
    {
        _logger.LogInformation("ReceivePurchaseOrderAsync called for Id: {Id}, placeholder implementation returning null.", id);
        return Task.FromResult<string?>(null);
    }

    public Task<List<KatanaSupplierDto>> GetSuppliersAsync()
    {
        _logger.LogInformation("GetSuppliersAsync called, but Katana endpoint mapping is not configured. Returning empty list.");
        return Task.FromResult(new List<KatanaSupplierDto>());
    }

    public Task<KatanaSupplierDto?> GetSupplierByIdAsync(string id)
    {
        _logger.LogInformation("GetSupplierByIdAsync called for Id: {Id}, placeholder implementation returning null.", id);
        return Task.FromResult<KatanaSupplierDto?>(null);
    }

    public Task<List<KatanaManufacturingOrderDto>> GetManufacturingOrdersAsync(string? status = null)
    {
        _logger.LogInformation("GetManufacturingOrdersAsync called (status: {Status}), placeholder implementation returning empty list.", status);
        return Task.FromResult(new List<KatanaManufacturingOrderDto>());
    }

    public Task<KatanaManufacturingOrderDto?> GetManufacturingOrderByIdAsync(string id)
    {
        _logger.LogInformation("GetManufacturingOrderByIdAsync called for Id: {Id}, placeholder implementation returning null.", id);
        return Task.FromResult<KatanaManufacturingOrderDto?>(null);
    }

    public Task<KatanaVariantDto?> GetVariantAsync(string variantId)
    {
        _logger.LogInformation("GetVariantAsync called for VariantId: {VariantId}, placeholder implementation returning null.", variantId);
        return Task.FromResult<KatanaVariantDto?>(null);
    }

    public Task<List<KatanaVariantDto>> GetVariantsAsync(string? productId = null)
    {
        _logger.LogInformation("GetVariantsAsync called (productId: {ProductId}), placeholder implementation returning empty list.", productId);
        return Task.FromResult(new List<KatanaVariantDto>());
    }

    public Task<List<KatanaBatchDto>> GetBatchesAsync(string? productId = null)
    {
        _logger.LogInformation("GetBatchesAsync called for ProductId: {ProductId}, placeholder implementation returning empty list.", productId);
        return Task.FromResult(new List<KatanaBatchDto>());
    }

    public Task<List<KatanaStockTransferDto>> GetStockTransfersAsync(string? status = null)
    {
        _logger.LogInformation("GetStockTransfersAsync called (status: {Status}), placeholder implementation returning empty list.", status);
        return Task.FromResult(new List<KatanaStockTransferDto>());
    }

    public Task<List<KatanaSalesReturnDto>> GetSalesReturnsAsync(DateTime? fromDate = null)
    {
        _logger.LogInformation("GetSalesReturnsAsync called (fromDate: {FromDate}), placeholder implementation returning empty list.", fromDate);
        return Task.FromResult(new List<KatanaSalesReturnDto>());
    }

    public async Task<List<StockAdjustmentDto>> GetStockAdjustmentsAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = new List<string>();
        if (fromDate.HasValue) query.Add($"fromDate={fromDate:yyyy-MM-dd}");
        if (toDate.HasValue) query.Add($"toDate={toDate:yyyy-MM-dd}");
        var url = _settings.Endpoints.StockAdjustments + (query.Count > 0 ? "?" + string.Join("&", query) : string.Empty);

        _logger.LogInformation("Fetching stock adjustments from Katana: {Url}", url);
        var resp = await _httpClient.GetAsync(url);
        var content = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("GetStockAdjustmentsAsync failed. Status {Status}, Body {Body}", resp.StatusCode, content);
            resp.EnsureSuccessStatusCode();
        }

        return JsonSerializer.Deserialize<List<StockAdjustmentDto>>(content, _jsonOptions) ?? new List<StockAdjustmentDto>();
    }

    public async Task<StockAdjustmentDto?> CreateStockAdjustmentAsync(StockAdjustmentCreateRequest request)
    {
        _logger.LogInformation("Creating stock adjustment via Katana API");
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var resp = await _httpClient.PostAsync(_settings.Endpoints.StockAdjustments, new StringContent(json, Encoding.UTF8, "application/json"));
        var content = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("CreateStockAdjustmentAsync failed. Status {Status}, Body {Body}", resp.StatusCode, content);
            resp.EnsureSuccessStatusCode();
        }

        return JsonSerializer.Deserialize<StockAdjustmentDto>(content, _jsonOptions);
    }

    public async Task<List<InventoryMovementDto>> GetInventoryMovementsAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = new List<string>();
        if (fromDate.HasValue) query.Add($"fromDate={fromDate:yyyy-MM-dd}");
        if (toDate.HasValue) query.Add($"toDate={toDate:yyyy-MM-dd}");
        var url = _settings.Endpoints.Stocktakes + (query.Count > 0 ? "?" + string.Join("&", query) : string.Empty);

        _logger.LogInformation("Fetching inventory movements from Katana: {Url}", url);
        var resp = await _httpClient.GetAsync(url);
        var content = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("GetInventoryMovementsAsync failed. Status {Status}, Body {Body}", resp.StatusCode, content);
            resp.EnsureSuccessStatusCode();
        }

        return JsonSerializer.Deserialize<List<InventoryMovementDto>>(content, _jsonOptions) ?? new List<InventoryMovementDto>();
    }

    public Task<List<SalesOrderDto>> GetSalesOrdersAsync(DateTime? fromDate = null)
    {
        var query = fromDate.HasValue ? $"?fromDate={fromDate:yyyy-MM-dd}" : string.Empty;
        return GetListAsync<SalesOrderDto>(_settings.Endpoints.SalesOrders + query, "sales orders");
    }

    /// <summary>
    /// Memory-efficient batched sales order retrieval with server-side pagination
    /// Uses Katana API's native pagination instead of loading all data
    /// </summary>
    public async IAsyncEnumerable<List<SalesOrderDto>> GetSalesOrdersBatchedAsync(
        DateTime? fromDate = null, 
        int batchSize = 100)
    {
        int page = 1;
        int totalRetrieved = 0;
        
        _logger.LogInformation("Starting paginated sales order retrieval (batchSize={BatchSize})", batchSize);

        while (true)
        {
            // Build query with pagination + optional date filter
            var queryParams = new List<string>
            {
                $"page={page}",
                $"limit={batchSize}"
            };
            
            if (fromDate.HasValue)
            {
                queryParams.Add($"created_at_min={fromDate:yyyy-MM-dd}");
            }
            
            var query = "?" + string.Join("&", queryParams);
            
            _logger.LogDebug("Fetching page {Page} from Katana API", page);
            
            List<SalesOrderDto> batch;
            try
            {
                batch = await GetListAsync<SalesOrderDto>(
                    _settings.Endpoints.SalesOrders + query, 
                    $"sales orders (page {page})");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch page {Page} from Katana API", page);
                break;
            }
            
            // No more data
            if (batch == null || batch.Count == 0)
            {
                _logger.LogInformation("Completed pagination after {TotalPages} pages, {TotalOrders} total orders", 
                    page - 1, totalRetrieved);
                break;
            }
            
            totalRetrieved += batch.Count;
            
            _logger.LogDebug("Retrieved page {Page} with {Count} orders (total so far: {Total})", 
                page, batch.Count, totalRetrieved);
            
            yield return batch;
            
            // If batch is smaller than limit, we've reached the end
            if (batch.Count < batchSize)
            {
                _logger.LogInformation("Completed pagination (last partial page), {TotalPages} pages, {TotalOrders} total orders", 
                    page, totalRetrieved);
                break;
            }
            
            page++;
            
            // Small delay to avoid overwhelming API
            await Task.Delay(50);
        }
    }

    public async Task<SalesOrderDto?> CreateSalesOrderAsync(SalesOrderDto salesOrder)
    {
        _logger.LogInformation("CreateSalesOrderAsync called for OrderNo: {OrderNo}", salesOrder?.OrderNo);
        var json = JsonSerializer.Serialize(salesOrder, _jsonOptions);
        var resp = await _httpClient.PostAsync(_settings.Endpoints.SalesOrders, new StringContent(json, Encoding.UTF8, "application/json"));
        var content = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("CreateSalesOrderAsync failed. Status {Status}, Body {Body}", resp.StatusCode, content);
            resp.EnsureSuccessStatusCode();
        }
        return JsonSerializer.Deserialize<SalesOrderDto>(content, _jsonOptions);
    }

    public async Task<SalesOrderDto?> UpdateSalesOrderAsync(SalesOrderDto salesOrder)
    {
        _logger.LogInformation("UpdateSalesOrderAsync called for OrderNo: {OrderNo}", salesOrder?.OrderNo);
        var json = JsonSerializer.Serialize(salesOrder, _jsonOptions);
        var resp = await _httpClient.PutAsync($"{_settings.Endpoints.SalesOrders}/{salesOrder?.Id}", new StringContent(json, Encoding.UTF8, "application/json"));
        var content = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("UpdateSalesOrderAsync failed. Status {Status}, Body {Body}", resp.StatusCode, content);
            resp.EnsureSuccessStatusCode();
        }
        return JsonSerializer.Deserialize<SalesOrderDto>(content, _jsonOptions);
    }

    public Task<List<LocationDto>> GetLocationsAsync()
    {
        return GetListAsync<LocationDto>(_settings.Endpoints.Locations, "locations");
    }

    public Task<List<ServiceDto>> GetServicesAsync()
    {
        return GetListAsync<ServiceDto>("services", "services");
    }

    public Task<List<TaxRateDto>> GetTaxRatesAsync()
    {
        return GetListAsync<TaxRateDto>(_settings.Endpoints.TaxRates, "tax rates");
    }

    public Task<List<PriceListDto>> GetPriceListsAsync()
    {
        return GetListAsync<PriceListDto>(_settings.Endpoints.PriceLists, "price lists");
    }

    public Task<List<WebhookDto>> GetWebhooksAsync()
    {
        return GetListAsync<WebhookDto>("webhooks", "webhooks");
    }

    public Task<List<SerialNumberDto>> GetSerialNumbersAsync()
    {
        return GetListAsync<SerialNumberDto>("serial-numbers", "serial numbers");
    }

    public Task<List<UserDto>> GetUsersAsync()
    {
        return GetListAsync<UserDto>("users", "users");
    }

    public Task<List<BomRowDto>> GetBomRowsAsync()
    {
        return GetListAsync<BomRowDto>(_settings.Endpoints.BomRows, "bom rows");
    }

    public Task<List<MaterialDto>> GetMaterialsAsync()
    {
        return GetListAsync<MaterialDto>(_settings.Endpoints.Materials, "materials");
    }

    private async Task<List<T>> GetListAsync<T>(string endpoint, string logName)
    {
        try
        {
            _logger.LogInformation("Fetching {Name} from Katana: {Endpoint}", logName, endpoint);
            var resp = await _httpClient.GetAsync(endpoint);
            var content = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("{Name} fetch failed. Status {Status}, Body {Body}", logName, resp.StatusCode, content);
                resp.EnsureSuccessStatusCode();
            }

            try
            {
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;
                if (root.TryGetProperty("data", out var dataEl) && dataEl.ValueKind == JsonValueKind.Array)
                {
                    return JsonSerializer.Deserialize<List<T>>(dataEl.GetRawText(), _jsonOptions) ?? new List<T>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to unwrap data property for {Name}, falling back to direct deserialization.", logName);
            }

            return JsonSerializer.Deserialize<List<T>>(content, _jsonOptions) ?? new List<T>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching {Name} from Katana", logName);
            throw;
        }
    }
}
