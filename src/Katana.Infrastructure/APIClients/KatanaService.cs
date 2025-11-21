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
/*Katana API'sından veri okuma operasyonlarını içerecek.
Amacı: Sadece Katana'dan veri okumaktan sorumlu olmak.
Sorumlulukları (Yeni):
Belirli bir tarihten sonraki stok hareketlerini getirme metodu.
Ödeme durumu değişen faturaları getirme metodu.*/
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

            var url = $"{_settings.Endpoints.Stock}?fromDate={fromDate:yyyy-MM-dd}&toDate={toDate:yyyy-MM-dd}";
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
        try
        {
            _logger.LogInformation("Getting products from Katana");
            _logger.LogInformation("DEBUG - BaseAddress: {BaseAddress}", _httpClient.BaseAddress);
            _logger.LogInformation("DEBUG - Endpoint: {Endpoint}", _settings.Endpoints.Products);
            _logger.LogInformation("DEBUG - Authorization: {Auth}", _httpClient.DefaultRequestHeaders.Authorization);
            _loggingService.LogInfo("Katana API: Fetching products", null, "GetProductsAsync", LogCategory.ExternalAPI);
            var response = await _httpClient.GetAsync(_settings.Endpoints.Products);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogWarning("KATANA API RESPONSE - Status: {StatusCode}, Content Length: {Length}, First 500 chars: {Content}", 
                response.StatusCode, responseContent.Length, responseContent.Substring(0, Math.Min(500, responseContent.Length)));

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Katana API failed. Status: {StatusCode}, Response: {Response}", 
                    response.StatusCode, responseContent);
                return new List<KatanaProductDto>();
            }

            // Parse JSON and map to internal KatanaProductDto shape
            try
            {
                using var doc = JsonDocument.Parse(responseContent);
                var root = doc.RootElement;
                var products = new List<KatanaProductDto>();
                if (root.TryGetProperty("data", out var dataEl) && dataEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var prodEl in dataEl.EnumerateArray())
                    {
                        var mapped = MapProductElement(prodEl);
                        products.Add(mapped);
                    }
                }

                _logger.LogInformation("Retrieved {Count} products from Katana", products.Count);
                _loggingService.LogInfo($"Successfully fetched {products.Count} products from Katana", null, "GetProductsAsync", LogCategory.ExternalAPI);
                return products;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse Katana products response");
                return new List<KatanaProductDto>();
            }
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

    private class KatanaApiResponse
    {
        public List<KatanaProductDto>? Data { get; set; }
    }

    // Helper: safely read decimal from JsonElement property which may be number or string
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

    // Map a raw product JsonElement (external API) to internal KatanaProductDto
    private KatanaProductDto MapProductElement(JsonElement prodEl)
    {
        var dto = new KatanaProductDto();

        // ID
        if (prodEl.TryGetProperty("id", out var idEl))
            dto.Id = idEl.ToString();

        dto.Name = prodEl.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? string.Empty : string.Empty;
        dto.Description = prodEl.TryGetProperty("additional_info", out var ai) ? ai.GetString() : null;

        // Category
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

        // Default values
        dto.SKU = string.Empty;
        dto.Price = 0m;
        dto.IsActive = true;

        // Determine active state from archived_at / deleted_at
        if (prodEl.TryGetProperty("archived_at", out var archived) && archived.ValueKind != JsonValueKind.Null)
            dto.IsActive = false;
        if (prodEl.TryGetProperty("deleted_at", out var deleted) && deleted.ValueKind != JsonValueKind.Null)
            dto.IsActive = false;

        // Try to read image url
        if (prodEl.TryGetProperty("image_url", out var imgEl) && imgEl.ValueKind == JsonValueKind.String)
            dto.ImageUrl = imgEl.GetString();
        else if (prodEl.TryGetProperty("main_image_url", out var mainImg) && mainImg.ValueKind == JsonValueKind.String)
            dto.ImageUrl = mainImg.GetString();

        // Try to read SKU from product level first (some APIs don't have variants)
        if (prodEl.TryGetProperty("sku", out var productSkuEl) && productSkuEl.ValueKind == JsonValueKind.String)
            dto.SKU = productSkuEl.GetString() ?? string.Empty;

        // Try to read stock from product level
        if (prodEl.TryGetProperty("in_stock", out var prodInStockEl) && prodInStockEl.ValueKind == JsonValueKind.Number)
            dto.InStock = prodInStockEl.GetInt32();
        
        if (prodEl.TryGetProperty("on_hand", out var prodOnHandEl) && prodOnHandEl.ValueKind == JsonValueKind.Number)
            dto.OnHand = prodOnHandEl.GetInt32();
        
        if (prodEl.TryGetProperty("available", out var prodAvailEl) && prodAvailEl.ValueKind == JsonValueKind.Number)
            dto.Available = prodAvailEl.GetInt32();
        
        if (prodEl.TryGetProperty("committed", out var prodCommitEl) && prodCommitEl.ValueKind == JsonValueKind.Number)
            dto.Committed = prodCommitEl.GetInt32();

        // Variants: get first variant's sku, prices and stock levels (override product-level if exists)
        if (prodEl.TryGetProperty("variants", out var variantsEl) && variantsEl.ValueKind == JsonValueKind.Array)
        {
            var firstVar = variantsEl.EnumerateArray().FirstOrDefault();
            if (firstVar.ValueKind != JsonValueKind.Undefined && firstVar.ValueKind != JsonValueKind.Null)
            {
                if (firstVar.TryGetProperty("sku", out var skuEl) && skuEl.ValueKind == JsonValueKind.String)
                    dto.SKU = skuEl.GetString() ?? string.Empty;

                // Prices
                dto.Price = ReadDecimalProperty(firstVar, "sales_price");
                dto.SalesPrice = ReadDecimalProperty(firstVar, "sales_price");
                dto.CostPrice = ReadDecimalProperty(firstVar, "cost");

                // Unit
                if (firstVar.TryGetProperty("unit", out var unitEl))
                    dto.Unit = unitEl.GetString();

                // Stock levels - Katana API provides: in_stock, committed, available, on_hand
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

    // Resolve a variant's SKU by variant id (with simple in-memory caching)
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
            // variant endpoint might return object directly or wrapped in data array
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

    // Map a raw sales_order JsonElement to external KatanaInvoiceDto
    private KatanaInvoiceDto MapInvoiceElement(JsonElement invEl, out List<int?> variantIds)
    {
        variantIds = new List<int?>();
        var dto = new KatanaInvoiceDto();

        // capture external customer id if available (helps downstream resolution)
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
                // SKU not available here; product info may require separate lookup
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

            var url = $"{_settings.Endpoints.Invoices}?fromDate={fromDate:yyyy-MM-dd}&toDate={toDate:yyyy-MM-dd}";
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
                        // Map invoices and collect variant ids for SKU lookup
                        var variantLookupEntries = new List<List<int?>>();
                        foreach (var invEl in dataEl.EnumerateArray())
                        {
                            var mapped = MapInvoiceElement(invEl, out var variantIds);
                            invoices.Add(mapped);
                            variantLookupEntries.Add(variantIds);
                        }

                        // Resolve SKUs for collected variant ids
                        var allVariantIds = variantLookupEntries.SelectMany(x => x).Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToList();
                        var variantSkuMap = new Dictionary<int, string>();
                        foreach (var vid in allVariantIds)
                        {
                            var sku = await GetVariantSkuAsync(vid);
                            if (!string.IsNullOrEmpty(sku))
                                variantSkuMap[vid] = sku;
                        }

                        // Apply SKUs back to invoice items
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
            // Correct approach: query variants endpoint to find variant by sku, then fetch product by product_id
            var variantUrl = $"variants?sku={Uri.EscapeDataString(sku)}";
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

                // product_id expected
                if (!firstVar.TryGetProperty("product_id", out var pidEl))
                    return null;

                int productId = 0;
                try { productId = pidEl.GetInt32(); } catch { return null; }

                // Fetch product by numeric id
                var productResp = await _httpClient.GetAsync($"{_settings.Endpoints.Products}/{productId}");
                var productContent = await productResp.Content.ReadAsStringAsync();
                if (!productResp.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Katana API product fetch failed for id {Id}. Status: {Status}", productId, productResp.StatusCode);
                    return null;
                }

                using var prodDoc = JsonDocument.Parse(productContent);
                var prodRoot = prodDoc.RootElement;
                // The product endpoint may return the product object directly or wrapped in data
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
}
