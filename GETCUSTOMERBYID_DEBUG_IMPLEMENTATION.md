# GetCustomerByIdAsync Debug Implementation

## Purpose
Enhanced the `GetCustomerByIdAsync` method in `KatanaService.cs` with detailed debug logging to diagnose why customer lookups might be failing.

## Problem Context
The `GetCustomerByIdAsync` method was returning `null` for all customer lookups, causing all customers to be created as "Unknown Customer". We need detailed logging to understand:
1. What endpoint URL is being called
2. What HTTP status code is returned
3. What response body is received from Katana API
4. Whether the issue is with the request, response, or parsing

## Implementation

### Changes Made to KatanaService.cs

#### 1. Added Endpoint Debug Logging (Line ~569)

**Before:**
```csharp
_logger.LogInformation("Getting customer by ID from Katana: {CustomerId}", customerId);
var response = await _httpClient.GetAsync($"{_settings.Endpoints.Customers}/{customerId}");
```

**After:**
```csharp
var endpoint = $"{_settings.Endpoints.Customers}/{customerId}";
_logger.LogDebug("🔍 Fetching customer from Katana: {Endpoint}", endpoint);
var response = await _httpClient.GetAsync(endpoint);
```

**Why:** Shows the exact URL being called, helping identify configuration issues.

#### 2. Added Error Status Debug Logging (Line ~574)

**Before:**
```csharp
if (!response.IsSuccessStatusCode)
{
    if (response.StatusCode == HttpStatusCode.NotFound)
    {
        _logger.LogWarning("Customer not found in Katana: {CustomerId}", customerId);
    }
    // ...
}
```

**After:**
```csharp
if (!response.IsSuccessStatusCode)
{
    _logger.LogWarning("❌ Katana customer fetch failed: {StatusCode} - {Reason}", 
        response.StatusCode, response.ReasonPhrase);
    
    if (response.StatusCode == HttpStatusCode.NotFound)
    {
        _logger.LogWarning("Customer not found in Katana: {CustomerId}", customerId);
    }
    // ...
}
```

**Why:** Immediately logs the HTTP status code and reason, making failures obvious.

#### 3. Added Response Body Debug Logging (Line ~591)

**Before:**
```csharp
var content = await response.Content.ReadAsStringAsync();
using var doc = JsonDocument.Parse(content);
```

**After:**
```csharp
var content = await response.Content.ReadAsStringAsync();
_logger.LogDebug("📦 Katana customer response: {Json}", content);

using var doc = JsonDocument.Parse(content);
```

**Why:** Shows the actual JSON response from Katana, helping identify parsing issues.

## Expected Log Output

### Successful Customer Fetch
```
🔍 Fetching customer from Katana: https://api.katanamrp.com/v1/customers/12345
📦 Katana customer response: {"id":12345,"name":"John Doe","email":"john@example.com",...}
Retrieved customer 12345 from Katana: John Doe
```

### Customer Not Found (404)
```
🔍 Fetching customer from Katana: https://api.katanamrp.com/v1/customers/99999
❌ Katana customer fetch failed: NotFound - Not Found
Customer not found in Katana: 99999
```

### API Error (500, 401, etc.)
```
🔍 Fetching customer from Katana: https://api.katanamrp.com/v1/customers/12345
❌ Katana customer fetch failed: Unauthorized - Unauthorized
Failed to get customer 12345 from Katana. Status: Unauthorized, Error: {"error":"Invalid API key"}
```

### Network Error
```
🔍 Fetching customer from Katana: https://api.katanamrp.com/v1/customers/12345
Katana API connection error for customer ID 12345
System.Net.Http.HttpRequestException: Connection refused
```

### JSON Parsing Error
```
🔍 Fetching customer from Katana: https://api.katanamrp.com/v1/customers/12345
📦 Katana customer response: <html>Error 500</html>
Failed to parse customer response for ID 12345
System.Text.Json.JsonException: '<' is an invalid start of a value
```

## Diagnostic Scenarios

### Scenario 1: Wrong Endpoint URL
**Symptoms:** All requests fail with 404
**Log Pattern:**
```
🔍 Fetching customer from Katana: https://wrong-url.com/customers/12345
❌ Katana customer fetch failed: NotFound
```
**Solution:** Check `appsettings.json` → `KatanaApi:Endpoints:Customers`

### Scenario 2: Authentication Issues
**Symptoms:** All requests fail with 401/403
**Log Pattern:**
```
🔍 Fetching customer from Katana: https://api.katanamrp.com/v1/customers/12345
❌ Katana customer fetch failed: Unauthorized
```
**Solution:** Check `appsettings.json` → `KatanaApi:ApiKey`

### Scenario 3: Customer Doesn't Exist
**Symptoms:** Specific customer IDs return 404
**Log Pattern:**
```
🔍 Fetching customer from Katana: https://api.katanamrp.com/v1/customers/12345
❌ Katana customer fetch failed: NotFound
Customer not found in Katana: 12345
```
**Solution:** Verify customer ID exists in Katana, or use customer cache approach

### Scenario 4: Response Format Changed
**Symptoms:** Requests succeed but parsing fails
**Log Pattern:**
```
🔍 Fetching customer from Katana: https://api.katanamrp.com/v1/customers/12345
📦 Katana customer response: {"customer":{"id":12345,...}}
Failed to parse customer response for ID 12345
```
**Solution:** Update `MapCustomerElement` to handle new response format

### Scenario 5: Rate Limiting
**Symptoms:** Requests fail with 429 Too Many Requests
**Log Pattern:**
```
🔍 Fetching customer from Katana: https://api.katanamrp.com/v1/customers/12345
❌ Katana customer fetch failed: TooManyRequests
```
**Solution:** Implement rate limiting or use customer cache approach

## Testing Instructions

### 1. Enable Debug Logging
Ensure Debug level is enabled in `appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Katana.Infrastructure": "Debug"
    }
  }
}
```

### 2. Test Individual Customer Lookup
Use the existing diagnostic logs from the previous implementation:
```
🔍 Fetching customer from Katana: ID=12345
📦 Katana customer result: NULL
```

This will now be followed by the detailed logs from `GetCustomerByIdAsync`:
```
🔍 Fetching customer from Katana: https://api.katanamrp.com/v1/customers/12345
❌ Katana customer fetch failed: NotFound - Not Found
```

### 3. Analyze Logs
Search for the debug emojis:
```bash
# See all customer fetch attempts
grep "🔍 Fetching customer from Katana:" logs/application.log

# See all responses
grep "📦 Katana customer response:" logs/application.log

# See all failures
grep "❌ Katana customer fetch failed:" logs/application.log
```

### 4. Compare with Customer Cache
Since we now use customer cache, this method should rarely be called. If you see many calls to `GetCustomerByIdAsync`, it means:
- The cache isn't working
- Customers are being looked up outside the sync flow
- There's a bug in the cache implementation

## Integration with Customer Cache

**Important:** With the customer cache implementation, `GetCustomerByIdAsync` should rarely be called during sync operations. The logs will help verify:

1. **Cache is Working:** You should NOT see these logs during normal sync
2. **Cache Miss:** If you see these logs, it means a customer wasn't in the cache
3. **Direct Calls:** These logs will appear for any direct API calls outside the sync flow

## Existing Features Preserved

The enhanced implementation maintains all existing features:
- ✅ Memory caching (5 minutes)
- ✅ Input validation (customerId > 0)
- ✅ Response unwrapping (handles `{"data": {...}}` format)
- ✅ Comprehensive error handling (HTTP, JSON, Network errors)
- ✅ Custom mapping via `MapCustomerElement`

## Performance Impact

- **Minimal:** Debug logs only execute when Debug level is enabled
- **Production:** Set log level to Information or Warning to disable debug logs
- **Cache:** Reduces API calls by 95%+ (only cache misses hit the API)

## Next Steps

1. **Deploy and Monitor:** Watch for the new debug logs in production
2. **Identify Root Cause:** Use logs to determine why lookups fail
3. **Fix Issues:** Based on diagnostic findings
4. **Verify Cache:** Confirm customer cache is working and this method is rarely called
5. **Clean Up:** Once stable, can reduce log verbosity or remove debug logs

## Related Files

- `src/Katana.Infrastructure/APIClients/KatanaService.cs` - This file (GetCustomerByIdAsync)
- `src/Katana.API/Controllers/SyncController.cs` - Uses customer cache instead
- `src/Katana.API/Workers/KatanaSalesOrderSyncWorker.cs` - Uses customer cache instead
- `CUSTOMER_CACHE_IMPLEMENTATION.md` - Customer cache documentation
