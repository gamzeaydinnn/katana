using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using Katana.Core.Interfaces;
using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Microsoft.Extensions.Options;
using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Katana.API.Controllers
{
    [ApiController]
    // Accept legacy paths /api/Luca/... and /api/luca/... in addition to /api/luca-proxy/...
    [Route("api/luca-proxy")]
    [Route("api/luca")]
    public class LucaProxyController : ControllerBase
    {
        private readonly string _lucaBaseUrl;
        private const string SessionCookieName = "LucaProxySession";
        private readonly ILucaCookieJarStore _cookieJarStore;
        private readonly ILogger<LucaProxyController> _logger;
        private readonly Katana.Data.Configuration.LucaApiSettings _settings;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public LucaProxyController(
            ILucaCookieJarStore cookieJarStore,
            IOptions<Katana.Data.Configuration.LucaApiSettings> settings,
            ILogger<LucaProxyController> logger,
            IServiceScopeFactory serviceScopeFactory)
        {
            _cookieJarStore = cookieJarStore;
            _logger = logger;
            _settings = settings.Value;
            _serviceScopeFactory = serviceScopeFactory;
            _lucaBaseUrl = (_settings.BaseUrl ?? string.Empty).TrimEnd('/');
            if (string.IsNullOrWhiteSpace(_lucaBaseUrl))
                _lucaBaseUrl = "https://akozas.luca.com.tr/Yetki"; 
        }

        private void ApplyManualSessionCookie(HttpRequestMessage request)
        {
            try
            {
                if (request == null) return;

                // Only apply explicitly configured manual cookie; do not add anything if empty/placeholder
                var manual = _settings.ManualSessionCookie ?? string.Empty;
                manual = manual.Trim();
                if (string.IsNullOrWhiteSpace(manual)) return;
                if (manual.IndexOf("FILL_ME", StringComparison.OrdinalIgnoreCase) >= 0) return;

                if (!request.Headers.Contains("Cookie"))
                {
                    request.Headers.TryAddWithoutValidation("Cookie", manual);
                }
            }
            catch {  }
        }

        
        private async Task<IActionResult> ForwardResponse(HttpResponseMessage response)
        {
            
            var responseContent = await response.Content.ReadAsStringAsync();
            return new ContentResult
            {
                Content = responseContent,
                ContentType = "application/json",
                StatusCode = (int)response.StatusCode
            };
        }

        private string? GetSessionIdFromRequest()
        {
            // Support both legacy/new header names: 'X-Luca-Session' and 'X-Luca-Proxy-Session'
            try
            {
                if (Request.Headers.TryGetValue("X-Luca-Session", out var headerVals1))
                {
                    var hdr1 = headerVals1.FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(hdr1))
                    {
                        _logger.LogDebug("GetSessionIdFromRequest: found header X-Luca-Session (length={Len})", hdr1?.Length ?? 0);
                        return hdr1;
                    }
                }

                if (Request.Headers.TryGetValue("X-Luca-Proxy-Session", out var headerVals2))
                {
                    var hdr2 = headerVals2.FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(hdr2))
                    {
                        _logger.LogDebug("GetSessionIdFromRequest: found header X-Luca-Proxy-Session (length={Len})", hdr2?.Length ?? 0);
                        return hdr2;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GetSessionIdFromRequest: exception while reading headers");
            }

            if (Request.Cookies.TryGetValue(SessionCookieName, out var cookieVal) && !string.IsNullOrWhiteSpace(cookieVal))
            {
                _logger.LogDebug("GetSessionIdFromRequest: found cookie {CookieName} (length={Len})", SessionCookieName, cookieVal?.Length ?? 0);
                return cookieVal;
            }

            _logger.LogDebug("GetSessionIdFromRequest: no session found in headers or cookies");

            return null;
        }

        private string EnsureSessionId()
        {
            if (Request.Cookies.TryGetValue(SessionCookieName, out var sessionId) && !string.IsNullOrWhiteSpace(sessionId))
            {
                return sessionId;
            }

            sessionId = Guid.NewGuid().ToString("N");
            var isHttps = string.Equals(Request.Scheme, "https", StringComparison.OrdinalIgnoreCase);
            Response.Cookies.Append(SessionCookieName, sessionId, new Microsoft.AspNetCore.Http.CookieOptions
            {
                HttpOnly = true,
                Secure = isHttps,
                SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax,
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddMinutes(5) 
            });
            return sessionId;
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] JsonElement body)
        {
            var sessionId = EnsureSessionId();
            // expose the proxy session id to clients explicitly
            try { Response.Headers["X-Luca-Proxy-Session"] = sessionId; } catch { }
            var cookieContainer = _cookieJarStore.GetOrCreate(sessionId);
            var handler = new HttpClientHandler
            {
                UseCookies = true,
                CookieContainer = cookieContainer,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            var client = new HttpClient(handler);
            // Apply timeout from settings (default 180 seconds for LUCA)
            var timeoutSeconds = _settings.TimeoutSeconds > 0 ? _settings.TimeoutSeconds : 180;
            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            _logger.LogDebug("LucaProxy Login: HttpClient timeout set to {TimeoutSeconds} seconds", timeoutSeconds);
            // Allow callers to provide credentials in the request body. If provided, prefer them;
            // otherwise fall back to configured settings.
            string orgCodeVal = _settings.MemberNumber ?? string.Empty;
            string userNameVal = _settings.Username ?? string.Empty;
            string userPasswordVal = _settings.Password ?? string.Empty;
            try
            {
                if (body.ValueKind == JsonValueKind.Object)
                {
                    if (body.TryGetProperty("orgCode", out var o) && o.ValueKind == JsonValueKind.String) { var v = o.GetString(); if (!string.IsNullOrWhiteSpace(v)) orgCodeVal = v; }
                    if (body.TryGetProperty("userName", out var u) && u.ValueKind == JsonValueKind.String) { var v = u.GetString(); if (!string.IsNullOrWhiteSpace(v)) userNameVal = v; }
                    if (body.TryGetProperty("userPassword", out var p) && p.ValueKind == JsonValueKind.String) { var v = p.GetString(); if (!string.IsNullOrWhiteSpace(v)) userPasswordVal = v; }
                }
            }
            catch { }

            var defaultPayload = new
            {
                orgCode = string.IsNullOrWhiteSpace(orgCodeVal) ? (object)"" : orgCodeVal,
                userName = string.IsNullOrWhiteSpace(userNameVal) ? (object)"" : userNameVal,
                userPassword = string.IsNullOrWhiteSpace(userPasswordVal) ? (object)"" : userPasswordVal
            };
            var payloadJson = JsonSerializer.Serialize(defaultPayload);

            var requestUrl = $"{_lucaBaseUrl}/{_settings.Endpoints.Auth}";
            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            request.Content = new StringContent(payloadJson, Encoding.UTF8, "application/json");
            ApplyManualSessionCookie(request);

            _logger.LogDebug("LucaProxy: Sending login request to {Url}. Payload preview: {PayloadPreview}", requestUrl, payloadJson?.Length > 1000 ? payloadJson.Substring(0, 1000) + "..." : payloadJson);

            try
            {
                var response = await client.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                var respPreview = responseContent != null && responseContent.Length > 1000 ? responseContent.Substring(0, 1000) + "..." : responseContent;
                _logger.LogDebug("LucaProxy: Received response from {Url}. Status: {Status}. Body preview: {Preview}", requestUrl, response.StatusCode, respPreview);

                
                object? parsed = null;
                try
                {
                    if (!string.IsNullOrEmpty(responseContent))
                        parsed = JsonSerializer.Deserialize<object>(responseContent);
                }
                catch {  }

                
                
                
                try
                {
                    if (parsed is JsonElement el && el.ValueKind == JsonValueKind.Object)
                    {
                        if (el.TryGetProperty("code", out var codeProp) && codeProp.ValueKind == JsonValueKind.Number && codeProp.GetInt32() != 0)
                        {
                            var msg = el.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String ? m.GetString() : "Login failed";
                            _logger.LogWarning("LucaProxy: Login returned code {Code}: {Message}", codeProp.GetInt32(), msg);
                            return Unauthorized(new { raw = parsed, sessionId, message = msg });
                        }
                    }
                }
                catch { }

                if (response.IsSuccessStatusCode)
                    return Ok(new { raw = parsed, sessionId });

                return StatusCode((int)response.StatusCode, new { raw = parsed, sessionId });
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "LucaProxy Login: Request timeout after {TimeoutSeconds} seconds to {Url}. LUCA server may be unavailable or slow.", timeoutSeconds, requestUrl);
                return StatusCode(504, new { 
                    sessionId, 
                    message = $"LUCA API zaman aşımına uğradı. Sunucu {timeoutSeconds} saniye içinde yanıt vermedi.",
                    error = "Gateway Timeout",
                    details = "akozas.luca.com.tr:443 ile bağlantı kurulamadı"
                });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "LucaProxy Login: Network error connecting to {Url}", requestUrl);
                return StatusCode(503, new { 
                    sessionId, 
                    message = "LUCA API ağ hatası. Lütfen daha sonra tekrar deneyin.",
                    error = "Service Unavailable",
                    details = ex.Message
                });
            }
        }


        [HttpPost("branches")]
        public async Task<IActionResult> Branches()
        {
            var sessionId = GetSessionIdFromRequest();
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return Unauthorized(new { message = "Missing LucaProxySession. Please login first (send X-Luca-Session header or login first)." });
            }
            var cookieContainer = _cookieJarStore.GetOrCreate(sessionId);
            var handler = new HttpClientHandler
            {
                UseCookies = true,
                CookieContainer = cookieContainer,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            var client = new HttpClient(handler);
            var timeoutSeconds = _settings.TimeoutSeconds > 0 ? _settings.TimeoutSeconds : 180;
            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            _logger.LogDebug("LucaProxy Branches: HttpClient timeout set to {TimeoutSeconds} seconds", timeoutSeconds);
            var requestUrl = $"{_lucaBaseUrl}/{_settings.Endpoints.Branches}";
            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
            ApplyManualSessionCookie(request);

            _logger.LogDebug("LucaProxy: Sending branches request to {Url}", requestUrl);

            try
            {
                var response = await client.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                var respPreview = responseContent != null && responseContent.Length > 1000 ? responseContent.Substring(0, 1000) + "..." : responseContent;
                _logger.LogDebug("LucaProxy: Branches response from {Url}. Status: {Status}. Body preview: {Preview}", requestUrl, response.StatusCode, respPreview);

                _logger.LogInformation("Luca /branches raw response: {Length} chars, status: {Status}", responseContent?.Length ?? 0, response.StatusCode);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Luca /branches non-success response: {Status}. Body: {Body}", response.StatusCode, responseContent);
                    return StatusCode((int)response.StatusCode, new { raw = responseContent, status = (int)response.StatusCode, message = "Luca API error" });
                }

                
                if ((responseContent?.Length ?? 0) < 200)
                {
                    _logger.LogWarning("Luca /branches suspiciously short response ({Length} chars): {Body}", responseContent?.Length ?? 0, responseContent);
                }

                try
                {
                    using var doc = JsonDocument.Parse(responseContent ?? "null");
                    var root = doc.RootElement;

                    JsonElement? foundArray = null;
                    if (root.ValueKind == JsonValueKind.Array) foundArray = root;
                    else if (root.TryGetProperty("list", out var list) && list.ValueKind == JsonValueKind.Array) foundArray = list;
                    else if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array) foundArray = data;
                    else if (root.TryGetProperty("branches", out var branches) && branches.ValueKind == JsonValueKind.Array) foundArray = branches;
                    else if (root.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array) foundArray = items;

                    if (foundArray.HasValue)
                    {
                        
                        
                        var branchesObj = JsonSerializer.Deserialize<object>(foundArray.Value.GetRawText());
                        var rawObj = JsonSerializer.Deserialize<object>(responseContent ?? "null");
                        return Ok(new { branches = branchesObj, raw = rawObj });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse Luca branches response");
                }

                
                return StatusCode((int)response.StatusCode, new { raw = responseContent, status = (int)response.StatusCode });
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "LucaProxy Branches: Request timeout after {TimeoutSeconds} seconds to {Url}.", timeoutSeconds, requestUrl);
                return StatusCode(504, new { 
                    message = $"LUCA /branches zaman aşımına uğradı ({timeoutSeconds}s)",
                    error = "Gateway Timeout"
                });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "LucaProxy Branches: Network error connecting to {Url}", requestUrl);
                return StatusCode(503, new { 
                    message = "LUCA /branches ağ hatası",
                    error = "Service Unavailable",
                    details = ex.Message
                });
            }
        }

        [HttpPost("document-type-details")]
        public async Task<IActionResult> DocumentTypeDetails([FromBody] LucaListDocumentTypeDetailsRequest? request = null)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var result = await lucaService.ListDocumentTypeDetailsAsync(request);
            return Ok(result);
        }

        [HttpPost("document-series")]
        public async Task<IActionResult> DocumentSeries([FromBody] LucaListDocumentSeriesRequest? request = null)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var effective = request ?? new LucaListDocumentSeriesRequest();
            var result = await lucaService.ListDocumentSeriesAsync(effective);
            return Ok(result);
        }

        [HttpPost("branch-currencies")]
        public async Task<IActionResult> BranchCurrencies([FromBody] LucaListBranchCurrenciesRequest? request = null)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var effective = request ?? new LucaListBranchCurrenciesRequest();
            var result = await lucaService.ListBranchCurrenciesAsync(effective);
            return Ok(result);
        }

        [HttpPost("document-series/max")]
        public async Task<IActionResult> DocumentSeriesMax([FromBody] LucaGetDocumentSeriesMaxRequest request)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var effective = request ?? new LucaGetDocumentSeriesMaxRequest();
            var result = await lucaService.GetDocumentSeriesMaxAsync(effective);
            return Ok(result);
        }

        [HttpPost("dynamic-lov-values")]
        public async Task<IActionResult> DynamicLovValues([FromBody] LucaListDynamicLovValuesRequest? request = null)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var effective = request ?? new LucaListDynamicLovValuesRequest();
            var result = await lucaService.ListDynamicLovValuesAsync(effective);
            return Ok(result);
        }

        [HttpPost("dynamic-lov-values/update")]
        public async Task<IActionResult> UpdateDynamicLovValue([FromBody] LucaUpdateDynamicLovValueRequest request)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var result = await lucaService.UpdateDynamicLovValueAsync(request);
            return Ok(result);
        }

        [HttpPost("dynamic-lov-values/create")]
        public async Task<IActionResult> CreateDynamicLovValue([FromBody] LucaCreateDynamicLovRequest request)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var result = await lucaService.CreateDynamicLovValueAsync(request);
            return Ok(result);
        }

        [HttpPost("attributes/update")]
        public async Task<IActionResult> UpdateAttribute([FromBody] LucaUpdateAttributeRequest request)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var result = await lucaService.UpdateAttributeAsync(request);
            return Ok(result);
        }

        [HttpPost("measurement-units/list")]
        public async Task<IActionResult> ListMeasurementUnits([FromBody] LucaListMeasurementUnitsRequest? request = null)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var effective = request ?? new LucaListMeasurementUnitsRequest();
            var result = await lucaService.ListMeasurementUnitsAsync(effective);
            return Ok(result);
        }

        [HttpPost("tax-offices/list")]
        public async Task<IActionResult> ListTaxOffices([FromBody] LucaListTaxOfficesRequest? request = null)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var effective = request ?? new LucaListTaxOfficesRequest();
            var result = await lucaService.ListTaxOfficesAsync(effective);
            return Ok(result);
        }

        [HttpPost("customer-addresses")]
        public async Task<IActionResult> CustomerAddresses([FromBody] LucaListCustomerAddressesRequest? request = null)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var effective = request ?? new LucaListCustomerAddressesRequest();
            var result = await lucaService.ListCustomerAddressesAsync(effective);
            return Ok(result);
        }

        [HttpPost("customer-working-conditions")]
        public async Task<IActionResult> CustomerWorkingConditions([FromBody] LucaGetCustomerWorkingConditionsRequest request)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var result = await lucaService.GetCustomerWorkingConditionsAsync(request);
            return Ok(result);
        }

        [HttpPost("customer-contacts")]
        public async Task<IActionResult> CustomerContacts([FromBody] LucaListCustomerContactsRequest request)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var effective = request ?? new LucaListCustomerContactsRequest();
            var result = await lucaService.ListCustomerContactsAsync(effective);
            return Ok(result);
        }

        [HttpPost("customer-authorized-persons")]
        public async Task<IActionResult> CustomerAuthorizedPersons([FromBody] LucaListCustomerAuthorizedPersonsRequest request)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var effective = request ?? new LucaListCustomerAuthorizedPersonsRequest();
            var result = await lucaService.ListCustomerAuthorizedPersonsAsync(effective);
            return Ok(result);
        }

        [HttpPost("customer-risk")]
        public async Task<IActionResult> CustomerRisk([FromBody] LucaGetCustomerRiskRequest request)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var effective = request ?? new LucaGetCustomerRiskRequest();
            var result = await lucaService.GetCustomerRiskAsync(effective);
            return Ok(result);
        }

        [HttpPost("customers/list")]
        public async Task<IActionResult> ListCustomers([FromBody] LucaListCustomersRequest? request = null)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var effective = request ?? new LucaListCustomersRequest();
            var result = await lucaService.ListCustomersAsync(effective);
            return Ok(result);
        }

        [HttpPost("customers/create")]
        public async Task<IActionResult> CreateCustomer([FromBody] LucaCreateCustomerRequest request)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var result = await lucaService.CreateCustomerAsync(request);
            return Ok(result);
        }

        [HttpPost("suppliers/list")]
        public async Task<IActionResult> ListSuppliers([FromBody] LucaListSuppliersRequest? request = null)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var effective = request ?? new LucaListSuppliersRequest();
            var result = await lucaService.ListSuppliersAsync(effective);
            return Ok(result);
        }

        [HttpPost("suppliers/create")]
        public async Task<IActionResult> CreateSupplier([FromBody] LucaCreateSupplierRequest request)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var result = await lucaService.CreateSupplierAsync(request);
            return Ok(result);
        }

        [HttpPost("customer-transaction")]
        public async Task<IActionResult> CreateCustomerTransaction([FromBody] LucaCreateCariHareketRequest request)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var result = await lucaService.CreateCustomerTransactionAsync(request);
            return Ok(result);
        }

        [HttpPost("customer-transactions/list")]
        public async Task<IActionResult> ListCustomerTransactions([FromBody] LucaListCariHareketBaslikRequest? request = null, [FromQuery] bool detayliListe = false)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var effective = request ?? new LucaListCariHareketBaslikRequest();
            var result = await lucaService.ListCustomerTransactionsAsync(effective, detayliListe);
            return Ok(result);
        }

        [HttpPost("customer-transactions/special")]
        public async Task<IActionResult> ListSpecialCustomerTransactions([FromBody] LucaListOzelCariHareketBaslikRequest? request = null)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var result = await lucaService.ListSpecialCustomerTransactionsAsync(request);
            return Ok(result);
        }

        [HttpPost("customer-contracts/create")]
        public async Task<IActionResult> CreateCustomerContract([FromBody] LucaCreateCustomerContractRequest request)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var result = await lucaService.CreateCustomerContractAsync(request);
            return Ok(result);
        }

        [HttpPost("stock-cards/list")]
        public async Task<IActionResult> StockCardsList([FromBody] LucaListStockCardsRequest? request = null)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var effective = request ?? new LucaListStockCardsRequest();
            var result = await lucaService.ListStockCardsAsync(effective, CancellationToken.None);
            return Ok(result);
        }

        [HttpPost("stock-cards/autocomplete")]
        public async Task<IActionResult> StockCardsAutoComplete([FromBody] LucaStockCardAutoCompleteRequest? request = null)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var effective = request ?? new LucaStockCardAutoCompleteRequest();
            var result = await lucaService.ListStockCardsAutoCompleteAsync(effective);
            return Ok(result);
        }

        [HttpPost("stock/dsh/create")]
        public async Task<IActionResult> CreateDsh([FromBody] LucaCreateDshBaslikRequest request)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var result = await lucaService.CreateOtherStockMovementAsync(request);
            return Ok(result);
        }

        [HttpPost("stock/depot-transfer")]
        public async Task<IActionResult> CreateDepotTransfer([FromBody] LucaStockTransferRequest request)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var result = await lucaService.CreateWarehouseTransferAsync(request);
            return Ok(result);
        }

        [HttpPost("stock-cards/create")]
        public async Task<IActionResult> CreateStockCard([FromBody] LucaCreateStokKartiRequest request)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var result = await lucaService.CreateStockCardAsync(request);
            return Ok(result);
        }

        [HttpPost("stock-categories/list")]
        public async Task<IActionResult> StockCategoriesList([FromBody] LucaListStockCategoriesRequest? request = null)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var effective = request ?? new LucaListStockCategoriesRequest();
            var result = await lucaService.ListStockCategoriesAsync(effective);
            return Ok(result);
        }

        [HttpPost("stock-cards/alt-units")]
        public async Task<IActionResult> StockCardAltUnits([FromBody] LucaStockCardByIdRequest request)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var result = await lucaService.ListStockCardAltUnitsAsync(request);
            return Ok(result);
        }

        [HttpPost("stock-cards/alt-stocks")]
        public async Task<IActionResult> StockCardAltStocks([FromBody] LucaStockCardByIdRequest request)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var result = await lucaService.ListStockCardAltStocksAsync(request);
            return Ok(result);
        }

        [HttpPost("stock-cards/purchase-prices")]
        public async Task<IActionResult> StockCardPurchasePrices([FromBody] LucaStockCardByIdRequest request)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var result = await lucaService.ListStockCardPurchasePricesAsync(request);
            return Ok(result);
        }

        [HttpPost("stock-cards/sales-prices")]
        public async Task<IActionResult> StockCardSalesPrices([FromBody] LucaStockCardByIdRequest request)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var result = await lucaService.ListStockCardSalesPricesAsync(request);
            return Ok(result);
        }


        [HttpPost("stock-cards/suppliers")]
        public async Task<IActionResult> StockCardSuppliers([FromBody] LucaStockCardByIdRequest request)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var result = await lucaService.ListStockCardSuppliersAsync(request, CancellationToken.None);
            return Ok(result);
        }

        [HttpPost("warehouses/list")]
        public async Task<IActionResult> WarehousesList([FromBody] LucaListWarehousesRequest? request = null)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var effective = request ?? new LucaListWarehousesRequest();
            var result = await lucaService.ListWarehousesAsync(effective);
            return Ok(result);
        }

        [HttpPost("delivery-notes/list")]
        public async Task<IActionResult> DeliveryNotesList([FromBody] LucaListIrsaliyeRequest? request = null, [FromQuery] bool detayliListe = false)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var result = await lucaService.ListDeliveryNotesAsync(request, detayliListe);
            return Ok(result);
        }

        [HttpPost("delivery-notes/create")]
        public async Task<IActionResult> CreateDeliveryNote([FromBody] LucaCreateIrsaliyeBaslikRequest request)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var result = await lucaService.CreateDeliveryNoteAsync(request);
            return Ok(result);
        }

        [HttpPost("delivery-notes/delete")]
        public async Task<IActionResult> DeleteDeliveryNote([FromBody] LucaDeleteIrsaliyeRequest request)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var result = await lucaService.DeleteDeliveryNoteAsync(request);
            return Ok(result);
        }

        [HttpPost("stock-count/create")]
        public async Task<IActionResult> CreateStockCount([FromBody] LucaCreateStockCountRequest request)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var result = await lucaService.CreateStockCountResultAsync(request);
            return Ok(result);
        }

        [HttpPost("delivery-notes/eirsaliye/xml")]
        public async Task<IActionResult> GetEirsaliyeXml([FromBody] LucaGetEirsaliyeXmlRequest request)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var xmlContent = await lucaService.GetEirsaliyeXmlAsync(request);
            return Content(xmlContent ?? string.Empty, "application/xml");
        }

        [HttpPost("warehouses/stock-quantity")]
        public async Task<IActionResult> WarehouseStockQuantity([FromBody] LucaGetWarehouseStockRequest request)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var result = await lucaService.GetWarehouseStockQuantityAsync(request);
            return Ok(result);
        }

        [HttpPost("uts/transmit")]
        public async Task<IActionResult> UtsTransmit([FromBody] LucaUtsTransmitRequest request)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var result = await lucaService.NotifyUtsAsync(request);
            return Ok(result);
        }

        [HttpPost("select-branch")]
        public async Task<IActionResult> SelectBranch([FromBody] JsonElement body)
        {
            var sessionId = GetSessionIdFromRequest();
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return Unauthorized(new { message = "Missing LucaProxySession. Please login first (send X-Luca-Session header or login first)." });
            }
            var cookieContainer = _cookieJarStore.GetOrCreate(sessionId);
            var handler = new HttpClientHandler
            {
                UseCookies = true,
                CookieContainer = cookieContainer,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            var client = new HttpClient(handler);
            var timeoutSeconds = _settings.TimeoutSeconds > 0 ? _settings.TimeoutSeconds : 180;
            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            _logger.LogDebug("LucaProxy SelectBranch: HttpClient timeout set to {TimeoutSeconds} seconds", timeoutSeconds);
            var requestUrl = $"{_lucaBaseUrl}/{_settings.Endpoints.ChangeBranch}";
            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            var bodyStr = body.ToString();
            request.Content = new StringContent(bodyStr, Encoding.UTF8, "application/json");
            ApplyManualSessionCookie(request);

            _logger.LogDebug("LucaProxy: Sending select-branch request to {Url}. Payload preview: {PayloadPreview}", requestUrl, bodyStr?.Length > 1000 ? bodyStr.Substring(0, 1000) + "..." : bodyStr);

            try
            {
                var response = await client.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();
                var respPreview = responseContent != null && responseContent.Length > 1000 ? responseContent.Substring(0, 1000) + "..." : responseContent;
                _logger.LogDebug("LucaProxy: Select-branch response from {Url}. Status: {Status}. Body preview: {Preview}", requestUrl, response.StatusCode, respPreview);

                return await ForwardResponse(response);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "LucaProxy SelectBranch: Request timeout after {TimeoutSeconds} seconds to {Url}.", timeoutSeconds, requestUrl);
                return StatusCode(504, new { 
                    message = $"LUCA /select-branch zaman aşımına uğradı ({timeoutSeconds}s)",
                    error = "Gateway Timeout"
                });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "LucaProxy SelectBranch: Network error connecting to {Url}", requestUrl);
                return StatusCode(503, new { 
                    message = "LUCA /select-branch ağ hatası",
                    error = "Service Unavailable",
                    details = ex.Message
                });
            }
        }

        [HttpPost("sync-products")]
        public IActionResult SyncProducts([FromBody] SyncOptionsDto? options)
        {
            var opts = options ?? new SyncOptionsDto();
            var sessionId = GetSessionIdFromRequest();
            _logger.LogInformation("[Background Sync] /api/luca/sync-products called. Starting background sync. SessionID: {SessionId}", string.IsNullOrWhiteSpace(sessionId) ? "AUTO-LOGIN" : sessionId);

            Task.Run(async () =>
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var scopedLogger = scope.ServiceProvider.GetRequiredService<ILogger<LucaProxyController>>();
                try
                {
                    var scopedSyncService = scope.ServiceProvider.GetRequiredService<ISyncService>();
                    scopedLogger.LogInformation("[Background Sync] SyncProductsToLucaAsync started.");
                    await scopedSyncService.SyncProductsToLucaAsync(opts);
                    scopedLogger.LogInformation("[Background Sync] SyncProductsToLucaAsync completed.");
                }
                catch (Exception ex)
                {
                    scopedLogger.LogError(ex, "[Background Sync] Error while syncing products to Luca.");
                }
            });

            return Ok(new
            {
                message = "Senkronizasyon arka planda başlatıldı. İşlem bitince ürünler Luca'da görünecektir."
            });
        }

        // Try to perform an automatic login with configured credentials and return the sessionId (or null)
        private async Task<string?> AutoLoginAndReturnSessionId()
        {
            try
            {
                var sessionId = EnsureSessionId();
                var cookieContainer = _cookieJarStore.GetOrCreate(sessionId);
                var handler = new HttpClientHandler
                {
                    UseCookies = true,
                    CookieContainer = cookieContainer,
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                };
                using var client = new HttpClient(handler);
                var timeoutSeconds = _settings.TimeoutSeconds > 0 ? _settings.TimeoutSeconds : 180;
                client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
                _logger.LogDebug("LucaProxy AutoLogin: HttpClient timeout set to {TimeoutSeconds} seconds", timeoutSeconds);

                var payload = new
                {
                    orgCode = _settings.MemberNumber ?? "",
                    userName = _settings.Username ?? "",
                    userPassword = _settings.Password ?? ""
                };
                var payloadJson = JsonSerializer.Serialize(payload);

                var requestUrl = $"{_lucaBaseUrl}/{_settings.Endpoints.Auth}";
                var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
                request.Content = new StringContent(payloadJson, Encoding.UTF8, "application/json");
                ApplyManualSessionCookie(request);

                var response = await client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Auto-login successful, sessionId: {SessionId}", sessionId);
                    try { Response.Headers["X-Luca-Proxy-Session"] = sessionId; } catch { }
                    // Attempt to select preferred branch immediately after login
                    await EnsureBranchSelectedForSessionAsync(sessionId, _settings.ForcedBranchId ?? _settings.DefaultBranchId);
                    return sessionId;
                }

                _logger.LogWarning("Auto-login failed with status {Status}", response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Auto-login failed");
                return null;
            }
        }

        // Ensure the desired branch is selected for the given proxy session
        private async Task<bool> EnsureBranchSelectedForSessionAsync(string? sessionId, long? preferredBranchId)
        {
            if (string.IsNullOrWhiteSpace(sessionId)) return false;
            if (!preferredBranchId.HasValue) return true;

            try
            {
                _logger.LogDebug("EnsureBranchSelectedForSessionAsync: session={SessionId}, preferredBranch={BranchId}", sessionId, preferredBranchId);

                var cookieContainer = _cookieJarStore.GetOrCreate(sessionId);
                var handler = new HttpClientHandler
                {
                    UseCookies = true,
                    CookieContainer = cookieContainer,
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                };

                using var client = new HttpClient(handler);
                var timeoutSeconds = _settings.TimeoutSeconds > 0 ? _settings.TimeoutSeconds : 180;
                client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
                _logger.LogDebug("LucaProxy EnsureBranch: HttpClient timeout set to {TimeoutSeconds} seconds", timeoutSeconds);
                
                var payload = new { orgSirketSubeId = preferredBranchId.Value };
                var payloadJson = JsonSerializer.Serialize(payload);

                var requestUrl = $"{_lucaBaseUrl}/{_settings.Endpoints.ChangeBranch}";
                var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
                request.Content = new StringContent(payloadJson, Encoding.GetEncoding(_settings.Encoding ?? "iso-8859-9"), "application/json");
                ApplyManualSessionCookie(request);

                _logger.LogDebug("EnsureBranchSelectedForSessionAsync: sending branch-change request to {Url}. Payload: {PayloadPreview}", requestUrl, payloadJson?.Length > 1000 ? payloadJson.Substring(0, 1000) + "..." : payloadJson);

                // Attempt to list cookies associated with the Luca base URL for debugging
                try
                {
                    var uri = new Uri(_lucaBaseUrl);
                    var cookies = cookieContainer.GetCookies(uri);
                    var cookieSb = new System.Text.StringBuilder();
                    foreach (System.Net.Cookie c in cookies)
                    {
                        cookieSb.Append(c.Name).Append("=").Append(c.Value).Append("; ");
                    }
                    var cookieDump = cookieSb.Length > 0 ? cookieSb.ToString() : "(no cookies)";
                    _logger.LogDebug("EnsureBranchSelectedForSessionAsync: cookies for {Host}: {Cookies}", uri.Host, cookieDump);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "EnsureBranchSelectedForSessionAsync: failed to enumerate cookies for session {SessionId}", sessionId);
                }

                var response = await client.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogDebug("EnsureBranchSelectedForSessionAsync: branch-change response status={Status}, length={Len}", response.StatusCode, responseContent?.Length ?? 0);

                var respNonNull = responseContent ?? string.Empty;

                var ok = response.IsSuccessStatusCode &&
                         (respNonNull.Contains("\"code\":0") ||
                          respNonNull.IndexOf("başarı", StringComparison.OrdinalIgnoreCase) >= 0 ||
                          respNonNull.IndexOf("Basari", StringComparison.OrdinalIgnoreCase) >= 0);

                if (!ok)
                {
                    _logger.LogWarning("EnsureBranchSelectedForSession failed. Status: {Status}, Body: {Body}", response.StatusCode, responseContent);
                }
                else
                {
                    _logger.LogInformation("Branch selection succeeded for session {SessionId} to branch {BranchId}", sessionId, preferredBranchId.Value);
                }

                return ok;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "EnsureBranchSelectedForSessionAsync threw an exception");
                return false;
            }
        }

        /// <summary>
        /// GET /api/luca/koza-stock-cards - Fetches stock cards from Luca/Koza
        /// </summary>
        [HttpGet("koza-stock-cards")]
        public async Task<IActionResult> GetKozaStockCards()
        {
            try
            {
                _logger.LogInformation("GetStockCards: Fetching stock cards from Luca/Koza...");

                // Get ILucaService from DI
                using var scope = _serviceScopeFactory.CreateScope();
                var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();

                // Use FetchProductsAsync to get stock cards from Luca
                var products = await lucaService.FetchProductsAsync(System.Threading.CancellationToken.None);

                if (products == null || products.Count == 0)
                {
                    _logger.LogWarning("GetStockCards: No products returned from Luca/Koza");
                    return Ok(new List<object>());
                }

                _logger.LogInformation("GetStockCards: Retrieved {Count} stock cards from Luca/Koza", products.Count);

                // Map to a simpler format for frontend - LucaProductDto has ProductCode, ProductName, Unit
                var result = products.Select(p => new
                {
                    id = p.SkartId,
                    code = p.ProductCode ?? "",
                    name = p.ProductName ?? "",
                    unit = p.Unit ?? "",
                    barcode = p.Barcode ?? "",
                    purchaseVatRate = p.PurchaseVatRate,
                    salesVatRate = p.SalesVatRate
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetStockCards: Error fetching stock cards");
                return StatusCode(500, new { error = true, message = ex.Message });
            }
        }
        
        [HttpPost("stock-cards/costs")]
        public async Task<IActionResult> StockCardCosts([FromBody] LucaStockCardByIdRequest request)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var result = await lucaService.ListStockCardCostsAsync(request);
            return Ok(result);
        }

        [HttpPost("stock-cards/purchase-terms")]
        public async Task<IActionResult> StockCardPurchaseTerms([FromBody] LucaStockCardByIdRequest request)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var result = await lucaService.ListStockCardPurchaseTermsAsync(request);
            return Ok(result);
        }
        [HttpPost("sales-orders/list")]
        public async Task<IActionResult> SalesOrdersList([FromBody] LucaListSalesOrdersRequest? request = null, [FromQuery] bool detayliListe = false)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var effective = request ?? new LucaListSalesOrdersRequest();
            var result = await lucaService.ListSalesOrdersAsync(effective, detayliListe);
            return Ok(result);
        }

        [HttpPost("sales-orders/create")]
        public async Task<IActionResult> CreateSalesOrder([FromBody] LucaCreateSalesOrderRequest request)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var result = await lucaService.CreateSalesOrderAsync(request);
            return Ok(result);
        }

        [HttpPost("sales-orders/delete")]
        public async Task<IActionResult> DeleteSalesOrder([FromBody] LucaDeleteSalesOrderRequest request)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var result = await lucaService.DeleteSalesOrderAsync(request);
            return Ok(result);
        }

        [HttpPost("purchase-orders/list")]
        public async Task<IActionResult> PurchaseOrdersList([FromBody] LucaListPurchaseOrdersRequest? request = null, [FromQuery] bool detayliListe = false)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var effective = request ?? new LucaListPurchaseOrdersRequest();
            var result = await lucaService.ListPurchaseOrdersAsync(effective, detayliListe);
            return Ok(result);
        }

        [HttpPost("purchase-orders/create")]
        public async Task<IActionResult> CreatePurchaseOrder([FromBody] LucaCreatePurchaseOrderRequest request)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var result = await lucaService.CreatePurchaseOrderAsync(request);
            return Ok(result);
        }

        [HttpPost("purchase-orders/delete")]
        public async Task<IActionResult> DeletePurchaseOrder([FromBody] LucaDeletePurchaseOrderRequest request)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var result = await lucaService.DeletePurchaseOrderAsync(request);
            return Ok(result);
        }

        [HttpPost("invoices/list")]
        public async Task<IActionResult> InvoicesList([FromBody] LucaListInvoicesRequest? request = null, [FromQuery] bool detayliListe = false)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var effective = request ?? new LucaListInvoicesRequest();
            var result = await lucaService.ListInvoicesAsync(effective, detayliListe);
            return Ok(result);
        }

        [HttpPost("invoices/create")]
        public async Task<IActionResult> CreateInvoice([FromBody] JsonElement request)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var result = await lucaService.CreateInvoiceRawJsonAsync(request.GetRawText());
            return Ok(result);
        }

        [HttpPost("invoices/pdf-link")]
        public async Task<IActionResult> InvoicePdfLink([FromBody] LucaInvoicePdfLinkRequest request)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var result = await lucaService.GetInvoicePdfLinkAsync(request);
            return Ok(result);
        }

        [HttpPost("invoices/close")]
        public async Task<IActionResult> CloseInvoice([FromBody] LucaCloseInvoiceRequest request)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var result = await lucaService.CloseInvoiceAsync(request);
            return Ok(result);
        }

        [HttpPost("invoices/delete")]
        public async Task<IActionResult> DeleteInvoice([FromBody] LucaDeleteInvoiceRequest request)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var result = await lucaService.DeleteInvoiceAsync(request);
            return Ok(result);
        }

        [HttpPost("invoices/currency")]
        public async Task<IActionResult> CurrencyInvoices([FromBody] LucaListCurrencyInvoicesRequest? request = null)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var effective = request ?? new LucaListCurrencyInvoicesRequest();
            var result = await lucaService.ListCurrencyInvoicesAsync(effective);
            return Ok(result);
        }

        [HttpPost("finance/credit-card-entry/create")]
        public async Task<IActionResult> CreateCreditCardEntry([FromBody] LucaCreateCreditCardEntryRequest request)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var result = await lucaService.CreateCreditCardEntryAsync(request);
            return Ok(result);
        }

        [HttpPost("finance/banks/list")]
        public async Task<IActionResult> ListBanks([FromBody] LucaListBanksRequest? request = null)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var effective = request ?? new LucaListBanksRequest();
            var result = await lucaService.ListBanksAsync(effective);
            return Ok(result);
        }

        [HttpPost("finance/cash/list")]
        public async Task<IActionResult> ListCashAccounts([FromBody] LucaListCashAccountsRequest? request = null)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var effective = request ?? new LucaListCashAccountsRequest();
            var result = await lucaService.ListCashAccountsAsync(effective);
            return Ok(result);
        }

        [HttpPost("finance/cari-movements/create")]
        public async Task<IActionResult> CreateCariMovement([FromBody] LucaCreateCariHareketRequest request)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var result = await lucaService.CreateCustomerTransactionAsync(request);
            return Ok(result);
        }

        [HttpPost("finance/cari-movements/list")]
        public async Task<IActionResult> ListCariMovements([FromBody] LucaListCariHareketBaslikRequest? request = null)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var effective = request ?? new LucaListCariHareketBaslikRequest();
            var result = await lucaService.ListCustomerTransactionsAsync(effective, false);
            return Ok(result);
        }

        [HttpPost("reports/stock-service")]
        public async Task<IActionResult> GenerateStockServiceReport([FromBody] LucaDynamicStockServiceReportRequest? request = null)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
            var effective = request ?? new LucaDynamicStockServiceReportRequest();
            var data = await lucaService.GenerateStockServiceReportAsync(effective);
            var fileName = !string.IsNullOrWhiteSpace(effective.OutputFileName)
                ? effective.OutputFileName
                : $"stok-hizmet-ekstre-{DateTime.UtcNow:yyyyMMddHHmmss}.xls";
            return File(data, "application/vnd.ms-excel", fileName);
        }

    }
}
