# Test sync for specific sill products
$baseUrl = "http://localhost:5055"

# Login
Write-Host "Logging in..." -ForegroundColor Cyan
$loginBody = @{ Username = "admin"; Password = "Katana2025!" } | ConvertTo-Json
$loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
$headers = @{ "Authorization" = "Bearer $($loginResponse.token)" }

Write-Host "✅ Logged in successfully" -ForegroundColor Green

# Get Katana products
Write-Host "`nFetching Katana products..." -ForegroundColor Cyan
$products = Invoke-RestMethod -Uri "$baseUrl/api/products" -Method Get -Headers $headers

# Find sill products
$sillProducts = $products | Where-Object { $_.sku -like "*sill*" -or $_.name -like "*sill*" }

Write-Host "`nFound $($sillProducts.Count) sill products in Katana:" -ForegroundColor Yellow
$sillProducts | ForEach-Object {
    Write-Host "  SKU: $($_.sku), Name: $($_.name), Price: $($_.salesPrice)" -ForegroundColor White
}

# Trigger sync with higher limit to include sill products
Write-Host "`nTriggering sync (limit=700, dryRun=false)..." -ForegroundColor Cyan
$syncBody = @{ 
    limit = 700
    dryRun = $false
    forceSendDuplicates = $false
} | ConvertTo-Json

try {
    $syncResult = Invoke-RestMethod -Uri "$baseUrl/api/sync/to-luca/stock-cards" -Method Post -Body $syncBody -ContentType "application/json" -Headers $headers

    Write-Host "`n=== SYNC RESULT ===" -ForegroundColor Yellow
    Write-Host "Success: $($syncResult.isSuccess)" -ForegroundColor $(if ($syncResult.isSuccess) { "Green" } else { "Red" })
    Write-Host "Processed: $($syncResult.processedRecords)"
    Write-Host "Successful: $($syncResult.successfulRecords)"
    Write-Host "Failed: $($syncResult.failedRecords)"
    Write-Host "Message: $($syncResult.message)"

    if ($syncResult.errors -and $syncResult.errors.Count -gt 0) {
        Write-Host "`nErrors:" -ForegroundColor Red
        $syncResult.errors | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    }
} catch {
    Write-Host "`n❌ Sync failed: $_" -ForegroundColor Red
}

Write-Host "`n✅ Done" -ForegroundColor Green
