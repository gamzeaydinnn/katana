# Luca Session Fix - HTML Response Issue

## Problem Summary

The backend was returning **0 stock cards** from Luca API because:

1. `ManualSessionCookie` in `appsettings.json` had placeholder value `"JSESSIONID=FILL_ME"`
2. This invalid cookie caused Luca API to return **HTML (login page)** instead of JSON
3. Empty cache prevented product change detection, causing sync issues

## Root Cause

```
Backend Log: [ERR] ListStockCardsAsync: Still HTML after retry
Backend Log: [INF] Luca cache hazır: 0 stok kartı yüklendi
```

The system was trying to use an invalid manual session cookie, and when Luca rejected it, the retry logic wasn't properly clearing the session state.

## Changes Made

### 1. Fixed Configuration (`src/Katana.API/appsettings.json`)

**Before:**

```json
"ManualSessionCookie": "JSESSIONID=FILL_ME",
```

**After:**

```json
"ManualSessionCookie": "",
```

This allows the system to perform **automatic login** instead of using an invalid manual cookie.

### 2. Improved Session Refresh (`src/Katana.Infrastructure/APIClients/LucaService.Operations.cs`)

**Before:**

```csharp
await PerformLoginAsync();
await EnsureBranchSelectedAsync();
```

**After:**

```csharp
await ForceSessionRefreshAsync();
```

Now uses `ForceSessionRefreshAsync()` which:

- Clears all session state
- Disposes old HttpClient
- Forces complete re-authentication
- Ensures clean session

### 3. Added Cookie Validation (`src/Katana.Infrastructure/APIClients/LucaService.Core.cs`)

Added validation to skip manual cookie if it's invalid:

```csharp
if (!string.IsNullOrWhiteSpace(_settings.ManualSessionCookie) &&
    !_settings.ManualSessionCookie.Contains("FILL_ME", StringComparison.OrdinalIgnoreCase) &&
    _settings.ManualSessionCookie.Length > 20)
{
    // Use manual cookie
}
else
{
    // Fall back to automatic login
}
```

### 4. Enhanced Logging

Added detailed logging to track:

- Manual cookie validity
- Session state during authentication
- Cookie expiry times
- HTML response detection

## Testing

Run the test script:

```powershell
.\test-luca-session-fix.ps1
```

This will:

1. ✅ Login to backend
2. ✅ Fetch Luca stock cards (should return > 0 cards)
3. ✅ Trigger sync with limit=700 (includes sill products)
4. ✅ Check for sill products in Luca

## Expected Results

### Before Fix:

```
[ERR] ListStockCardsAsync: Still HTML after retry
[INF] Luca cache hazır: 0 stok kartı yüklendi
```

### After Fix:

```
[INF] 🔐 Login SUCCESS: JSESSIONID acquired
[INF] Luca cache hazır: 1234 stok kartı yüklendi  ← Should be > 0
[INF] ✅ ForceSessionRefreshAsync tamamlandı
```

## Impact on Original Issues

### Issue 1: Mapping Error (Name vs SKU)

- **Status**: Will be testable now that cache is populated
- **Next**: Run sync and check logs for "Katana Name:" vs "Luca Name:" comparison
- The mapping code was already correct, but couldn't be tested with empty cache

### Issue 2: Product Updates Not Reflecting

- **Status**: Will work now that cache is populated
- **Next**: Update a sill product in Katana and verify new versioned card is created
- System can now detect existing products and create -V2, -V3 versions

## Verification Steps

1. **Restart backend** to apply config changes:

   ```bash
   docker-compose restart backend
   ```

2. **Check logs** for successful authentication:

   ```bash
   docker-compose logs -f backend | grep "Luca cache"
   ```

   Should see: `Luca cache hazır: X stok kartı yüklendi` where X > 0

3. **Run test script**:

   ```powershell
   .\test-luca-session-fix.ps1
   ```

4. **Trigger sync** with sill products:

   ```powershell
   .\test-sync-sill-specific.ps1
   ```

5. **Check for sill products**:
   ```powershell
   .\check-sill-in-luca.ps1
   ```

## Alternative: Using Manual Cookie

If you prefer to use a manual session cookie (for faster startup):

1. **Get a valid JSESSIONID** from Luca web interface:

   - Login to https://akozas.luca.com.tr/Yetki/
   - Open browser DevTools → Application → Cookies
   - Copy the JSESSIONID value

2. **Update appsettings.json**:

   ```json
   "ManualSessionCookie": "JSESSIONID=YOUR_ACTUAL_COOKIE_VALUE_HERE",
   ```

3. **Note**: Manual cookies expire after ~20 minutes, so automatic login is more reliable

## Files Modified

- ✅ `src/Katana.API/appsettings.json` - Cleared invalid manual cookie
- ✅ `src/Katana.Infrastructure/APIClients/LucaService.Core.cs` - Added cookie validation
- ✅ `src/Katana.Infrastructure/APIClients/LucaService.Operations.cs` - Improved session refresh
- ✅ `test-luca-session-fix.ps1` - Created test script

## Next Steps

1. Restart backend
2. Run test script
3. Verify stock card cache is populated (> 0 cards)
4. Test product sync with sill products
5. Verify product updates create new versions
