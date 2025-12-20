# Test Luca Session Fix
# This script tests if the Luca API session issue is resolved

$baseUrl = "http://localhost:5055"
$ErrorActionPreference = "Stop"

Write-Host "=== LUCA SESSION FIX TEST ===" -ForegroundColor Cyan
Write-Host ""

# 1. Login to backend
Write-Host "1. Logging in to backend..." -ForegroundColor Yellow
try {
    $loginBody = @{ Username = "admin"; Password = "Katana2025!" } | ConvertTo-Json
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
    $headers = @{ "Authorization" = "Bearer $($loginResponse.token)" }
    Write-Host "   ✅ Login successful" -ForegroundColor Green
} catch {
    Write-Host "   ❌ Login failed: $_" -ForegroundColor Red
    exit 1
}

# 2. Test Luca stock cards endpoint
Write-Host ""
Write-Host "2. Fetching Luca stock cards..." -ForegroundColor Yellow
try {
    $stockCards = Invoke-RestMethod -Uri "$baseUrl/api/products/luca-stock-cards" -Method Get -Headers $headers
    $count = if ($stockCards -is [Array]) { $stockCards.Count } else { 1 }
    
    if ($count -eq 0) {
        Write-Host "   ⚠️  WARNING: 0 stock cards returned (session might still be broken)" -ForegroundColor Yellow
        Write-Host "   Check backend logs for HTML response errors" -ForegroundColor Yellow
    } else {
        Write-Host "   ✅ SUCCESS: $count stock cards loaded" -ForegroundColor Green
    }
} catch {
    Write-Host "   ❌ Failed to fetch stock cards: $_" -ForegroundColor Red
    exit 1
}

# 3. Trigger sync with sill products
Write-Host ""
Write-Host "3. Triggering sync with limit=700 (includes sill products at index 678)..." -ForegroundColor Yellow
try {
    $syncBody = @{ 
        limit = 700
        forceSync = $true 
    } | ConvertTo-Json
    
    $syncResponse = Invoke-RestMethod -Uri "$baseUrl/api/sync/trigger" -Method Post -Body $syncBody -ContentType "application/json" -Headers $headers
    
    Write-Host "   ✅ Sync triggered" -ForegroundColor Green
    Write-Host "   Status: $($syncResponse.message)" -ForegroundColor Cyan
    
    if ($syncResponse.processedRecords) {
        Write-Host "   Processed: $($syncResponse.processedRecords) records" -ForegroundColor Cyan
    }
} catch {
    Write-Host "   ❌ Sync failed: $_" -ForegroundColor Red
    Write-Host "   Response: $($_.Exception.Response)" -ForegroundColor Red
}

# 4. Check for sill products in Luca
Write-Host ""
Write-Host "4. Checking for 'sill' products in Luca..." -ForegroundColor Yellow
try {
    $stockCards = Invoke-RestMethod -Uri "$baseUrl/api/products/luca-stock-cards" -Method Get -Headers $headers
    $sillProducts = $stockCards | Where-Object { 
        $_.stokKodu -like "*sill*" -or $_.stokAdi -like "*sill*" 
    }
    
    if ($sillProducts) {
        $sillCount = if ($sillProducts -is [Array]) { $sillProducts.Count } else { 1 }
        Write-Host "   ✅ Found $sillCount sill products:" -ForegroundColor Green
        $sillProducts | ForEach-Object {
            Write-Host "      - SKU: $($_.stokKodu), Name: $($_.stokAdi)" -ForegroundColor Cyan
        }
    } else {
        Write-Host "   ⚠️  No sill products found yet" -ForegroundColor Yellow
        Write-Host "   This is expected if sync just started" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ❌ Failed to check sill products: $_" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== TEST COMPLETE ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Check backend logs for 'ListStockCardsAsync' messages" -ForegroundColor White
Write-Host "2. Look for 'Luca cache hazır: X stok kartı yüklendi' - should be > 0" -ForegroundColor White
Write-Host "3. Verify no 'Still HTML after retry' errors" -ForegroundColor White
Write-Host "4. Wait for sync to complete and check if sill products appear" -ForegroundColor White
