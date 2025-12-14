using Katana.Core.DTOs;
using Katana.Business.Interfaces;
using Katana.Core.Enums;
using Katana.Data.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text;
using System.Net.Http.Headers;
using System.Linq;
using System.Globalization;
using Microsoft.Extensions.Caching.Memory;
using System.Net;
using Katana.Core.Converters;

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
            WriteIndented = false,
            Converters = { new StringToDecimalConverter() }
        };
    }

    private static StringContent CreateKatanaJsonContent(string json)
    {
        var content = new StringContent(json, Encoding.UTF8);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json")
        {
            CharSet = null
        };
        return content;
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

    public async Task<bool> SyncProductStockAsync(string sku, decimal quantity, long? locationId = null, string? productName = null, decimal? salesPrice = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(sku))
            {
                _logger.LogWarning("SyncProductStockAsync called with empty SKU");
                return false;
            }

            sku = sku.Trim();

            if (quantity <= 0)
            {
                _logger.LogWarning("SyncProductStockAsync called with non-positive quantity. Sku={Sku}, Qty={Qty}", sku, quantity);
                return false;
            }

            async Task<(long? VariantId, long? ProductId)> FindVariantAsync(string inputSku)
            {
                var skuVariants = new[] { inputSku, inputSku.Trim(), inputSku.ToLowerInvariant(), inputSku.ToUpperInvariant() }
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .ToArray();

                foreach (var skuVariant in skuVariants)
                {
                    try
                    {
                        var variantUrl = $"{_settings.Endpoints.Variants}?sku={Uri.EscapeDataString(skuVariant)}";
                        var resp = await _httpClient.GetAsync(variantUrl);
                        var body = await resp.Content.ReadAsStringAsync();

                        if (!resp.IsSuccessStatusCode)
                        {
                            _logger.LogWarning("Katana variant lookup failed. Sku={Sku}, Status={Status}, Body={Body}", skuVariant, resp.StatusCode, body);
                            continue;
                        }

                        using var doc = JsonDocument.Parse(body);
                        if (!doc.RootElement.TryGetProperty("data", out var dataEl) || dataEl.ValueKind != JsonValueKind.Array)
                            continue;

                        var first = dataEl.EnumerateArray().FirstOrDefault();
                        if (first.ValueKind != JsonValueKind.Object)
                            continue;

                        long? variantId = null;
                        long? productId = null;

                        if (first.TryGetProperty("id", out var idEl) && idEl.ValueKind == JsonValueKind.Number)
                            variantId = idEl.GetInt64();
                        else if (first.TryGetProperty("id", out idEl) && idEl.ValueKind == JsonValueKind.String && long.TryParse(idEl.GetString(), out var parsedVar))
                            variantId = parsedVar;

                        if (first.TryGetProperty("product_id", out var pidEl) && pidEl.ValueKind == JsonValueKind.Number)
                            productId = pidEl.GetInt64();
                        else if (first.TryGetProperty("product_id", out pidEl) && pidEl.ValueKind == JsonValueKind.String && long.TryParse(pidEl.GetString(), out var parsedPid))
                            productId = parsedPid;

                        if (variantId.HasValue)
                        {
                            _logger.LogInformation("Katana variant found. Sku={Sku}, VariantId={VariantId}, ProductId={ProductId}", skuVariant, variantId, productId);
                            return (variantId, productId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Katana variant lookup parse failed. Sku={Sku}", skuVariant);
                    }
                }

                return (null, null);
            }

            async Task<long?> ResolveLocationIdAsync()
            {
                if (locationId.HasValue && locationId.Value > 0)
                    return locationId.Value;

                const string cacheKey = "katana-primary-location-id";
                if (_cache.TryGetValue<long?>(cacheKey, out var cached) && cached.HasValue)
                    return cached;

                try
                {
                    var locations = await GetLocationsAsync();
                    var primary = locations.FirstOrDefault(l => l.IsPrimary == true) ?? locations.FirstOrDefault();
                    if (primary == null)
                        return null;

                    _cache.Set(cacheKey, primary.Id, TimeSpan.FromMinutes(30));
                    return primary.Id;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to resolve Katana primary location id");
                    return null;
                }
            }

            var (variantId, _) = await FindVariantAsync(sku);
            var createdNewProduct = false;
            if (!variantId.HasValue)
            {
                _logger.LogWarning("SyncProductStockAsync: variant not found in Katana for SKU {Sku}. Attempting create...", sku);

                var createDto = new KatanaProductDto
                {
                    Name = string.IsNullOrWhiteSpace(productName) ? $"Yeni Ürün ({sku})" : productName.Trim(),
                    SKU = sku,
                    SalesPrice = salesPrice ?? 0,
                    Unit = "pcs",
                    IsActive = true
                };

                var created = await CreateProductAsync(createDto);
                if (created == null)
                {
                    // SKU already exists / race condition scenarios may yield null (e.g., 422),
                    // so we fall back to re-querying variants before failing hard.
                    _logger.LogWarning("SyncProductStockAsync: CreateProductAsync returned null. Will retry variant lookup. Sku={Sku}", sku);
                }
                else
                {
                    createdNewProduct = true;
                }

                // After creating product (or if creation raced), re-query variants by SKU to obtain variant_id.
                (variantId, _) = await FindVariantAsync(sku);
                if (!variantId.HasValue)
                {
                    _logger.LogWarning("SyncProductStockAsync: variant still not found after create attempt. Sku={Sku}", sku);
                    return false;
                }
            }

            var resolvedLocationId = await ResolveLocationIdAsync();
            if (!resolvedLocationId.HasValue)
            {
                _logger.LogWarning("SyncProductStockAsync: location id could not be resolved. Sku={Sku}", sku);
                return false;
            }

            var req = new StockAdjustmentCreateRequest
            {
                StockAdjustmentNumber = $"{(createdNewProduct ? "ADMIN-NEW" : "ADMIN")}-{DateTime.UtcNow:yyyyMMddHHmmss}-{sku}".Length > 50
                    ? $"{(createdNewProduct ? "ADMIN-NEW" : "ADMIN")}-{DateTime.UtcNow:yyyyMMddHHmmss}-{sku}".Substring(0, 50)
                    : $"{(createdNewProduct ? "ADMIN-NEW" : "ADMIN")}-{DateTime.UtcNow:yyyyMMddHHmmss}-{sku}",
                StockAdjustmentDate = DateTime.UtcNow,
                LocationId = resolvedLocationId.Value,
                Reason = "Admin approval",
                AdditionalInfo = createdNewProduct
                    ? $"SalesOrder approval stock increase for NEW SKU={sku}"
                    : $"SalesOrder approval stock increase for SKU={sku}",
                StockAdjustmentRows = new List<StockAdjustmentRowDto>
                {
                    new StockAdjustmentRowDto
                    {
                        VariantId = variantId.Value,
                        Quantity = quantity
                    }
                }
            };

            _logger.LogInformation("Katana stock adjustment create. Sku={Sku}, VariantId={VariantId}, LocationId={LocationId}, Qty={Qty}",
                sku, variantId, resolvedLocationId, quantity);

            var createdAdj = await CreateStockAdjustmentAsync(req);
            if (createdAdj == null)
            {
                _logger.LogWarning("SyncProductStockAsync: CreateStockAdjustmentAsync returned null. Sku={Sku}", sku);
                return false;
            }

            _logger.LogInformation("SyncProductStockAsync: stock adjustment created. Sku={Sku}, AdjustmentId={AdjustmentId}, Number={Number}",
                sku, createdAdj.Id, createdAdj.StockAdjustmentNumber);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SyncProductStockAsync failed. Sku={Sku}", sku);
            _loggingService.LogError($"Katana SyncProductStockAsync failed for {sku}", ex, null, "SyncProductStockAsync", LogCategory.ExternalAPI);
            return false;
        }
    }

    public async Task<List<KatanaProductDto>> GetProductsAsync()
    {
        var allProducts = new List<KatanaProductDto>();
        var page = 1;
        const int PAGE_SIZE = 100; // ✅ Katana MRP API maksimum limit (250 yerine 100)
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

                    // ✅ Sonsuz döngü önleme: Hem empty hem de count kontrolü
                    if (pageProducts.Count == 0 || pageProducts.Count < PAGE_SIZE)
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
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Katana API request timeout (page {Page}, total products: {Count}). Consider increasing TimeoutSeconds in appsettings.", page, allProducts.Count);
            _loggingService.LogError($"Katana API timeout at page {page}", ex, null, "GetProductsAsync", LogCategory.ExternalAPI);
            // Zaman aşımında bile önceki sayfalardan alınan ürünleri döndür
            return allProducts;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Katana API connection error: {Message}", ex.Message);
            _loggingService.LogError("Katana API connection failed", ex, null, "GetProductsAsync", LogCategory.ExternalAPI);
            return allProducts.Count > 0 ? allProducts : new List<KatanaProductDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling Katana API");
            _loggingService.LogError("Unexpected error in Katana API call", ex, null, "GetProductsAsync", LogCategory.ExternalAPI);
            return allProducts.Count > 0 ? allProducts : new List<KatanaProductDto>();
        }
    }

    public async Task<List<KatanaCustomerDto>> GetCustomersAsync()
    {
        var allCustomers = new List<KatanaCustomerDto>();
        var page = 1;
        const int PAGE_SIZE = 100; // ✅ Katana MRP API maksimum limit (250 yerine 100)
        var moreRecords = true;

        try
        {
            _logger.LogInformation("Getting customers from Katana with pagination (pageSize={PageSize})", PAGE_SIZE);
            _loggingService.LogInfo("Katana API: Fetching customers (paged)", null, "GetCustomersAsync", LogCategory.ExternalAPI);

            while (moreRecords)
            {
                var url = $"{_settings.Endpoints.Customers}?limit={PAGE_SIZE}&page={page}";
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
                    var pageCustomers = new List<KatanaCustomerDto>();
                    if (root.TryGetProperty("data", out var dataEl) && dataEl.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var custEl in dataEl.EnumerateArray())
                        {
                            var mapped = MapCustomerElement(custEl);
                            pageCustomers.Add(mapped);
                        }
                    }

                    _logger.LogInformation("Page {Page}: +{Count} customers (Total: {Total})", page, pageCustomers.Count, allCustomers.Count + pageCustomers.Count);
                    allCustomers.AddRange(pageCustomers);

                    // ✅ Sonsuz döngü önleme: Hem empty hem de count kontrolü
                    if (pageCustomers.Count == 0 || pageCustomers.Count < PAGE_SIZE)
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
                    _logger.LogError(ex, "Failed to parse Katana customers response on page {Page}", page);
                    moreRecords = false;
                }
            }

            _logger.LogInformation("✅ TOTAL {Count} customers loaded from Katana across {Pages} pages.", allCustomers.Count, page);
            _loggingService.LogInfo($"Successfully fetched {allCustomers.Count} customers from Katana ({page} pages)", null, "GetCustomersAsync", LogCategory.ExternalAPI);
            return allCustomers;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Katana API connection error: {Message}", ex.Message);
            _loggingService.LogError("Katana API connection failed", ex, null, "GetCustomersAsync", LogCategory.ExternalAPI);
            return new List<KatanaCustomerDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling Katana API");
            _loggingService.LogError("Unexpected error in Katana API call", ex, null, "GetCustomersAsync", LogCategory.ExternalAPI);
            return new List<KatanaCustomerDto>();
        }
    }

    private KatanaCustomerDto MapCustomerElement(JsonElement custEl)
    {
        var dto = new KatanaCustomerDto();

        // ID
        if (custEl.TryGetProperty("id", out var idEl) && idEl.ValueKind == JsonValueKind.Number)
        {
            try { dto.Id = idEl.GetInt32(); } catch { dto.Id = 0; }
        }

        // Basic fields - önce parçaları al
        dto.FirstName = custEl.TryGetProperty("first_name", out var fnEl) ? fnEl.GetString() : null;
        dto.LastName = custEl.TryGetProperty("last_name", out var lnEl) ? lnEl.GetString() : null;
        dto.Company = custEl.TryGetProperty("company", out var compEl) ? compEl.GetString() : null;
        dto.Email = custEl.TryGetProperty("email", out var emailEl) ? emailEl.GetString() : null;
        
        // ✅ NULL-SAFE NAME MAPPING: Fallback stratejisi
        dto.Name = custEl.TryGetProperty("name", out var nameEl) && nameEl.ValueKind == JsonValueKind.String
            ? nameEl.GetString() ?? string.Empty
            : string.Empty;
        
        // Eğer Name boşsa, Company veya FirstName+LastName kullan
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            if (!string.IsNullOrWhiteSpace(dto.Company))
            {
                dto.Name = dto.Company;
            }
            else if (!string.IsNullOrWhiteSpace(dto.FirstName) || !string.IsNullOrWhiteSpace(dto.LastName))
            {
                dto.Name = $"{dto.FirstName} {dto.LastName}".Trim();
            }
            else
            {
                dto.Name = $"UNNAMED_CUSTOMER_{dto.Id}";
                _logger.LogWarning("Customer {Id} has no name, company, or full name - using fallback", dto.Id);
            }
        }
        
        // 🔥 DEBUG: Katana API'sinden gelen customer alanını logla
        _logger.LogDebug("🔍 Katana API Response - Customer ID: {Id}, Name: '{Name}', Email: '{Email}'", dto.Id, dto.Name, dto.Email ?? "N/A");
        
        dto.Phone = custEl.TryGetProperty("phone", out var phoneEl) ? phoneEl.GetString() : null;
        dto.Comment = custEl.TryGetProperty("comment", out var commentEl) ? commentEl.GetString() : null;
        dto.Currency = custEl.TryGetProperty("currency", out var currEl) ? currEl.GetString() ?? "TRY" : "TRY";
        dto.ReferenceId = custEl.TryGetProperty("reference_id", out var refEl) ? refEl.GetString() : null;
        dto.Category = custEl.TryGetProperty("category", out var catEl) ? catEl.GetString() : null;

        // Decimal fields
        if (custEl.TryGetProperty("discount_rate", out var discEl))
            dto.DiscountRate = ReadDecimalProperty(custEl, "discount_rate");

        // Default address IDs
        if (custEl.TryGetProperty("default_billing_id", out var billEl) && billEl.ValueKind == JsonValueKind.Number)
        {
            try { dto.DefaultBillingId = billEl.GetInt32(); } catch { }
        }
        if (custEl.TryGetProperty("default_shipping_id", out var shipEl) && shipEl.ValueKind == JsonValueKind.Number)
        {
            try { dto.DefaultShippingId = shipEl.GetInt32(); } catch { }
        }

        // Timestamps
        if (custEl.TryGetProperty("created_at", out var createdEl) && createdEl.ValueKind == JsonValueKind.String)
        {
            if (DateTime.TryParse(createdEl.GetString(), out var createdDate))
                dto.CreatedAt = createdDate;
        }
        if (custEl.TryGetProperty("updated_at", out var updatedEl) && updatedEl.ValueKind == JsonValueKind.String)
        {
            if (DateTime.TryParse(updatedEl.GetString(), out var updatedDate))
                dto.UpdatedAt = updatedDate;
        }

        // Addresses
        if (custEl.TryGetProperty("addresses", out var addrsEl) && addrsEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var addrEl in addrsEl.EnumerateArray())
            {
                var address = MapCustomerAddressElement(addrEl);
                dto.Addresses.Add(address);
            }
        }

        return dto;
    }

    private KatanaCustomerAddressDto MapCustomerAddressElement(JsonElement addrEl)
    {
        var dto = new KatanaCustomerAddressDto();

        if (addrEl.TryGetProperty("id", out var idEl) && idEl.ValueKind == JsonValueKind.Number)
        {
            try { dto.Id = idEl.GetInt32(); } catch { dto.Id = 0; }
        }
        if (addrEl.TryGetProperty("customer_id", out var custIdEl) && custIdEl.ValueKind == JsonValueKind.Number)
        {
            try { dto.CustomerId = custIdEl.GetInt32(); } catch { dto.CustomerId = 0; }
        }

        dto.EntityType = addrEl.TryGetProperty("entity_type", out var etEl) ? etEl.GetString() ?? string.Empty : string.Empty;
        
        if (addrEl.TryGetProperty("default", out var defEl) && defEl.ValueKind == JsonValueKind.True)
            dto.Default = true;

        dto.FirstName = addrEl.TryGetProperty("first_name", out var fnEl) ? fnEl.GetString() : null;
        dto.LastName = addrEl.TryGetProperty("last_name", out var lnEl) ? lnEl.GetString() : null;
        dto.Company = addrEl.TryGetProperty("company", out var compEl) ? compEl.GetString() : null;
        dto.Phone = addrEl.TryGetProperty("phone", out var phoneEl) ? phoneEl.GetString() : null;
        dto.Line1 = addrEl.TryGetProperty("line1", out var l1El) ? l1El.GetString() : null;
        dto.Line2 = addrEl.TryGetProperty("line2", out var l2El) ? l2El.GetString() : null;
        dto.City = addrEl.TryGetProperty("city", out var cityEl) ? cityEl.GetString() : null;
        dto.State = addrEl.TryGetProperty("state", out var stateEl) ? stateEl.GetString() : null;
        dto.Zip = addrEl.TryGetProperty("zip", out var zipEl) ? zipEl.GetString() : null;
        dto.Country = addrEl.TryGetProperty("country", out var countryEl) ? countryEl.GetString() : null;

        if (addrEl.TryGetProperty("created_at", out var createdEl) && createdEl.ValueKind == JsonValueKind.String)
        {
            if (DateTime.TryParse(createdEl.GetString(), out var createdDate))
                dto.CreatedAt = createdDate;
        }
        if (addrEl.TryGetProperty("updated_at", out var updatedEl) && updatedEl.ValueKind == JsonValueKind.String)
        {
            if (DateTime.TryParse(updatedEl.GetString(), out var updatedDate))
                dto.UpdatedAt = updatedDate;
        }

        return dto;
    }

    public async Task<KatanaCustomerDto?> GetCustomerByIdAsync(int customerId)
    {
        if (customerId <= 0)
        {
            _logger.LogError("GetCustomerByIdAsync called with invalid customerId: {CustomerId}", customerId);
            return null;
        }

        var cacheKey = $"customer-{customerId}";
        if (_cache.TryGetValue<KatanaCustomerDto?>(cacheKey, out var cached))
        {
            _logger.LogDebug("Retrieved customer {CustomerId} from cache", customerId);
            return cached;
        }

        try
        {
            _logger.LogInformation("Getting customer by ID from Katana: {CustomerId}", customerId);
            
            var response = await _httpClient.GetAsync($"{_settings.Endpoints.Customers}/{customerId}");
            
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Customer not found in Katana: {CustomerId}", customerId);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Failed to get customer {CustomerId} from Katana. Status: {Status}, Error: {Error}", 
                        customerId, response.StatusCode, errorContent);
                }
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;
            
            JsonElement customerElement = root;
            if (root.TryGetProperty("data", out var wrapped) && wrapped.ValueKind == JsonValueKind.Object)
                customerElement = wrapped;

            var mapped = MapCustomerElement(customerElement);
            
            // Cache for 5 minutes
            _cache.Set(cacheKey, mapped, TimeSpan.FromMinutes(5));
            
            _logger.LogInformation("Retrieved customer {CustomerId} from Katana: {Name}", customerId, mapped?.Name);
            return mapped;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Katana API connection error for customer ID {CustomerId}", customerId);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse customer response for ID {CustomerId}", customerId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting customer by ID {CustomerId}", customerId);
            return null;
        }
    }

    public async Task<KatanaCustomerDto?> CreateCustomerAsync(KatanaCustomerDto customer)
    {
        try
        {
            // Validate input
            if (customer == null)
            {
                _logger.LogError("CreateCustomerAsync called with null customer");
                return null;
            }

            if (string.IsNullOrWhiteSpace(customer.Name))
            {
                _logger.LogError("CreateCustomerAsync: Customer name is required");
                return null;
            }

            _logger.LogInformation("Creating customer in Katana: {CustomerName}", customer.Name);
            _loggingService.LogInfo($"Creating customer: {customer.Name}", null, "CreateCustomerAsync", LogCategory.ExternalAPI);

            // Build payload matching Katana API specification
            var payload = new Dictionary<string, object?>
            {
                { "name", customer.Name }
            };

            // Add optional fields
            if (!string.IsNullOrWhiteSpace(customer.FirstName))
                payload["first_name"] = customer.FirstName;

            if (!string.IsNullOrWhiteSpace(customer.LastName))
                payload["last_name"] = customer.LastName;

            if (!string.IsNullOrWhiteSpace(customer.Company))
                payload["company"] = customer.Company;

            if (!string.IsNullOrWhiteSpace(customer.Email))
                payload["email"] = customer.Email;

            if (!string.IsNullOrWhiteSpace(customer.Phone))
                payload["phone"] = customer.Phone;

            if (!string.IsNullOrWhiteSpace(customer.Currency))
                payload["currency"] = customer.Currency;
            else
                payload["currency"] = "TRY"; // Default currency

            if (!string.IsNullOrWhiteSpace(customer.ReferenceId))
                payload["reference_id"] = customer.ReferenceId;

            if (!string.IsNullOrWhiteSpace(customer.Category))
                payload["category"] = customer.Category;

            if (!string.IsNullOrWhiteSpace(customer.Comment))
                payload["comment"] = customer.Comment;

            if (customer.DiscountRate.HasValue)
                payload["discount_rate"] = customer.DiscountRate.Value;

            // Add addresses if present
            if (customer.Addresses != null && customer.Addresses.Count > 0)
            {
                var addresses = customer.Addresses.Select(addr => new Dictionary<string, object?>
                {
                    { "entity_type", addr.EntityType },
                    { "default", addr.Default },
                    { "first_name", addr.FirstName },
                    { "last_name", addr.LastName },
                    { "company", addr.Company },
                    { "phone", addr.Phone },
                    { "line1", addr.Line1 },
                    { "line2", addr.Line2 },
                    { "city", addr.City },
                    { "state", addr.State },
                    { "zip", addr.Zip },
                    { "country", addr.Country }
                }).ToList();

                payload["addresses"] = addresses;
            }

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            _logger.LogDebug("CreateCustomerAsync payload: {Payload}", json);

            var content = CreateKatanaJsonContent(json);
            var response = await _httpClient.PostAsync(_settings.Endpoints.Customers, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                // Handle 422 validation errors specifically
                if ((int)response.StatusCode == 422)
                {
                    _logger.LogError("CreateCustomerAsync validation error. Status: {StatusCode}, Response: {Response}", 
                        response.StatusCode, responseContent);
                    _loggingService.LogError("Customer creation validation failed", null, responseContent, "CreateCustomerAsync", LogCategory.ExternalAPI);
                }
                else
                {
                    _logger.LogError("CreateCustomerAsync failed. Status: {StatusCode}, Response: {Response}", 
                        response.StatusCode, responseContent);
                    _loggingService.LogError($"Customer creation failed: {response.StatusCode}", null, responseContent, "CreateCustomerAsync", LogCategory.ExternalAPI);
                }
                return null;
            }

            _logger.LogInformation("Customer created successfully in Katana: {CustomerName}", customer.Name);
            _loggingService.LogInfo($"Customer created successfully: {customer.Name}", null, "CreateCustomerAsync", LogCategory.ExternalAPI);
            
            // Parse and return the created customer
            using var doc = JsonDocument.Parse(responseContent);
            var root = doc.RootElement;
            
            JsonElement customerElement = root;
            if (root.TryGetProperty("data", out var wrapped) && wrapped.ValueKind == JsonValueKind.Object)
                customerElement = wrapped;

            var createdCustomer = MapCustomerElement(customerElement);
            return createdCustomer;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse customer response. Customer: {CustomerName}", customer?.Name ?? "Unknown");
            _loggingService.LogError("JSON parsing error in customer creation", ex, null, "CreateCustomerAsync", LogCategory.ExternalAPI);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Katana API connection error creating customer. Customer: {CustomerName}", customer?.Name ?? "Unknown");
            _loggingService.LogError("Connection error in customer creation", ex, null, "CreateCustomerAsync", LogCategory.ExternalAPI);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer in Katana API. Customer: {CustomerName}", customer?.Name ?? "Unknown");
            _loggingService.LogError("Unexpected error in customer creation", ex, null, "CreateCustomerAsync", LogCategory.ExternalAPI);
            return null;
        }
    }

    public async Task<KatanaCustomerDto?> UpdateCustomerAsync(int customerId, KatanaCustomerDto customer)
    {
        try
        {
            if (customerId <= 0)
            {
                _logger.LogError("UpdateCustomerAsync called with invalid customerId: {CustomerId}", customerId);
                return null;
            }

            if (customer == null)
            {
                _logger.LogError("UpdateCustomerAsync called with null customer for customerId: {CustomerId}", customerId);
                return null;
            }

            _logger.LogInformation("Updating customer in Katana. CustomerId: {CustomerId}, CustomerName: {CustomerName}", customerId, customer.Name);
            _loggingService.LogInfo($"Updating customer {customerId}: {customer.Name}", null, "UpdateCustomerAsync", LogCategory.ExternalAPI);

            // Build payload with all fields (Katana PATCH accepts full payload)
            var payload = new Dictionary<string, object?>();

            // Add fields if they are not null/empty
            if (!string.IsNullOrWhiteSpace(customer.Name))
                payload["name"] = customer.Name;

            if (!string.IsNullOrWhiteSpace(customer.FirstName))
                payload["first_name"] = customer.FirstName;

            if (!string.IsNullOrWhiteSpace(customer.LastName))
                payload["last_name"] = customer.LastName;

            if (!string.IsNullOrWhiteSpace(customer.Company))
                payload["company"] = customer.Company;

            if (!string.IsNullOrWhiteSpace(customer.Email))
                payload["email"] = customer.Email;

            if (!string.IsNullOrWhiteSpace(customer.Phone))
                payload["phone"] = customer.Phone;

            if (!string.IsNullOrWhiteSpace(customer.Currency))
                payload["currency"] = customer.Currency;

            if (!string.IsNullOrWhiteSpace(customer.ReferenceId))
                payload["reference_id"] = customer.ReferenceId;

            if (!string.IsNullOrWhiteSpace(customer.Category))
                payload["category"] = customer.Category;

            if (!string.IsNullOrWhiteSpace(customer.Comment))
                payload["comment"] = customer.Comment;

            if (customer.DiscountRate.HasValue)
                payload["discount_rate"] = customer.DiscountRate.Value;

            if (customer.DefaultBillingId.HasValue)
                payload["default_billing_id"] = customer.DefaultBillingId.Value;

            if (customer.DefaultShippingId.HasValue)
                payload["default_shipping_id"] = customer.DefaultShippingId.Value;

            // Add addresses if present
            if (customer.Addresses != null && customer.Addresses.Count > 0)
            {
                var addresses = customer.Addresses.Select(addr => new Dictionary<string, object?>
                {
                    { "entity_type", addr.EntityType },
                    { "default", addr.Default },
                    { "first_name", addr.FirstName },
                    { "last_name", addr.LastName },
                    { "company", addr.Company },
                    { "phone", addr.Phone },
                    { "line1", addr.Line1 },
                    { "line2", addr.Line2 },
                    { "city", addr.City },
                    { "state", addr.State },
                    { "zip", addr.Zip },
                    { "country", addr.Country }
                }).ToList();

                payload["addresses"] = addresses;
            }

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            _logger.LogDebug("UpdateCustomerAsync payload for CustomerId {CustomerId}: {Payload}", customerId, json);

            var content = CreateKatanaJsonContent(json);
            var updateUrl = $"{_settings.Endpoints.Customers}/{customerId}";
            var response = await _httpClient.PatchAsync(updateUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Customer not found for update: {CustomerId}", customerId);
                    _loggingService.LogWarning($"Customer not found: {customerId}", null, "UpdateCustomerAsync", LogCategory.ExternalAPI);
                }
                else if ((int)response.StatusCode == 422)
                {
                    _logger.LogError("UpdateCustomerAsync validation error for CustomerId: {CustomerId}. Status: {StatusCode}, Response: {Response}", 
                        customerId, response.StatusCode, responseContent);
                    _loggingService.LogError("Customer update validation failed", null, responseContent, "UpdateCustomerAsync", LogCategory.ExternalAPI);
                }
                else
                {
                    _logger.LogError("UpdateCustomerAsync failed for CustomerId: {CustomerId}. Status: {StatusCode}, Response: {Response}", 
                        customerId, response.StatusCode, responseContent);
                    _loggingService.LogError($"Customer update failed: {response.StatusCode}", null, responseContent, "UpdateCustomerAsync", LogCategory.ExternalAPI);
                }
                return null;
            }

            // Invalidate cache
            _cache.Remove($"customer-{customerId}");

            _logger.LogInformation("Customer updated successfully in Katana. CustomerId: {CustomerId}, CustomerName: {CustomerName}", customerId, customer.Name);
            _loggingService.LogInfo($"Customer updated successfully: {customerId}", null, "UpdateCustomerAsync", LogCategory.ExternalAPI);
            
            // Parse and return the updated customer
            using var doc = JsonDocument.Parse(responseContent);
            var root = doc.RootElement;
            
            JsonElement customerElement = root;
            if (root.TryGetProperty("data", out var wrapped) && wrapped.ValueKind == JsonValueKind.Object)
                customerElement = wrapped;

            var updatedCustomer = MapCustomerElement(customerElement);
            return updatedCustomer;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse customer response. CustomerId: {CustomerId}", customerId);
            _loggingService.LogError("JSON parsing error in customer update", ex, null, "UpdateCustomerAsync", LogCategory.ExternalAPI);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Katana API connection error updating customer. CustomerId: {CustomerId}", customerId);
            _loggingService.LogError("Connection error in customer update", ex, null, "UpdateCustomerAsync", LogCategory.ExternalAPI);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer in Katana API. CustomerId: {CustomerId}, Customer: {CustomerName}", customerId, customer?.Name ?? "Unknown");
            _loggingService.LogError("Unexpected error in customer update", ex, null, "UpdateCustomerAsync", LogCategory.ExternalAPI);
            return null;
        }
    }

    public async Task<bool> DeleteCustomerAsync(int customerId)
    {
        try
        {
            if (customerId <= 0)
            {
                _logger.LogError("DeleteCustomerAsync called with invalid customerId: {CustomerId}", customerId);
                return false;
            }

            _logger.LogInformation("Deleting customer from Katana: {CustomerId}", customerId);
            _loggingService.LogInfo($"Deleting customer: {customerId}", null, "DeleteCustomerAsync", LogCategory.ExternalAPI);

            var response = await _httpClient.DeleteAsync($"{_settings.Endpoints.Customers}/{customerId}");

            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                _logger.LogInformation("Successfully deleted customer: {CustomerId}", customerId);
                _loggingService.LogInfo($"Customer deleted successfully: {customerId}", null, "DeleteCustomerAsync", LogCategory.ExternalAPI);
                
                // Clear cache
                _cache.Remove($"customer-{customerId}");
                return true;
            }

            // Handle specific error cases
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Customer not found for deletion: {CustomerId}", customerId);
                _loggingService.LogWarning($"Customer not found for deletion: {customerId}", null, "DeleteCustomerAsync", LogCategory.ExternalAPI);
                return false;
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogError("Unauthorized to delete customer: {CustomerId}", customerId);
                _loggingService.LogError("Unauthorized to delete customer", null, null, "DeleteCustomerAsync", LogCategory.ExternalAPI);
                return false;
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to delete customer: {CustomerId}. Status: {StatusCode}, Response: {Response}", 
                customerId, response.StatusCode, errorContent);
            _loggingService.LogError($"Customer deletion failed: {response.StatusCode}", null, errorContent, "DeleteCustomerAsync", LogCategory.ExternalAPI);
            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Katana API connection error deleting customer: {CustomerId}", customerId);
            _loggingService.LogError("Connection error in customer deletion", ex, null, "DeleteCustomerAsync", LogCategory.ExternalAPI);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting customer from Katana API. CustomerId: {CustomerId}", customerId);
            _loggingService.LogError("Unexpected error in customer deletion", ex, null, "DeleteCustomerAsync", LogCategory.ExternalAPI);
            return false;
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
        
        // 🔥 DEBUG: Katana API'sinden gelen name alanını logla
        if (!string.IsNullOrWhiteSpace(dto.Name))
        {
            _logger.LogDebug("🔍 Katana API Response - ID: {Id}, Name: '{Name}'", dto.Id, dto.Name);
        }

        
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
                {
                    dto.SKU = skuEl.GetString() ?? string.Empty;
                    // 🔥 DEBUG: Variant'tan SKU alındı, Name değişmedi
                    _logger.LogDebug("🔍 Variant SKU atandı - SKU: '{SKU}', Name: '{Name}' (değişmedi)", dto.SKU, dto.Name);
                }

                
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

    
    public async Task<string?> GetVariantSkuAsync(long variantId)
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
            
            // Katana API is case-sensitive, try original SKU first, then lowercase
            var skuVariants = new[] { sku, sku.ToLowerInvariant(), sku.ToUpperInvariant() }.Distinct();
            
            foreach (var skuVariant in skuVariants)
            {
                var variantUrl = $"{_settings.Endpoints.Variants}?sku={Uri.EscapeDataString(skuVariant)}";
                var varResp = await _httpClient.GetAsync(variantUrl);
                var varContent = await varResp.Content.ReadAsStringAsync();

                if (!varResp.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Katana API variant lookup failed for SKU {SKU}. Status: {Status}", skuVariant, varResp.StatusCode);
                    continue;
                }

                try
                {
                    using var doc = JsonDocument.Parse(varContent);
                    var root = doc.RootElement;
                    if (!root.TryGetProperty("data", out var dataEl) || dataEl.ValueKind != JsonValueKind.Array)
                        continue;

                    var firstVar = dataEl.EnumerateArray().FirstOrDefault();
                    if (firstVar.ValueKind == JsonValueKind.Undefined || firstVar.ValueKind == JsonValueKind.Null)
                        continue;
                    
                    // Found a match - proceed with this variant
                    _logger.LogInformation("Found product in Katana API with SKU variant: {SKUVariant} (original: {OriginalSKU})", skuVariant, sku);

                    if (!firstVar.TryGetProperty("product_id", out var pidEl))
                        continue;

                    int productId = 0;
                    try { productId = pidEl.GetInt32(); } catch { continue; }

                    var productResp = await _httpClient.GetAsync($"{_settings.Endpoints.Products}/{productId}");
                    var productContent = await productResp.Content.ReadAsStringAsync();
                    if (!productResp.IsSuccessStatusCode)
                    {
                        _logger.LogWarning("Katana API product fetch failed for id {Id}. Status: {Status}", productId, productResp.StatusCode);
                        continue;
                    }

                    using var prodDoc = JsonDocument.Parse(productContent);
                    var prodRoot = prodDoc.RootElement;
                    
                    JsonElement productElement = prodRoot;
                    if (prodRoot.TryGetProperty("data", out var wrapped) && wrapped.ValueKind == JsonValueKind.Object)
                        productElement = wrapped;

                    var mapped = MapProductElement(productElement);
                    _logger.LogInformation("Retrieved product {SKU} from Katana", sku);
                    return mapped;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error parsing variant response for SKU variant {SKUVariant}", skuVariant);
                    continue;
                }
            }
            
            // No variant found with any case
            _logger.LogWarning("Product not found in Katana API for SKU {SKU} (tried original, lowercase, uppercase)", sku);
            return null;
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

    public async Task<KatanaProductDto?> GetProductByIdAsync(int productId)
    {
        if (productId <= 0)
        {
            _logger.LogError("GetProductByIdAsync called with invalid productId: {ProductId}", productId);
            return null;
        }

        var cacheKey = $"product-{productId}";
        if (_cache.TryGetValue<KatanaProductDto?>(cacheKey, out var cached))
        {
            _logger.LogDebug("Retrieved product {ProductId} from cache", productId);
            return cached;
        }

        try
        {
            _logger.LogInformation("Getting product by ID from Katana: {ProductId}", productId);
            
            var response = await _httpClient.GetAsync($"{_settings.Endpoints.Products}/{productId}");
            
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Product not found in Katana: {ProductId}", productId);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Failed to get product {ProductId} from Katana. Status: {Status}, Error: {Error}", 
                        productId, response.StatusCode, errorContent);
                }
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;
            
            JsonElement productElement = root;
            if (root.TryGetProperty("data", out var wrapped) && wrapped.ValueKind == JsonValueKind.Object)
                productElement = wrapped;

            var mapped = MapProductElement(productElement);
            
            // Cache for 5 minutes
            _cache.Set(cacheKey, mapped, TimeSpan.FromMinutes(5));
            
            _logger.LogInformation("Retrieved product {ProductId} from Katana: {SKU}", productId, mapped?.SKU);
            return mapped;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Katana API connection error for product ID {ProductId}", productId);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse product response for ID {ProductId}", productId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting product by ID {ProductId}", productId);
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
            var content = CreateKatanaJsonContent(json);

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

    /// <summary>
    /// Creates a new product in Katana API
    /// 
    /// Payload structure:
    /// {
    ///   "name": "Product Name",           // Required
    ///   "uom": "pcs",                     // Required
    ///   "category_name": "Category",      // Optional
    ///   "is_sellable": true,              // Optional, default true
    ///   "is_producible": false,           // Optional
    ///   "is_purchasable": true,           // Optional
    ///   "variants": [                     // Required, at least 1
    ///     {
    ///       "sku": "PROD-001",
    ///       "sales_price": 100.00,
    ///       "purchase_price": 50.00
    ///     }
    ///   ]
    /// }
    /// </summary>
    public async Task<KatanaProductDto?> CreateProductAsync(KatanaProductDto product)
    {
        try
        {
            // Validate input
            if (product == null)
            {
                _logger.LogError("CreateProductAsync called with null product");
                return null;
            }

            if (string.IsNullOrWhiteSpace(product.Name))
            {
                _logger.LogError("CreateProductAsync: Product name is required");
                return null;
            }

            if (string.IsNullOrWhiteSpace(product.Unit))
            {
                _logger.LogError("CreateProductAsync: Product unit (uom) is required");
                return null;
            }

            if (string.IsNullOrWhiteSpace(product.SKU))
            {
                _logger.LogError("CreateProductAsync: Product SKU is required");
                return null;
            }

            _logger.LogInformation("Creating product in Katana: {ProductName} (SKU: {SKU})", product.Name, product.SKU);

            // Build payload matching Katana API specification
            var variants = new List<object>
            {
                new
                {
                    sku = product.SKU,
                    sales_price = product.SalesPrice ?? product.Price,
                    purchase_price = product.PurchasePrice ?? 0
                }
            };

            var payload = new Dictionary<string, object?>
            {
                { "name", product.Name },
                { "uom", product.Unit },
                { "is_sellable", true },
                { "is_producible", false },
                { "is_purchasable", true },
                { "variants", variants }
            };

            // Add optional fields
            if (!string.IsNullOrWhiteSpace(product.Category))
            {
                payload["category_name"] = product.Category;
            }

            if (!string.IsNullOrWhiteSpace(product.Description))
            {
                payload["description"] = product.Description;
            }

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            _logger.LogDebug("CreateProductAsync payload: {Payload}", json);

            var content = CreateKatanaJsonContent(json);
            var response = await _httpClient.PostAsync(_settings.Endpoints.Products, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                // Handle 422 validation errors specifically
                if ((int)response.StatusCode == 422)
                {
                    _logger.LogError("CreateProductAsync validation error. Status: {StatusCode}, Response: {Response}", 
                        response.StatusCode, responseContent);
                }
                else
                {
                    _logger.LogError("CreateProductAsync failed. Status: {StatusCode}, Response: {Response}", 
                        response.StatusCode, responseContent);
                }
                return null;
            }

            _logger.LogInformation("Product created successfully in Katana: {ProductName} (SKU: {SKU})", product.Name, product.SKU);
            
            // Parse and return the created product
            var createdProduct = JsonSerializer.Deserialize<KatanaProductDto>(responseContent, _jsonOptions);
            return createdProduct;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product in Katana API. Product: {ProductName}", product?.Name ?? "Unknown");
            return null;
        }
    }

    /// <summary>
    /// Deletes a product from Katana API
    /// </summary>
    /// <param name="productId">The Katana product ID to delete</param>
    /// <returns>True if deletion successful (204 No Content), false otherwise</returns>
    public async Task<bool> DeleteProductAsync(int productId)
    {
        try
        {
            if (productId <= 0)
            {
                _logger.LogError("DeleteProductAsync called with invalid productId: {ProductId}", productId);
                return false;
            }

            _logger.LogInformation("Deleting product from Katana: {ProductId}", productId);

            var response = await _httpClient.DeleteAsync($"{_settings.Endpoints.Products}/{productId}");

            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                _logger.LogInformation("Successfully deleted product: {ProductId}", productId);
                return true;
            }

            // Handle specific error cases
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Product not found for deletion: {ProductId}", productId);
                return false;
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogError("Unauthorized to delete product: {ProductId}", productId);
                return false;
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to delete product: {ProductId}. Status: {StatusCode}, Response: {Response}", 
                productId, response.StatusCode, errorContent);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product from Katana API. ProductId: {ProductId}", productId);
            return false;
        }
    }

    public async Task<List<KatanaPurchaseOrderDto>> GetPurchaseOrdersAsync(string? status = null, DateTime? fromDate = null)
    {
        try
        {
            var allOrders = new List<KatanaPurchaseOrderDto>();
            var page = 1;
            const int PAGE_SIZE = 100;
            var moreRecords = true;

            // ✅ Status mapping: null/"all" → param gönderme, "open" → open, "done" → done
            string? mappedStatus = null;
            if (!string.IsNullOrEmpty(status) && 
                !status.Equals("all", StringComparison.OrdinalIgnoreCase) &&
                !status.Equals("tümü", StringComparison.OrdinalIgnoreCase))
            {
                mappedStatus = status.ToLowerInvariant();
            }

            _logger.LogInformation("🔄 Katana'dan purchase orders çekiliyor (status: {Status}, fromDate: {FromDate})", 
                mappedStatus ?? "all", fromDate?.ToString("yyyy-MM-dd") ?? "none");

            while (moreRecords)
            {
                var queryParams = new List<string>
                {
                    $"page={page}",
                    $"limit={PAGE_SIZE}"
                };

                // Status filtresi ekle (varsa)
                if (!string.IsNullOrEmpty(mappedStatus))
                {
                    queryParams.Add($"status={mappedStatus}");
                }

                // Tarih filtresi ekle (varsa) - ama dikkatli, çok kısıtlayıcı olmasın
                if (fromDate.HasValue)
                {
                    var dateStr = fromDate.Value.ToString("yyyy-MM-dd");
                    queryParams.Add($"created_at_from={dateStr}");
                }

                var queryString = string.Join("&", queryParams);
                var url = $"/purchase-orders?{queryString}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Katana purchase orders API returned {StatusCode} for page {Page}", 
                        response.StatusCode, page);
                    break;
                }

                var content = await response.Content.ReadAsStringAsync();
                
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;
                var pageOrders = new List<KatanaPurchaseOrderDto>();

                if (root.TryGetProperty("data", out var dataEl) && dataEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var poEl in dataEl.EnumerateArray())
                    {
                        var id = poEl.TryGetProperty("id", out var idEl) ? idEl.ToString() : string.Empty;
                        var statusVal = poEl.TryGetProperty("status", out var statusEl) ? statusEl.GetString() : "unknown";
                        var createdAt = poEl.TryGetProperty("created_at", out var createdEl) && 
                                       DateTime.TryParse(createdEl.GetString(), out var dt) ? dt : DateTime.UtcNow;
                        var supplierId = poEl.TryGetProperty("supplier_id", out var supplierEl) ? supplierEl.ToString() : string.Empty;

                        var items = new List<KatanaPurchaseOrderItemDto>();
                        if (poEl.TryGetProperty("rows", out var rowsEl) && rowsEl.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var rowEl in rowsEl.EnumerateArray())
                            {
                                var sku = rowEl.TryGetProperty("variant_sku", out var skuEl) ? skuEl.GetString() : string.Empty;
                                var qty = rowEl.TryGetProperty("quantity", out var qtyEl) && qtyEl.TryGetDecimal(out var q) ? (int)q : 0;
                                var price = rowEl.TryGetProperty("unit_price", out var priceEl) && priceEl.TryGetDecimal(out var p) ? p : 0;

                                items.Add(new KatanaPurchaseOrderItemDto
                                {
                                    ProductSKU = sku ?? string.Empty,
                                    Quantity = qty,
                                    UnitPrice = price,
                                    TotalAmount = qty * price
                                });
                            }
                        }

                        pageOrders.Add(new KatanaPurchaseOrderDto
                        {
                            Id = id,
                            Status = statusVal ?? "unknown",
                            OrderDate = createdAt,
                            SupplierCode = supplierId,
                            Items = items
                        });
                    }
                }

                _logger.LogInformation("Page {Page}: +{Count} purchase orders (Total: {Total})", 
                    page, pageOrders.Count, allOrders.Count + pageOrders.Count);
                
                allOrders.AddRange(pageOrders);

                if (pageOrders.Count == 0 || pageOrders.Count < PAGE_SIZE)
                {
                    moreRecords = false;
                }
                else
                {
                    page++;
                    await Task.Delay(50); // Rate limit
                }
            }

            _logger.LogInformation("✅ Katana'dan {Count} purchase order çekildi (status: {Status})", 
                allOrders.Count, mappedStatus ?? "all");
            
            return allOrders;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Katana purchase orders çekme hatası");
            return new List<KatanaPurchaseOrderDto>();
        }
    }

    public async Task<KatanaPurchaseOrderDto?> GetPurchaseOrderByIdAsync(string id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/purchase-orders/{id}");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Katana purchase order {Id} bulunamadı: {StatusCode}", id, response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            if (!root.TryGetProperty("data", out var dataEl))
            {
                return null;
            }

            var poId = dataEl.TryGetProperty("id", out var idEl) ? idEl.ToString() : string.Empty;
            var statusVal = dataEl.TryGetProperty("status", out var statusEl) ? statusEl.GetString() : "unknown";
            var createdAt = dataEl.TryGetProperty("created_at", out var createdEl) && 
                           DateTime.TryParse(createdEl.GetString(), out var dt) ? dt : DateTime.UtcNow;
            var supplierId = dataEl.TryGetProperty("supplier_id", out var supplierEl) ? supplierEl.ToString() : string.Empty;

            var items = new List<KatanaPurchaseOrderItemDto>();
            if (dataEl.TryGetProperty("rows", out var rowsEl) && rowsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var rowEl in rowsEl.EnumerateArray())
                {
                    var sku = rowEl.TryGetProperty("variant_sku", out var skuEl) ? skuEl.GetString() : string.Empty;
                    var qty = rowEl.TryGetProperty("quantity", out var qtyEl) && qtyEl.TryGetDecimal(out var q) ? (int)q : 0;
                    var price = rowEl.TryGetProperty("unit_price", out var priceEl) && priceEl.TryGetDecimal(out var p) ? p : 0;

                    items.Add(new KatanaPurchaseOrderItemDto
                    {
                        ProductSKU = sku ?? string.Empty,
                        Quantity = qty,
                        UnitPrice = price,
                        TotalAmount = qty * price
                    });
                }
            }

            return new KatanaPurchaseOrderDto
            {
                Id = poId,
                Status = statusVal ?? "unknown",
                OrderDate = createdAt,
                SupplierCode = supplierId,
                Items = items
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Katana purchase order {Id} çekme hatası", id);
            return null;
        }
    }

    public async Task<string?> ReceivePurchaseOrderAsync(string id)
    {
        try
        {
            var request = new { purchase_order_id = id };
            var jsonContent = JsonSerializer.Serialize(request, _jsonOptions);
            var httpContent = CreateKatanaJsonContent(jsonContent);

            var response = await _httpClient.PostAsync("/purchase-orders/receive", httpContent);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Katana purchase order {Id} receive failed: {StatusCode}, {Error}", 
                    id, response.StatusCode, errorContent);
                return null;
            }

            _logger.LogInformation("✅ Katana purchase order {Id} received", id);
            return id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Katana purchase order {Id} receive hatası", id);
            return null;
        }
    }

    public async Task<List<KatanaSupplierDto>> GetSuppliersAsync()
    {
        var allSuppliers = new List<KatanaSupplierDto>();
        var page = 1;
        const int PAGE_SIZE = 100; // ✅ Katana MRP API maksimum limit (250 yerine 100)
        var moreRecords = true;

        try
        {
            _logger.LogInformation("Getting suppliers from Katana with pagination (pageSize={PageSize})", PAGE_SIZE);
            _loggingService.LogInfo("Katana API: Fetching suppliers (paged)", null, "GetSuppliersAsync", LogCategory.ExternalAPI);

            while (moreRecords)
            {
                var url = $"{_settings.Endpoints.Suppliers}?limit={PAGE_SIZE}&page={page}";
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
                    var pageSuppliers = new List<KatanaSupplierDto>();
                    if (root.TryGetProperty("data", out var dataEl) && dataEl.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var supplierEl in dataEl.EnumerateArray())
                        {
                            var mapped = MapSupplierElement(supplierEl);
                            pageSuppliers.Add(mapped);
                        }
                    }

                    _logger.LogInformation("Page {Page}: +{Count} suppliers (Total: {Total})", page, pageSuppliers.Count, allSuppliers.Count + pageSuppliers.Count);
                    allSuppliers.AddRange(pageSuppliers);

                    // ✅ Sonsuz döngü önleme: Hem empty hem de count kontrolü
                    if (pageSuppliers.Count == 0 || pageSuppliers.Count < PAGE_SIZE)
                    {
                        moreRecords = false;
                    }
                    else
                    {
                        page++;
                        await Task.Delay(50);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to parse Katana suppliers response on page {Page}", page);
                    moreRecords = false;
                }
            }

            _logger.LogInformation("✅ TOTAL {Count} suppliers loaded from Katana across {Pages} pages.", allSuppliers.Count, page);
            _loggingService.LogInfo($"Successfully fetched {allSuppliers.Count} suppliers from Katana ({page} pages)", null, "GetSuppliersAsync", LogCategory.ExternalAPI);
            return allSuppliers;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Katana API connection error: {Message}", ex.Message);
            _loggingService.LogError("Katana API connection failed", ex, null, "GetSuppliersAsync", LogCategory.ExternalAPI);
            return new List<KatanaSupplierDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling Katana API");
            _loggingService.LogError("Unexpected error in Katana API call", ex, null, "GetSuppliersAsync", LogCategory.ExternalAPI);
            return new List<KatanaSupplierDto>();
        }
    }

    public async Task<KatanaSupplierDto?> GetSupplierByIdAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            _logger.LogError("GetSupplierByIdAsync called with invalid id");
            return null;
        }

        var cacheKey = $"supplier-{id}";
        if (_cache.TryGetValue<KatanaSupplierDto?>(cacheKey, out var cached))
        {
            _logger.LogDebug("Retrieved supplier {SupplierId} from cache", id);
            return cached;
        }

        try
        {
            _logger.LogInformation("Getting supplier by ID from Katana: {SupplierId}", id);
            
            var response = await _httpClient.GetAsync($"{_settings.Endpoints.Suppliers}/{id}");
            
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Supplier not found in Katana: {SupplierId}", id);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Failed to get supplier {SupplierId} from Katana. Status: {Status}, Error: {Error}", 
                        id, response.StatusCode, errorContent);
                }
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;
            
            JsonElement supplierElement = root;
            if (root.TryGetProperty("data", out var wrapped) && wrapped.ValueKind == JsonValueKind.Object)
                supplierElement = wrapped;

            var mapped = MapSupplierElement(supplierElement);
            
            _cache.Set(cacheKey, mapped, TimeSpan.FromMinutes(5));
            
            _logger.LogInformation("Retrieved supplier {SupplierId} from Katana: {Name}", id, mapped?.Name);
            return mapped;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Katana API connection error for supplier ID {SupplierId}", id);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse supplier response for ID {SupplierId}", id);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting supplier by ID {SupplierId}", id);
            return null;
        }
    }

    public async Task<KatanaSupplierDto?> CreateSupplierAsync(KatanaSupplierDto supplier)
    {
        try
        {
            if (supplier == null)
            {
                _logger.LogError("CreateSupplierAsync called with null supplier");
                return null;
            }

            if (string.IsNullOrWhiteSpace(supplier.Name))
            {
                _logger.LogError("CreateSupplierAsync: Supplier name is required");
                return null;
            }

            _logger.LogInformation("Creating supplier in Katana: {SupplierName}", supplier.Name);
            _loggingService.LogInfo($"Creating supplier: {supplier.Name}", null, "CreateSupplierAsync", LogCategory.ExternalAPI);

            var payload = new Dictionary<string, object?>
            {
                { "name", supplier.Name }
            };

            if (!string.IsNullOrWhiteSpace(supplier.Email))
                payload["email"] = supplier.Email;

            if (!string.IsNullOrWhiteSpace(supplier.Phone))
                payload["phone"] = supplier.Phone;

            if (!string.IsNullOrWhiteSpace(supplier.Currency))
                payload["currency"] = supplier.Currency;
            else
                payload["currency"] = "TRY";

            if (!string.IsNullOrWhiteSpace(supplier.Comment))
                payload["comment"] = supplier.Comment;

            if (supplier.Addresses != null && supplier.Addresses.Count > 0)
            {
                var addresses = supplier.Addresses.Select(addr => new Dictionary<string, object?>
                {
                    { "line_1", addr.Line1 },
                    { "line_2", addr.Line2 },
                    { "city", addr.City },
                    { "state", addr.State },
                    { "zip", addr.Zip },
                    { "country", addr.Country ?? "TR" }
                }).ToList();

                payload["addresses"] = addresses;
            }

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            _logger.LogDebug("CreateSupplierAsync payload: {Payload}", json);

            var content = CreateKatanaJsonContent(json);
            var response = await _httpClient.PostAsync(_settings.Endpoints.Suppliers, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                if ((int)response.StatusCode == 422)
                {
                    _logger.LogError("CreateSupplierAsync validation error. Status: {StatusCode}, Response: {Response}", 
                        response.StatusCode, responseContent);
                    _loggingService.LogError("Supplier creation validation failed", null, responseContent, "CreateSupplierAsync", LogCategory.ExternalAPI);
                }
                else
                {
                    _logger.LogError("CreateSupplierAsync failed. Status: {StatusCode}, Response: {Response}", 
                        response.StatusCode, responseContent);
                    _loggingService.LogError($"Supplier creation failed: {response.StatusCode}", null, responseContent, "CreateSupplierAsync", LogCategory.ExternalAPI);
                }
                return null;
            }

            _logger.LogInformation("Supplier created successfully in Katana: {SupplierName}", supplier.Name);
            _loggingService.LogInfo($"Supplier created successfully: {supplier.Name}", null, "CreateSupplierAsync", LogCategory.ExternalAPI);
            
            using var doc = JsonDocument.Parse(responseContent);
            var root = doc.RootElement;
            
            JsonElement supplierElement = root;
            if (root.TryGetProperty("data", out var wrapped) && wrapped.ValueKind == JsonValueKind.Object)
                supplierElement = wrapped;

            var createdSupplier = MapSupplierElement(supplierElement);
            return createdSupplier;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse supplier response. Supplier: {SupplierName}", supplier?.Name ?? "Unknown");
            _loggingService.LogError("JSON parsing error in supplier creation", ex, null, "CreateSupplierAsync", LogCategory.ExternalAPI);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Katana API connection error creating supplier. Supplier: {SupplierName}", supplier?.Name ?? "Unknown");
            _loggingService.LogError("Connection error in supplier creation", ex, null, "CreateSupplierAsync", LogCategory.ExternalAPI);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating supplier in Katana API. Supplier: {SupplierName}", supplier?.Name ?? "Unknown");
            _loggingService.LogError("Unexpected error in supplier creation", ex, null, "CreateSupplierAsync", LogCategory.ExternalAPI);
            return null;
        }
    }

    public async Task<KatanaSupplierDto?> UpdateSupplierAsync(int supplierId, KatanaSupplierDto supplier)
    {
        try
        {
            if (supplierId <= 0)
            {
                _logger.LogError("UpdateSupplierAsync called with invalid supplierId: {SupplierId}", supplierId);
                return null;
            }

            if (supplier == null)
            {
                _logger.LogError("UpdateSupplierAsync called with null supplier for supplierId: {SupplierId}", supplierId);
                return null;
            }

            _logger.LogInformation("Updating supplier in Katana. SupplierId: {SupplierId}, SupplierName: {SupplierName}", supplierId, supplier.Name);
            _loggingService.LogInfo($"Updating supplier {supplierId}: {supplier.Name}", null, "UpdateSupplierAsync", LogCategory.ExternalAPI);

            var payload = new Dictionary<string, object?>();

            if (!string.IsNullOrWhiteSpace(supplier.Name))
                payload["name"] = supplier.Name;

            if (!string.IsNullOrWhiteSpace(supplier.Email))
                payload["email"] = supplier.Email;

            if (!string.IsNullOrWhiteSpace(supplier.Phone))
                payload["phone"] = supplier.Phone;

            if (!string.IsNullOrWhiteSpace(supplier.Currency))
                payload["currency"] = supplier.Currency;

            if (!string.IsNullOrWhiteSpace(supplier.Comment))
                payload["comment"] = supplier.Comment;

            if (supplier.Addresses != null && supplier.Addresses.Count > 0)
            {
                var addresses = supplier.Addresses.Select(addr => new Dictionary<string, object?>
                {
                    { "line_1", addr.Line1 },
                    { "line_2", addr.Line2 },
                    { "city", addr.City },
                    { "state", addr.State },
                    { "zip", addr.Zip },
                    { "country", addr.Country }
                }).ToList();

                payload["addresses"] = addresses;
            }

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            _logger.LogDebug("UpdateSupplierAsync payload for SupplierId {SupplierId}: {Payload}", supplierId, json);

            var content = CreateKatanaJsonContent(json);
            var updateUrl = $"{_settings.Endpoints.Suppliers}/{supplierId}";
            var response = await _httpClient.PatchAsync(updateUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Supplier not found for update: {SupplierId}", supplierId);
                    _loggingService.LogWarning($"Supplier not found: {supplierId}", null, "UpdateSupplierAsync", LogCategory.ExternalAPI);
                }
                else if ((int)response.StatusCode == 422)
                {
                    _logger.LogError("UpdateSupplierAsync validation error for SupplierId: {SupplierId}. Status: {StatusCode}, Response: {Response}", 
                        supplierId, response.StatusCode, responseContent);
                    _loggingService.LogError("Supplier update validation failed", null, responseContent, "UpdateSupplierAsync", LogCategory.ExternalAPI);
                }
                else
                {
                    _logger.LogError("UpdateSupplierAsync failed for SupplierId: {SupplierId}. Status: {StatusCode}, Response: {Response}", 
                        supplierId, response.StatusCode, responseContent);
                    _loggingService.LogError($"Supplier update failed: {response.StatusCode}", null, responseContent, "UpdateSupplierAsync", LogCategory.ExternalAPI);
                }
                return null;
            }

            _cache.Remove($"supplier-{supplierId}");

            _logger.LogInformation("Supplier updated successfully in Katana. SupplierId: {SupplierId}, SupplierName: {SupplierName}", supplierId, supplier.Name);
            _loggingService.LogInfo($"Supplier updated successfully: {supplierId}", null, "UpdateSupplierAsync", LogCategory.ExternalAPI);
            
            using var doc = JsonDocument.Parse(responseContent);
            var root = doc.RootElement;
            
            JsonElement supplierElement = root;
            if (root.TryGetProperty("data", out var wrapped) && wrapped.ValueKind == JsonValueKind.Object)
                supplierElement = wrapped;

            var updatedSupplier = MapSupplierElement(supplierElement);
            return updatedSupplier;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse supplier response. SupplierId: {SupplierId}", supplierId);
            _loggingService.LogError("JSON parsing error in supplier update", ex, null, "UpdateSupplierAsync", LogCategory.ExternalAPI);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Katana API connection error updating supplier. SupplierId: {SupplierId}", supplierId);
            _loggingService.LogError("Connection error in supplier update", ex, null, "UpdateSupplierAsync", LogCategory.ExternalAPI);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating supplier in Katana API. SupplierId: {SupplierId}, Supplier: {SupplierName}", supplierId, supplier?.Name ?? "Unknown");
            _loggingService.LogError("Unexpected error in supplier update", ex, null, "UpdateSupplierAsync", LogCategory.ExternalAPI);
            return null;
        }
    }

    public async Task<bool> DeleteSupplierAsync(int supplierId)
    {
        try
        {
            if (supplierId <= 0)
            {
                _logger.LogError("DeleteSupplierAsync called with invalid supplierId: {SupplierId}", supplierId);
                return false;
            }

            _logger.LogInformation("Deleting supplier from Katana: {SupplierId}", supplierId);
            _loggingService.LogInfo($"Deleting supplier: {supplierId}", null, "DeleteSupplierAsync", LogCategory.ExternalAPI);

            var response = await _httpClient.DeleteAsync($"{_settings.Endpoints.Suppliers}/{supplierId}");

            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                _logger.LogInformation("Successfully deleted supplier: {SupplierId}", supplierId);
                _loggingService.LogInfo($"Supplier deleted successfully: {supplierId}", null, "DeleteSupplierAsync", LogCategory.ExternalAPI);
                
                _cache.Remove($"supplier-{supplierId}");
                return true;
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Supplier not found for deletion: {SupplierId}", supplierId);
                _loggingService.LogWarning($"Supplier not found for deletion: {supplierId}", null, "DeleteSupplierAsync", LogCategory.ExternalAPI);
                return false;
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogError("Unauthorized to delete supplier: {SupplierId}", supplierId);
                _loggingService.LogError("Unauthorized to delete supplier", null, null, "DeleteSupplierAsync", LogCategory.ExternalAPI);
                return false;
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to delete supplier: {SupplierId}. Status: {StatusCode}, Response: {Response}", 
                supplierId, response.StatusCode, errorContent);
            _loggingService.LogError($"Supplier deletion failed: {response.StatusCode}", null, errorContent, "DeleteSupplierAsync", LogCategory.ExternalAPI);
            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Katana API connection error deleting supplier: {SupplierId}", supplierId);
            _loggingService.LogError("Connection error in supplier deletion", ex, null, "DeleteSupplierAsync", LogCategory.ExternalAPI);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting supplier from Katana API. SupplierId: {SupplierId}", supplierId);
            _loggingService.LogError("Unexpected error in supplier deletion", ex, null, "DeleteSupplierAsync", LogCategory.ExternalAPI);
            return false;
        }
    }

    private KatanaSupplierDto MapSupplierElement(JsonElement supplierEl)
    {
        var dto = new KatanaSupplierDto();

        if (supplierEl.TryGetProperty("id", out var idEl) && idEl.ValueKind == JsonValueKind.Number)
        {
            try { dto.Id = idEl.GetInt32(); } catch { dto.Id = 0; }
        }

        dto.Name = supplierEl.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? string.Empty : string.Empty;
        dto.Email = supplierEl.TryGetProperty("email", out var emailEl) ? emailEl.GetString() : null;
        dto.Phone = supplierEl.TryGetProperty("phone", out var phoneEl) ? phoneEl.GetString() : null;
        dto.Currency = supplierEl.TryGetProperty("currency", out var currEl) ? currEl.GetString() : null;
        dto.Comment = supplierEl.TryGetProperty("comment", out var commentEl) ? commentEl.GetString() : null;

        if (supplierEl.TryGetProperty("created_at", out var createdEl) && createdEl.ValueKind == JsonValueKind.String)
        {
            if (DateTime.TryParse(createdEl.GetString(), out var createdDate))
                dto.CreatedAt = createdDate;
        }
        if (supplierEl.TryGetProperty("updated_at", out var updatedEl) && updatedEl.ValueKind == JsonValueKind.String)
        {
            if (DateTime.TryParse(updatedEl.GetString(), out var updatedDate))
                dto.UpdatedAt = updatedDate;
        }

        if (supplierEl.TryGetProperty("addresses", out var addrsEl) && addrsEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var addrEl in addrsEl.EnumerateArray())
            {
                var address = MapSupplierAddressElement(addrEl);
                dto.Addresses.Add(address);
            }
        }

        return dto;
    }

    private KatanaSupplierAddressDto MapSupplierAddressElement(JsonElement addrEl)
    {
        var dto = new KatanaSupplierAddressDto();

        if (addrEl.TryGetProperty("id", out var idEl) && idEl.ValueKind == JsonValueKind.Number)
        {
            try { dto.Id = idEl.GetInt32(); } catch { dto.Id = 0; }
        }
        if (addrEl.TryGetProperty("supplier_id", out var supplierIdEl) && supplierIdEl.ValueKind == JsonValueKind.Number)
        {
            try { dto.SupplierId = supplierIdEl.GetInt32(); } catch { dto.SupplierId = 0; }
        }

        dto.Line1 = addrEl.TryGetProperty("line_1", out var l1El) ? l1El.GetString() : null;
        dto.Line2 = addrEl.TryGetProperty("line_2", out var l2El) ? l2El.GetString() : null;
        dto.City = addrEl.TryGetProperty("city", out var cityEl) ? cityEl.GetString() : null;
        dto.State = addrEl.TryGetProperty("state", out var stateEl) ? stateEl.GetString() : null;
        dto.Zip = addrEl.TryGetProperty("zip", out var zipEl) ? zipEl.GetString() : null;
        dto.Country = addrEl.TryGetProperty("country", out var countryEl) ? countryEl.GetString() : null;
        
        if (addrEl.TryGetProperty("is_default", out var defEl) && defEl.ValueKind == JsonValueKind.True)
            dto.IsDefault = true;

        if (addrEl.TryGetProperty("created_at", out var createdEl) && createdEl.ValueKind == JsonValueKind.String)
        {
            if (DateTime.TryParse(createdEl.GetString(), out var createdDate))
                dto.CreatedAt = createdDate;
        }
        if (addrEl.TryGetProperty("updated_at", out var updatedEl) && updatedEl.ValueKind == JsonValueKind.String)
        {
            if (DateTime.TryParse(updatedEl.GetString(), out var updatedDate))
                dto.UpdatedAt = updatedDate;
        }

        return dto;
    }

    public async Task<List<KatanaSupplierAddressDto>> GetSupplierAddressesAsync(int? supplierId = null)
    {
        try
        {
            _logger.LogInformation("Getting supplier addresses from Katana. SupplierId filter: {SupplierId}", supplierId);
            
            var url = supplierId.HasValue 
                ? $"{_settings.Endpoints.Suppliers}/{supplierId}/addresses"
                : "supplier-addresses";
                
            var response = await _httpClient.GetAsync(url);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get supplier addresses. Status: {StatusCode}", response.StatusCode);
                return new List<KatanaSupplierAddressDto>();
            }

            using var doc = JsonDocument.Parse(responseContent);
            var root = doc.RootElement;
            var addresses = new List<KatanaSupplierAddressDto>();

            if (root.TryGetProperty("data", out var dataEl) && dataEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var addrEl in dataEl.EnumerateArray())
                {
                    var address = MapSupplierAddressElement(addrEl);
                    addresses.Add(address);
                }
            }

            return addresses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting supplier addresses from Katana API");
            return new List<KatanaSupplierAddressDto>();
        }
    }

    public async Task<KatanaSupplierAddressDto?> CreateSupplierAddressAsync(KatanaSupplierAddressDto address)
    {
        try
        {
            if (address == null || address.SupplierId <= 0)
            {
                _logger.LogError("CreateSupplierAddressAsync called with invalid address");
                return null;
            }

            _logger.LogInformation("Creating supplier address for SupplierId: {SupplierId}", address.SupplierId);

            var payload = new Dictionary<string, object?>
            {
                { "supplier_id", address.SupplierId },
                { "line_1", address.Line1 },
                { "line_2", address.Line2 },
                { "city", address.City },
                { "state", address.State },
                { "zip", address.Zip },
                { "country", address.Country ?? "TR" },
                { "is_default", address.IsDefault }
            };

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            var content = CreateKatanaJsonContent(json);
            var response = await _httpClient.PostAsync("supplier-addresses", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("CreateSupplierAddressAsync failed. Status: {StatusCode}, Response: {Response}", 
                    response.StatusCode, responseContent);
                return null;
            }

            using var doc = JsonDocument.Parse(responseContent);
            var root = doc.RootElement;
            
            JsonElement addressElement = root;
            if (root.TryGetProperty("data", out var wrapped) && wrapped.ValueKind == JsonValueKind.Object)
                addressElement = wrapped;

            return MapSupplierAddressElement(addressElement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating supplier address");
            return null;
        }
    }

    public async Task<KatanaSupplierAddressDto?> UpdateSupplierAddressAsync(int addressId, KatanaSupplierAddressDto address)
    {
        try
        {
            if (addressId <= 0 || address == null)
            {
                _logger.LogError("UpdateSupplierAddressAsync called with invalid parameters");
                return null;
            }

            _logger.LogInformation("Updating supplier address: {AddressId}", addressId);

            var payload = new Dictionary<string, object?>();
            
            if (!string.IsNullOrWhiteSpace(address.Line1))
                payload["line_1"] = address.Line1;
            if (!string.IsNullOrWhiteSpace(address.Line2))
                payload["line_2"] = address.Line2;
            if (!string.IsNullOrWhiteSpace(address.City))
                payload["city"] = address.City;
            if (!string.IsNullOrWhiteSpace(address.State))
                payload["state"] = address.State;
            if (!string.IsNullOrWhiteSpace(address.Zip))
                payload["zip"] = address.Zip;
            if (!string.IsNullOrWhiteSpace(address.Country))
                payload["country"] = address.Country;
            
            payload["is_default"] = address.IsDefault;

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            var content = CreateKatanaJsonContent(json);
            var response = await _httpClient.PatchAsync($"supplier-addresses/{addressId}", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("UpdateSupplierAddressAsync failed. Status: {StatusCode}, Response: {Response}", 
                    response.StatusCode, responseContent);
                return null;
            }

            using var doc = JsonDocument.Parse(responseContent);
            var root = doc.RootElement;
            
            JsonElement addressElement = root;
            if (root.TryGetProperty("data", out var wrapped) && wrapped.ValueKind == JsonValueKind.Object)
                addressElement = wrapped;

            return MapSupplierAddressElement(addressElement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating supplier address");
            return null;
        }
    }

    public async Task<bool> DeleteSupplierAddressAsync(int addressId)
    {
        try
        {
            if (addressId <= 0)
            {
                _logger.LogError("DeleteSupplierAddressAsync called with invalid addressId: {AddressId}", addressId);
                return false;
            }

            _logger.LogInformation("Deleting supplier address: {AddressId}", addressId);

            var response = await _httpClient.DeleteAsync($"supplier-addresses/{addressId}");

            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                _logger.LogInformation("Successfully deleted supplier address: {AddressId}", addressId);
                return true;
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Supplier address not found for deletion: {AddressId}", addressId);
                return false;
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to delete supplier address: {AddressId}. Status: {StatusCode}, Response: {Response}", 
                addressId, response.StatusCode, errorContent);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting supplier address. AddressId: {AddressId}", addressId);
            return false;
        }
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

    public async Task<long?> FindVariantIdBySkuAsync(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
            return null;

        var skuVariants = new[] { sku, sku.Trim(), sku.ToLowerInvariant(), sku.ToUpperInvariant() }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToArray();

        foreach (var skuVariant in skuVariants)
        {
            try
            {
                var variantUrl = $"{_settings.Endpoints.Variants}?sku={Uri.EscapeDataString(skuVariant)}";
                var resp = await _httpClient.GetAsync(variantUrl);
                var body = await resp.Content.ReadAsStringAsync();

                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Katana variant lookup failed. Sku={Sku}, Status={Status}", skuVariant, resp.StatusCode);
                    continue;
                }

                using var doc = JsonDocument.Parse(body);
                if (!doc.RootElement.TryGetProperty("data", out var dataEl) || dataEl.ValueKind != JsonValueKind.Array)
                    continue;

                var first = dataEl.EnumerateArray().FirstOrDefault();
                if (first.ValueKind != JsonValueKind.Object)
                    continue;

                long? variantId = null;
                if (first.TryGetProperty("id", out var idEl) && idEl.ValueKind == JsonValueKind.Number)
                    variantId = idEl.GetInt64();
                else if (first.TryGetProperty("id", out idEl) && idEl.ValueKind == JsonValueKind.String && long.TryParse(idEl.GetString(), out var parsedVar))
                    variantId = parsedVar;

                if (variantId.HasValue)
                {
                    _logger.LogInformation("Katana variant found by SKU. Sku={Sku}, VariantId={VariantId}", skuVariant, variantId);
                    return variantId;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Katana variant lookup parse failed. Sku={Sku}", skuVariant);
            }
        }

        _logger.LogWarning("FindVariantIdBySkuAsync: variant not found for SKU {Sku}", sku);
        return null;
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
        _logger.LogDebug("CreateStockAdjustmentAsync payload: {Payload}", json);
        var resp = await _httpClient.PostAsync(_settings.Endpoints.StockAdjustments, CreateKatanaJsonContent(json));
        var content = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("CreateStockAdjustmentAsync failed. Status {Status}, Body {Body}", resp.StatusCode, content);
            resp.EnsureSuccessStatusCode();
        }

        _logger.LogDebug("CreateStockAdjustmentAsync response: {Body}", content);
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
        var resp = await _httpClient.PostAsync(_settings.Endpoints.SalesOrders, CreateKatanaJsonContent(json));
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
        var resp = await _httpClient.PutAsync($"{_settings.Endpoints.SalesOrders}/{salesOrder?.Id}", CreateKatanaJsonContent(json));
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

    public async Task<LocationDto?> CreateLocationAsync(LocationDto location)
    {
        try
        {
            if (location == null)
            {
                _logger.LogError("CreateLocationAsync called with null location");
                return null;
            }

            _logger.LogInformation("Creating location in Katana: {LocationName}", location.Name);

            // Build payload matching Katana API specification
            var payload = new Dictionary<string, object?>
            {
                { "name", location.Name },
                { "legal_name", location.LegalName ?? location.Name },
                { "is_primary", location.IsPrimary },
                { "sales_allowed", location.SalesAllowed },
                { "manufacturing_allowed", location.ManufacturingAllowed },
                { "purchase_allowed", location.PurchaseAllowed }
            };

            // Add optional address_id if present
            if (location.AddressId.HasValue && location.AddressId > 0)
            {
                payload["address_id"] = location.AddressId.Value;
            }

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            _logger.LogDebug("CreateLocationAsync payload: {Payload}", json);

            var content = CreateKatanaJsonContent(json);
            var response = await _httpClient.PostAsync(_settings.Endpoints.Locations, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("CreateLocationAsync failed. Status: {StatusCode}, Response: {Response}", 
                    response.StatusCode, responseContent);
                return null;
            }

            _logger.LogInformation("Location created successfully in Katana: {LocationName}", location.Name);
            
            // Parse and return the created location
            return JsonSerializer.Deserialize<LocationDto>(responseContent, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating location in Katana API. Location: {LocationName}", location?.Name ?? "Unknown");
            return null;
        }
    }

    public async Task<LocationDto?> UpdateLocationAsync(int locationId, LocationDto location)
    {
        try
        {
            if (locationId <= 0)
            {
                _logger.LogError("UpdateLocationAsync called with invalid locationId: {LocationId}", locationId);
                return null;
            }

            if (location == null)
            {
                _logger.LogError("UpdateLocationAsync called with null location for locationId: {LocationId}", locationId);
                return null;
            }

            _logger.LogInformation("Updating location in Katana. LocationId: {LocationId}, LocationName: {LocationName}", locationId, location.Name);

            // Build payload matching Katana API specification
            var payload = new Dictionary<string, object?>
            {
                { "name", location.Name },
                { "legal_name", location.LegalName ?? location.Name },
                { "is_primary", location.IsPrimary },
                { "sales_allowed", location.SalesAllowed },
                { "manufacturing_allowed", location.ManufacturingAllowed },
                { "purchase_allowed", location.PurchaseAllowed }
            };

            // Add optional address_id if present
            if (location.AddressId.HasValue && location.AddressId > 0)
            {
                payload["address_id"] = location.AddressId.Value;
            }

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            _logger.LogDebug("UpdateLocationAsync payload for LocationId {LocationId}: {Payload}", locationId, json);

            var content = CreateKatanaJsonContent(json);
            var updateUrl = $"{_settings.Endpoints.Locations}/{locationId}";
            var response = await _httpClient.PutAsync(updateUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("UpdateLocationAsync failed for LocationId: {LocationId}. Status: {StatusCode}, Response: {Response}", 
                    locationId, response.StatusCode, responseContent);
                return null;
            }

            _logger.LogInformation("Location updated successfully in Katana. LocationId: {LocationId}, LocationName: {LocationName}", locationId, location.Name);
            
            // Parse and return the updated location
            return JsonSerializer.Deserialize<LocationDto>(responseContent, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating location in Katana API. LocationId: {LocationId}, Location: {LocationName}", locationId, location?.Name ?? "Unknown");
            return null;
        }
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

            _logger.LogDebug("Raw response length for {Name}: {Length} chars", logName, content?.Length ?? 0);

            try
            {
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;
                if (root.TryGetProperty("data", out var dataEl) && dataEl.ValueKind == JsonValueKind.Array)
                {
                    var rawText = dataEl.GetRawText();
                    _logger.LogDebug("Unwrapped data array for {Name}, length: {Length}", logName, rawText.Length);
                    var result = JsonSerializer.Deserialize<List<T>>(rawText, _jsonOptions) ?? new List<T>();
                    _logger.LogDebug("Successfully deserialized {Count} items for {Name}", result.Count, logName);
                    return result;
                }
                else
                {
                    _logger.LogWarning("No 'data' array found in response for {Name}", logName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to unwrap data property for {Name}, falling back to direct deserialization.", logName);
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
